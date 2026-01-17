using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Cysharp.Threading.Tasks;
using Nox.CCK.Language;
using Nox.CCK.Mods.Cores;
using Nox.CCK.Mods.Initializers;
using Nox.CCK.Network;
using Nox.CCK.Utils;
using Nox.Users;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;
using Logger = Nox.CCK.Utils.Logger;

namespace Nox.Network.Runtime {
	public class Main : IMainModInitializer, INetworkAPI {
		private IModCoreAPI coreAPI;
		private static Main _instance;
		private LanguagePack _language;

		public void OnInitialize(IModCoreAPI api) {
			coreAPI = api;
			_instance = this;
			RequestExtension.OnCreated.AddListener(OnBeforeRequest);
		}
		private void OnBeforeRequest(Request arg0) {
			var headers = new Dictionary<string, string>();

			var builder = new StringBuilder();
			builder.Clear();
			builder.Append(Application.productName);
			builder.Append('/');
			builder.Append(Application.version);
			builder.Append(' ');
			builder.Append(Constants.ProtocolIdentifier);
			builder.Append('/');
			builder.Append(Constants.ProtocolVersion);
			builder.Append(" (en=");
			builder.Append(EngineExtensions.CurrentEngine.GetEngineName());
			builder.Append("; pn=");
			builder.Append(PlatformExtensions.CurrentPlatform.GetPlatformName());
			builder.Append(')');

			headers["user-agent"] = builder.ToString();
			headers["x-uuid"] = SystemInfo.deviceUniqueIdentifier;
			headers["x-powered-by"] = "Nox";

			builder.Clear();

			var mods = coreAPI.ModAPI.GetMods();
			var first = true;

			foreach (var mod in mods) {
				if (mod?.IsLoaded() != true) continue;

				var metadata = mod.GetMetadata();
				if (metadata == null) continue;

				if (!first) builder.Append("; ");
				builder.Append(metadata.GetId());
				builder.Append('/');
				builder.Append(metadata.GetVersion());
				first = false;
			}

			headers["x-nox-mods"] = builder.ToString();

			foreach (var header in headers)
				arg0.SetRequestHeader(header.Key, header.Value);
		}

		#if UNITY_EDITOR
		[MenuItem("Tools/Nox/Network Test Upload")]
		public static void TestUpload()
			=> _instance?.OnPostInitializeMainAsync().Forget();

		public async UniTask OnPostInitializeMainAsync() {
			var req = RequestExtension.To("https://httpbin.org/post");
			req.method = RequestExtension.Method.POST;

			var rng = new System.Random();
			var randomBytes = new byte[rng.Next(16, 128)];
			rng.NextBytes(randomBytes);

			req.SetBody(new List<IMultipartFormSection>() {
				new MultipartFormDataSection("test_field", "test_value"),
				new MultipartFormFileSection("file", randomBytes, "test_file.txt", "text/plain")
			});

			if (await RequestExtension.Send(req) && req.responseCode == 200)
				Logger.LogDebug("Upload successful! " + await req.Text(), tag: "TestNetwork");
			else Logger.LogWarning($"Upload failed with code {req.responseCode}", tag: "TestNetwork");
		}
		#endif

		public void OnDispose() {
			coreAPI = null;
			_instance = null;
		}

		private readonly HashSet<UnityWebRequest> _activeRequests = new();

		[NoxPublic(NoxAccess.Method)]
		public async UniTask<Texture2D> FetchTexture(string url, UnityWebRequest req = null, Action<float, ulong> progress = null, CancellationToken token = default) {
			if (string.IsNullOrEmpty(url))
				return null;

			Logger.Log($"Fetching [TEXTURE] {url}...", tag: "Network");
			var request = _activeRequests.FirstOrDefault(r => r.url == url);

			if (request == null) {
				request = RequestExtension.To(url);
				request.downloadHandler = new DownloadHandlerTexture();
				_activeRequests.Add(request);
				request.HandleDownloadProgress(progress, token);
				await request.Send(token);
				_activeRequests.Remove(request);
			}
			else {
				request.HandleDownloadProgress(progress, token);
				await request.Wait(token);
			}

			return request.Ok()
				? (request.downloadHandler as DownloadHandlerTexture)?.texture
				: null;
		}

		[NoxPublic(NoxAccess.Method)]
		public async UniTask<string> DownloadFile(string url, string hash, UnityWebRequest req = null, Action<float, ulong> progress = null, CancellationToken token = default) {
			if (string.IsNullOrEmpty(url) || string.IsNullOrEmpty(hash)) {
				Logger.LogWarning("URL or hash is null or empty.", tag: "Network");
				return null;
			}

			var tmp = Path.Combine(Application.temporaryCachePath, Guid.NewGuid().ToString());

			Logger.Log($"Fetching [FILE] {url}...", tag: "Network");
			var request = _activeRequests.FirstOrDefault(r => r.url == url);

			if (request == null) {
				request = RequestExtension.To(url);
				request.downloadHandler = new DownloadHandlerFile(tmp);
				_activeRequests.Add(request);
				request.HandleDownloadProgress(progress, token);
				await request.Send(token);
				_activeRequests.Remove(request);
			}
			else {
				request.HandleDownloadProgress(progress, token);
				await request.Wait(token);
			}

			if (!request.Ok()) {
				Logger.LogWarning($"Failed to download file from {url}.", tag: "Network");
				if (File.Exists(tmp))
					File.Delete(tmp);
				return null;
			}

			if (Hashing.HashFile(tmp) != hash) {
				Logger.LogWarning($"Hash mismatch for file downloaded from {url}.", tag: "Network");
				if (File.Exists(tmp))
					File.Delete(tmp);
				return null;
			}

			return tmp;
		}
	}
}