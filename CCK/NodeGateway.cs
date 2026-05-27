using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using Nox.CCK.Convertors;
using Nox.CCK.Utils;
using UnityEngine;
using UnityEngine.Networking;
using Logger = Nox.CCK.Utils.Logger;

namespace Nox.CCK.Network {
	public class DiscoveredGateway {
		public string GatewayUrl;
		public DateTime ExpiresAt;
		public NoxWellKnown WellKnown;
	}

	public static class NodeGateway {
		private const string WellKnownPath = "/.well-known/nox";
		private const string NodeInfoPath = "/.well-known/nodeinfo";
		private const string NoxNodeInfoRel = "nox/1.0";
		private static readonly TimeSpan FallbackTtl = TimeSpan.FromMinutes(5);

		/// <summary>
		/// Discovers the API gateway for the given server address.
		/// Tries four strategies in order, returning on first success:
		///   1. DNS SRV  — _nox._tcp.&lt;address&gt;
		///   2. DNS TXT  — _nox.&lt;address&gt;, record contains ng=&lt;url&gt;
		///   3. NodeInfo — /.well-known/nodeinfo, link rel="nox/1.0"
		///   4. Manual   — /.well-known/nox directly (https then http)
		/// </summary>
		public static async UniTask<DiscoveredGateway> Discover(string address) {
			if (string.IsNullOrEmpty(address))
				return null;
			var t0 = DateTime.Now;
			var result =
				await DiscoverViaSrv(address) ??
				await DiscoverViaTxt(address) ??
				await DiscoverViaNodeInfo(address) ??
				await DiscoverManual(address);
			Logger.LogDebug($"NodeGateway.Discover: {address} => {result?.GatewayUrl ?? "null"} ({(DateTime.Now - t0).TotalSeconds:0.00}s)");
			return result;
		}

		// Strategy 1 — DNS SRV: _nox._tcp.<address>
		private static async UniTask<DiscoveredGateway> DiscoverViaSrv(string address) {
			var host = GetHost(address);
			try {
				var dnsUrl = $"https://dns.google/resolve?name=_nox._tcp.{host}&type=SRV";
				Logger.LogDebug($"NodeGateway.SRV: querying {dnsUrl}");
				var req = new UnityWebRequest(dnsUrl, UnityWebRequest.kHttpVerbGET)
					{ downloadHandler = new DownloadHandlerBuffer() };
				req.timeout = 5;
				await req.SendWebRequest();
				if (req.result != UnityWebRequest.Result.Success) {
					Logger.LogDebug($"NodeGateway.SRV: DNS request failed — {req.error}");
					return null;
				}

				Logger.LogDebug($"NodeGateway.SRV: response — {req.downloadHandler.text}");
				var dns = JsonUtility.FromJson<Txt>(req.downloadHandler.text);
				if (dns is not { Status: 0 } || dns.Answer == null || dns.Answer.Length == 0) {
					Logger.LogDebug($"NodeGateway.SRV: no usable records (status={dns?.Status}, answers={dns?.Answer?.Length ?? 0})");
					return null;
				}

				// SRV data format: "<priority> <weight> <port> <target>"
				var records = new List<SrvRecord>();
				foreach (var answer in dns.Answer) {
					var parts = answer.data.Trim('"').Split(' ');
					if (parts.Length < 4
						|| !int.TryParse(parts[0], out var priority)
						|| !int.TryParse(parts[1], out var weight)
						|| !int.TryParse(parts[2], out var port))
						continue;
					records.Add(new SrvRecord {
						Priority = priority,
						Weight   = weight,
						Port     = port,
						Target   = parts[3].TrimEnd('.')
					});
				}

				// Sort by priority↑ then weight↓
				records.Sort((a, b) => a.Priority != b.Priority ? a.Priority - b.Priority : b.Weight - a.Weight);

				foreach (var r in records)
				foreach (var scheme in new[] { "https", "http" }) {
					var discovered = await TryFetchWellKnown($"{scheme}://{r.Target}:{r.Port}{WellKnownPath}");
					if (discovered != null)
						return discovered;
				}
			} catch (Exception ex) { Logger.LogDebug($"NodeGateway.SRV: exception — {ex.Message}"); }

			Logger.LogDebug("NodeGateway.SRV: no valid SRV records found");
			return null;
		}

