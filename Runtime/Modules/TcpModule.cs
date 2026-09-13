using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using Nox.CCK.Scripting;
using Nox.Scripting;
using Logger = Nox.CCK.Utils.Logger;

namespace Nox.Network.Runtime.Modules {
	/// <summary>
	/// Scripting module <c>"tcp"</c> — low-level TCP client.
	///
	/// <para>
	/// Provides a <c>connect(host, port)</c> factory that returns a <c>TcpSocket</c>
	/// object. The socket exposes async read/write operations and event objects
	/// (<c>socket.data.on(...)</c>, <c>socket.closed.on(...)</c>) so scripts can
	/// react to incoming data or loss of connection.
	/// </para>
	///
	/// <code>
	/// import { connect } from 'tcp';
	///
	/// // Connect to a TCP server
	/// const socket = await connect('example.com', 8080);
	///
	/// // Send raw bytes
	/// await socket.write(new Uint8Array([0x01, 0x02, 0x03]));
	///
	/// // Read raw bytes (blocks until data arrives)
	/// const bytes = await socket.read();               // Promise&lt;byte[]&gt;
	///
	/// // Check connection state
	/// console.log(socket.connected);    // boolean
	/// console.log(socket.host);         // string
	/// console.log(socket.port);         // number
	///
	/// // Close the connection
	/// await socket.close();
	/// </code>
	/// </summary>
	public static class TcpModule {
		private const int CONNECT_TIMEOUT_MS = 5000;
		private const int BUFFER_SIZE = 65536;

		public static readonly IScriptingModuleDefinition Module =
			ScriptingModuleBuilder.Create("tcp")
				.WithTags("session")
				// ── Factory ───────────────────────────────────────────────────
				.AddAsyncMethod("connect", async (ctx, args) => {
					if (args.Length < 2) {
						Logger.LogWarning("connect requires a host and port.", tag: nameof(TcpModule));
						return null;
					}
					var host = args[0]?.ToString();
					if (!int.TryParse(args[1]?.ToString(), out var port)) {
						Logger.LogWarning("connect received an invalid port.", tag: nameof(TcpModule));
						return null;
					}
					if (string.IsNullOrEmpty(host)) {
						Logger.LogWarning("connect received an empty host.", tag: nameof(TcpModule));
						return null;
					}
					var socket = new TcpSocket(host, port, ctx.CancellationToken);
					if (!await socket.ConnectAsync()) {
						Logger.LogWarning($"Connection failed to {host}:{port}.", tag: nameof(TcpModule));
						return null;
					}
					return (object)socket;
				})
				.Build();

		// ── TcpSocket ──────────────────────────────────────────────────────────

		/// <summary>
		/// Managed TCP connection exposed to scripts. Wraps <see cref="Socket"/>
		/// with async read/write and fires data/error/connected/closed events
		/// so scripts can subscribe using <c>on('data', handler)</c> etc.
		/// </summary>
		public sealed class TcpSocket : IDisposable {
			private Socket _socket;
			private readonly CancellationToken _token;
			private readonly byte[] _receiveBuffer = new byte[BUFFER_SIZE];
			private readonly object _sendLock = new();
			private bool _disposed;

			// Event handlers stored as lists to support multiple subscribers
			private readonly List<Action<object[]>> _dataHandlers = new();
			private readonly List<Action<object[]>> _errorHandlers = new();
			private readonly List<Action<object[]>> _connectedHandlers = new();
			private readonly List<Action<object[]>> _closedHandlers = new();
			private readonly object _lock = new();

			/// <summary>Remote host this socket is connected to.</summary>
			public string Host { get; }

			/// <summary>Remote port this socket is connected to.</summary>
			public int Port { get; }

			/// <summary>Whether the underlying TCP connection is still open.</summary>
			public bool Connected
				=> !_disposed && _socket is { Connected: true };

			internal TcpSocket(string host, int port, CancellationToken token = default) {
				Host  = host;
				Port  = port;
				_token = token;
			}

			/// <summary>Establish the TCP connection. Returns <c>true</c> on success.</summary>
			internal async UniTask<bool> ConnectAsync() {
				try {
					var connectTask = Task.Run(() => ConnectBlocking(), _token);
					var timeoutTask = Task.Delay(CONNECT_TIMEOUT_MS, _token);
					var completedTask = await Task.WhenAny(connectTask, timeoutTask).AsUniTask();
					if (completedTask == timeoutTask) {
						Logger.LogWarning($"Connection timeout to {Host}:{Port} after {CONNECT_TIMEOUT_MS} ms.", tag: nameof(TcpModule));
						lock (_lock) {
							_disposed = true;
							try { _socket?.Close(); } catch { }
							_socket = null;
						}
						return false;
					}

					var connectedSocket = await connectTask.AsUniTask();
					if (connectedSocket == null) return false;
					lock (_lock) {
						if (_disposed) {
							try { connectedSocket.Close(); } catch { }
							return false;
						}
						_socket = connectedSocket;
					}
					// Fire connected event
					FireConnected();
					// Keep blocking network continuations away from Unity's main thread.
					UniTask.RunOnThreadPool(
						() => ReadLoopAsync(),
						cancellationToken: _token
					).Forget();
					return true;
				} catch (Exception ex) {
					Logger.LogWarning($"Connection failed ({Host}:{Port}): {ex.Message}", tag: nameof(TcpModule));
					try { _socket?.Close(); } catch { }
					_socket = null;
					FireError(ex.Message);
					return false;
				}
			}