		// Strategy 2 — DNS TXT: _nox.<address>, looks for ng=<url>
		private static async UniTask<DiscoveredGateway> DiscoverViaTxt(string address) {
			var host = GetHost(address);
			try {
				var dnsUrl = $"https://dns.google/resolve?name=_nox.{host}&type=TXT";
				Logger.LogDebug($"NodeGateway.TXT: querying {dnsUrl}");
				var req = new UnityWebRequest(dnsUrl, UnityWebRequest.kHttpVerbGET)
					{ downloadHandler = new DownloadHandlerBuffer() };
				req.timeout = 5;
				await req.SendWebRequest();
				if (req.result != UnityWebRequest.Result.Success) {
					Logger.LogDebug($"NodeGateway.TXT: DNS request failed — {req.error}");
					return null;
				}

				Logger.LogDebug($"NodeGateway.TXT: response — {req.downloadHandler.text}");
				var dns = JsonUtility.FromJson<Txt>(req.downloadHandler.text);
				if (dns is not { Status: 0 } || dns.Answer == null || dns.Answer.Length == 0) {
					Logger.LogDebug($"NodeGateway.TXT: no usable records (status={dns?.Status}, answers={dns?.Answer?.Length ?? 0})");
					return null;
				}

				foreach (var answer in dns.Answer) {
					var line = answer.data.Trim('"');
					Logger.LogDebug($"NodeGateway.TXT: record — {line}");
					var match = Regex.Match(line, @"(?:^|[;\s])ng=([^\s;]+)");
					if (!match.Success) {
						Logger.LogDebug($"NodeGateway.TXT: no ng= in record");
						continue;
					}
					Logger.LogDebug($"NodeGateway.TXT: found ng={match.Groups[1].Value}");
					var discovered = await TryFetchWellKnown(match.Groups[1].Value);
					if (discovered != null)
						return discovered;
				}
			} catch (Exception ex) { Logger.LogDebug($"NodeGateway.TXT: exception — {ex.Message}"); }

			Logger.LogDebug("NodeGateway.TXT: no valid ng= record found in any TXT answer");
			return null;
		}

		// Strategy 3 — NodeInfo: /.well-known/nodeinfo, follow link rel="nox/1.0"
		private static async UniTask<DiscoveredGateway> DiscoverViaNodeInfo(string address) {
			foreach (var scheme in new[] { "https", "http" })
				try {
					var nodeUrl = $"{scheme}://{address}{NodeInfoPath}";
					Logger.LogDebug($"NodeGateway.NodeInfo: querying {nodeUrl}");
					var req = new UnityWebRequest(nodeUrl, UnityWebRequest.kHttpVerbGET)
						{ downloadHandler = new DownloadHandlerBuffer(), certificateHandler = new AcceptAllCertificates() };
					req.timeout = 5;
					await req.SendWebRequest();
					if (req.result != UnityWebRequest.Result.Success) {
						Logger.LogDebug($"NodeGateway.NodeInfo: failed — {req.error}");
						continue;
					}

					Logger.LogDebug($"NodeGateway.NodeInfo: response — {req.downloadHandler.text}");
					var doc = JsonUtility.FromJson<NodeInfoLinks>(req.downloadHandler.text);
					if (doc?.links == null) {
						Logger.LogDebug("NodeGateway.NodeInfo: no links in response");
						continue;
					}

					var link = doc.links.FirstOrDefault(l => l.rel == NoxNodeInfoRel);
					if (string.IsNullOrEmpty(link?.href)) {
						Logger.LogDebug($"NodeGateway.NodeInfo: no link with rel={NoxNodeInfoRel}");
						continue;
					}

					Logger.LogDebug($"NodeGateway.NodeInfo: found href={link.href}");
					var discovered = await TryFetchWellKnown(link.href);
					if (discovered != null)
						return discovered;
				} catch (Exception ex) { Logger.LogDebug($"NodeGateway.NodeInfo: exception — {ex.Message}"); }
			return null;
		}

		// Strategy 4 — Manual: /.well-known/nox directly (https then http)
		private static async UniTask<DiscoveredGateway> DiscoverManual(string address) {
			foreach (var scheme in new[] { "https", "http" }) {
				var discovered = await TryFetchWellKnown($"{scheme}://{address}{WellKnownPath}");
				if (discovered != null)
					return discovered;
			}
			return null;
		}

		/// <summary>
		/// GETs a /.well-known/nox URL, parses the NoxWellKnown JSON, and reads TTL from response headers.
		/// </summary>
		private static async UniTask<DiscoveredGateway> TryFetchWellKnown(string url) {
			try {
				Logger.LogDebug($"NodeGateway.WellKnown: fetching {url}");
				var req = new UnityWebRequest(url, UnityWebRequest.kHttpVerbGET)
					{ downloadHandler = new DownloadHandlerBuffer(), certificateHandler = new AcceptAllCertificates() };
				req.timeout = 5;
				await req.SendWebRequest();
				if (req.result != UnityWebRequest.Result.Success) {
					Logger.LogDebug($"NodeGateway.WellKnown: HTTP failed ({(int)req.responseCode}) — {req.error}");
					return null;
				}

				Logger.LogDebug($"NodeGateway.WellKnown: body — {req.downloadHandler.text}");
				var rawText = req.downloadHandler.text;
				var wk = JsonConvert.DeserializeObject<NoxWellKnown>(rawText);
				if (wk == null) {
					Logger.LogDebug("NodeGateway.WellKnown: deserialization returned null");
					return null;
				}
				if (wk.gateway == null) {
					Logger.LogDebug("NodeGateway.WellKnown: 'gateway' field is null after deserialization");
					return null;
				}
				if (!wk.gateway.TryGetValue("api", out var gatewayApi) || string.IsNullOrEmpty(gatewayApi)) {
					Logger.LogDebug($"NodeGateway.WellKnown: gateway has no 'api' key (keys: [{string.Join(", ", wk.gateway.Keys)}])");
					return null;
				}

				var ttl = ParseTtl(
					req.GetResponseHeader("Cache-Control"),
					req.GetResponseHeader("Expires"));
				Logger.LogDebug($"NodeGateway.WellKnown: success — gatewayApi={gatewayApi}, ttl={ttl}");
				return new DiscoveredGateway {
					GatewayUrl = gatewayApi,
					ExpiresAt  = DateTime.UtcNow + ttl,
					WellKnown  = wk
				};
			} catch (Exception ex) {
				Logger.LogDebug($"NodeGateway.WellKnown: exception — {ex.GetType().Name}: {ex.Message}");
				return null;
			}
		}

		private static TimeSpan ParseTtl(string cacheControl, string expires) {
			if (!string.IsNullOrEmpty(cacheControl)) {
				var m = Regex.Match(cacheControl, @"max-age=(\d+)");
				if (m.Success && int.TryParse(m.Groups[1].Value, out var s) && s > 0)
					return TimeSpan.FromSeconds(s);
			}
			if (!string.IsNullOrEmpty(expires) && DateTime.TryParse(expires, out var exp)) {
				var ttl = exp.ToUniversalTime() - DateTime.UtcNow;
				if (ttl > TimeSpan.Zero)
					return ttl;
			}
			return FallbackTtl;
		}

		/// <summary>Extracts the pure hostname from an address that may include a port.</summary>
		private static string GetHost(string address)
			=> Uri.TryCreate($"https://{address}", UriKind.Absolute, out var uri) ? uri.Host : address;

		private struct SrvRecord {
			public int Priority,
				Weight,
				Port;
			public string Target;
		}
	}

	[Serializable]
	public class NoxWellKnown {
		public string id;
		public NoxSoftware software;
		public string status;
		public double started;
		[JsonProperty("public")]
		public string publicKey;
		public string address;
		public int port;
		[JsonProperty]
		public Dictionary<string, string> gateway;
		[JsonProperty]
		public Dictionary<string, string> endpoints;
		[JsonProperty]
		public Dictionary<string, string> versions;
		public NoxMetadata metadata;
		public string[] features;
		public string[] capabilities;
		public string maintenance;
	}

	[Serializable]
	public class NoxSoftware {
		public string name;
		public string version;
	}

	[Serializable]
	public class NoxEndpoints {
		public string wellknown;
		public string webfinger;
		public string nodeinfo;
	}

	[Serializable]
	public class NoxMetadata {
		[JsonConverter(typeof(TranslatedStringConverter))]
		public TranslatedString title;

		[JsonConverter(typeof(TranslatedStringConverter))]
		public TranslatedString description;

		[JsonConverter(typeof(DictionnaryOrStringConverter), true)]
		public DictionnaryOrString icon;

		public string contact;
	}

	[Serializable]
	public class NodeInfoLinks {
		public NodeInfoLink[] links;
	}

	[Serializable]
	public class NodeInfoLink {
		public string rel;
		public string href;
	}

	// Kept for backward compatibility
	[Serializable]
	public class Txt {
		public int Status;
		public TxtAnswer[] Answer;
	}

	[Serializable]
	public class TxtAnswer {
		public string data;

		public Dictionary<string, string> ToDataDictionary()
			=> data
				.Split(';')
				.Select(item => item.Split('='))
				.Where(kv => kv.Length == 2)
				.ToDictionary(kv => kv[0].Trim(), kv => kv[1].Trim());

		public bool TryGet(string key, out string value)
			=> ToDataDictionary().TryGetValue(key, out value);
	}

	internal class AcceptAllCertificates : CertificateHandler {
		protected override bool ValidateCertificate(byte[] certificateData)
			=> true;
	}
}