			private Socket ConnectBlocking() {
				Socket connectedSocket = null;
				try {
					if (!IPAddress.TryParse(Host, out var ipAddress)) {
						var hostEntry = Dns.GetHostEntry(Host);
						ipAddress = hostEntry.AddressList.FirstOrDefault(ip => ip.AddressFamily == AddressFamily.InterNetwork);
						if (ipAddress == null) {
							Logger.LogWarning($"No IPv4 address found for {Host}.", tag: nameof(TcpModule));
							return null;
						}
					}

					connectedSocket = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp) {
						NoDelay = true,
						ReceiveBufferSize = BUFFER_SIZE,
						SendBufferSize = BUFFER_SIZE
					};
					connectedSocket.Connect(ipAddress, Port);
					lock (_lock) {
						if (_disposed) {
							connectedSocket.Close();
							return null;
						}
					}
					return connectedSocket.Connected ? connectedSocket : null;
				} catch {
					try { connectedSocket?.Close(); } catch { }
					throw;
				}
			}

			// ── Event emitter internals ────────────────────────────────────

			/// <summary>Add a handler for an event.</summary>
			internal void AddHandler(string eventName, Action<object[]> handler) {
				lock (_lock) 
					switch (eventName) {
						case "data":     _dataHandlers.Add(handler); break;
						case "error":    _errorHandlers.Add(handler); break;
						case "connected": _connectedHandlers.Add(handler); break;
						case "closed":   _closedHandlers.Add(handler); break;
					}
			}

			/// <summary>Add a handler that fires once then auto-removes.</summary>
			internal void AddOnceHandler(string eventName, Action<object[]> handler) {
                void onceWrapper(object[] args)
                {
                    try { handler(args); }
                    catch { /* ignored */ }
                    RemoveHandler(eventName, onceWrapper);
                }

                AddHandler(eventName, onceWrapper);
			}

			/// <summary>Remove a handler for an event.</summary>
			internal void RemoveHandler(string eventName, Action<object[]> handler) {
				lock (_lock)
					switch (eventName) {
						case "data":     _dataHandlers.Remove(handler); break;
						case "error":    _errorHandlers.Remove(handler); break;
						case "connected": _connectedHandlers.Remove(handler); break;
						case "closed":   _closedHandlers.Remove(handler); break;
					}
			}

			/// <summary>Emit an event from script side (if AllowEmit flag is set).</summary>
			internal void Emit(string eventName, object[] args) {
				lock (_lock)
					switch (eventName) {
						case "data":     foreach (var h in _dataHandlers) { try { h(args); } catch { } } break;
						case "error":    foreach (var h in _errorHandlers) { try { h(args); } catch { } } break;
						case "connected": foreach (var h in _connectedHandlers) { try { h(args); } catch { } } break;
						case "closed":   foreach (var h in _closedHandlers) { try { h(args); } catch { } } break;
					}
			}

			private void FireData(byte[] bytes) {
				var args = new object[] { bytes };
				PostHandlers(_dataHandlers, args);
			}

			private void FireError(string message) {
				var args = new object[] { message };
				PostHandlers(_errorHandlers, args);
			}

			private void FireConnected() {
				var args = Array.Empty<object>();
				PostHandlers(_connectedHandlers, args);
			}

			private void FireClosed() {
				var args = Array.Empty<object>();
				PostHandlers(_closedHandlers, args);
			}

			private void PostHandlers(List<Action<object[]>> handlers, object[] args) {
				Action<object[]>[] snapshot;
				lock (_lock)
					snapshot = handlers.ToArray();
				if (snapshot.Length == 0)
					return;

				UniTask.Post(() => {
					foreach (var handler in snapshot)
						try { handler(args); }
						catch (Exception ex) { Logger.LogWarning($"TCP event handler failed: {ex.Message}", tag: nameof(TcpModule)); }
				});
			}

			// ── Read loop ────────────────────────────────────────────────

			private async UniTask ReadLoopAsync() {
				try {
					while (!_disposed && Connected) {
						_token.ThrowIfCancellationRequested();
						var count = await _socket.ReceiveAsync(new ArraySegment<byte>(_receiveBuffer), SocketFlags.None).ConfigureAwait(false);
						if (count == 0)
							break;
						var result = new byte[count];
						Buffer.BlockCopy(_receiveBuffer, 0, result, 0, count);
						FireData(result);
					}
				} catch (OperationCanceledException) {
					// Expected on cancellation
				} catch (Exception ex) {
					Logger.LogWarning($"Receive failed ({Host}:{Port}): {ex.Message}", tag: nameof(TcpModule));
					FireError(ex.Message);
				} finally {
					if (!_disposed) {
						Dispose();
					}
				}
			}

			// ── Write ────────────────────────────────────────────────────────

			/// <summary>Write raw bytes to the stream.</summary>
			public UniTask Write(byte[] bytes)
				=> UniTask.RunOnThreadPool(() => SendBlocking(bytes), cancellationToken: _token);

			private void SendBlocking(byte[] bytes) {
				if (_socket == null || _disposed || bytes == null) return;
				_token.ThrowIfCancellationRequested();
				try {
					lock (_sendLock) {
						var sent = 0;
						while (sent < bytes.Length) {
							var sentNow = _socket.Send(bytes, sent, bytes.Length - sent, SocketFlags.None);
							if (sentNow <= 0) return;
							sent += sentNow;
						}
					}
				} catch (Exception ex) {
					Logger.LogWarning($"Send failed ({Host}:{Port}): {ex.Message}", tag: nameof(TcpModule));
					FireError(ex.Message);
				}
			}

			// ── Read (legacy, for backward compat) ─────────────────────────

			/// <summary>Read available bytes from the stream and return them as a <c>byte[]</c>.</summary>
			public async UniTask<byte[]> Read() {
				if (_socket == null || _disposed) return null;
				try {
					_token.ThrowIfCancellationRequested();
					var buffer = new byte[BUFFER_SIZE];
					var count  = await _socket.ReceiveAsync(new ArraySegment<byte>(buffer), SocketFlags.None).ConfigureAwait(false);
					if (count == 0) return null;
					var result = new byte[count];
					Buffer.BlockCopy(buffer, 0, result, 0, count);
					return result;
				} catch (OperationCanceledException) {
					return null;
				} catch (Exception ex) {
					Logger.LogWarning($"Read failed ({Host}:{Port}): {ex.Message}", tag: nameof(TcpModule));
					return null;
				}
			}

			// ── Control ──────────────────────────────────────────────────────

			/// <summary>Close the TCP connection and release resources.</summary>
			public UniTask Close() {
				Dispose();
				return UniTask.CompletedTask;
			}

			/// <inheritdoc/>
			public void Dispose() {
				if (_disposed) return;
				_disposed = true;
				FireClosed();
				try { _socket?.Shutdown(SocketShutdown.Both); }
				catch { /* ignored */ }
				try { _socket?.Close(); }
                catch { /* ignored */ }
			}
		}

		// ── Type converter ─────────────────────────────────────────────────────

		/// <summary>
		/// Type converter for <see cref="TcpSocket"/> that exposes properties,
		/// methods, and events (on/off/once/emit) to scripting backends.
		/// </summary>
		public static readonly IScriptingTypeConverter SocketConverter =
			ScriptingTypeConverterBuilder<TcpSocket>.Create()
				// Properties
				.AddProperty("host",      sock => sock.Host,      flags: ScriptingTypePropertyFlags.InspectGetter | ScriptingTypePropertyFlags.IsReadOnly)
				.AddProperty("port",      sock => sock.Port,      flags: ScriptingTypePropertyFlags.InspectGetter | ScriptingTypePropertyFlags.IsReadOnly)
				.AddProperty("connected", sock => sock.Connected, flags: ScriptingTypePropertyFlags.InspectGetter | ScriptingTypePropertyFlags.IsReadOnly)
				// Methods
				.AddAsyncMethod("write", async (sock, args) => {
					await sock.Write(args.Length > 0 ? args[0] as byte[] : null);
					return (object)null;
				})
				.AddAsyncMethod("read", async (sock, _) => (object)await sock.Read())
				.AddAsyncMethod("close", async (sock, _) => {
					await sock.Close();
					return (object)null;
				})
				// Events: socket.data.on(), socket.error.on(), socket.connected.on(), socket.closed.on()
				.AddEvent(
					"data",
					(ctx, sock, handler) => sock.AddHandler("data", handler),
					(ctx, sock, handler) => sock.AddOnceHandler("data", handler),
					(ctx, sock, handler) => sock.RemoveHandler("data", handler),
					(ctx, sock, args) => sock.Emit("data", args)
				)
				.AddEvent(
					"error",
					(ctx, sock, handler) => sock.AddHandler("error", handler),
					(ctx, sock, handler) => sock.AddOnceHandler("error", handler),
					(ctx, sock, handler) => sock.RemoveHandler("error", handler),
					(ctx, sock, args) => sock.Emit("error", args)
				)
				.AddEvent(
					"connected",
					(ctx, sock, handler) => sock.AddHandler("connected", handler),
					(ctx, sock, handler) => sock.AddOnceHandler("connected", handler),
					(ctx, sock, handler) => sock.RemoveHandler("connected", handler),
					(ctx, sock, args) => sock.Emit("connected", args)
				)
				.AddEvent(
					"closed",
					(ctx, sock, handler) => sock.AddHandler("closed", handler),
					(ctx, sock, handler) => sock.AddOnceHandler("closed", handler),
					(ctx, sock, handler) => sock.RemoveHandler("closed", handler),
					(ctx, sock, args) => sock.Emit("closed", args)
				)
				.SetDefault((TcpSocket)null)
				.Build();
	}
}
