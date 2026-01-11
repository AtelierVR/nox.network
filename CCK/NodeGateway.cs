using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Nox.CCK.Utils;
using UnityEngine;
using UnityEngine.Networking;
using Logger = Nox.CCK.Utils.Logger;

namespace Nox.CCK.Network {
	public static class NodeGateway {
		public const ushort DEFAULT_PORT_MASTER = 53032;

		public static async UniTask<Uri> FindGatewayMaster(string address) {
			if (string.IsNullOrEmpty(address)) return null;

			// check in config
			var config = Config.Load();
			if (config.Has(new[] { "servers", address, "gateway" })) {
				var uri = config.Get<string>(new[] { "servers", address, "gateway" });
				if (uri != null) {
					Logger.LogDebug($"FindGatewayMaster: config {address} => {uri}");
					return new Uri(uri);
				}
			}

			// check with dns
			var t0 = DateTime.Now;

			var host = address.Split(':');
			var uriType = Uri.CheckHostName(host[0]);
			if (uriType is UriHostNameType.IPv4 or UriHostNameType.IPv6) {
				Logger.LogDebug($"FindGatewayMaster: IPv4/IPv6 {address}");
				var uri = new Uri($"tcp://{address}");
				if (uri.Port == -1) uri = new Uri($"tcp://{address}:{DEFAULT_PORT_MASTER}");
				var fmg = await FindGm($"{uri.Host}:{uri.Port}", true);
				return Debugger(fmg != null ? fmg : null);
			}

			if (host[0] == "localhost") {
				Logger.LogDebug($"FindGatewayMaster: localhost {address}");
				var uri = new Uri($"tcp://{address}");
				if (uri.Port == -1) uri = new Uri($"tcp://{address}:{DEFAULT_PORT_MASTER}");
				var fmg = await FindGm($"{uri.Host}:{uri.Port}", true);
				return Debugger(fmg != null ? fmg : null);
			}

			if (uriType == UriHostNameType.Dns) {
				Logger.LogDebug($"FindGatewayMaster: DNS {address}");
				var uri = new Uri($"tcp://{address}");
				if (uri.Port == -1) uri = new Uri($"tcp://{address}:{DEFAULT_PORT_MASTER}");
				var fmg = await FindGm($"{uri.Host}:{uri.Port}");
				if (fmg != null) {
					Logger.LogDebug($"{fmg.Host}:{fmg.Port} (DNS)");
					return Debugger(fmg);
				}

				return Debugger((await ResolveMasterDns(uri.Host)).FirstOrDefault());
			}

			return null;

			Uri Debugger(Uri uri) {
				var t1 = DateTime.Now - t0;
				Logger.LogDebug($"FindGatewayMaster: {address} => {uri} ({t1.TotalSeconds:0.00}s)");
				return uri;
			}
		}

		private static async UniTask<Uri[]> ResolveMasterDns(string domain) {
			Logger.LogDebug($"ResolveMasterDns: domain {domain}");
			List<Uri> uris = new();
			var url = $"https://dns.google/resolve?name=_nox.{domain}&type=TXT";
			try {
				var req = new UnityWebRequest(
					url,
					UnityWebRequest.kHttpVerbGET
				) { downloadHandler = new DownloadHandlerBuffer() };
				req.timeout = 5;
				await req.SendWebRequest();

				if (req.result == UnityWebRequest.Result.Success) {
					var txt = JsonUtility.FromJson<Txt>(req.downloadHandler.text);
					if (txt.Status != 0 || txt.Answer.Length <= 0)
						return uris.ToArray();

					foreach (var answer in txt.Answer)
						if (answer.TryGet("mg", out var gateway)) {
							var uri = new Uri(gateway);
							uris.Add(uri);
						}

					return uris.ToArray();
				}
			} catch {
				// ignored
			}

			return uris.ToArray();
		}

		private static async UniTask<Uri> FindGm(string domain, bool forceHttp = false) {
			var protos = forceHttp ? new[] { "http" } : new[] { "https", "http" };
			foreach (var protocol in protos)
				try {
					var uri = new Uri($"{protocol}://{domain}/.well-known/nox");
					var req = new UnityWebRequest(uri, UnityWebRequest.kHttpVerbGET)
						{ downloadHandler = new DownloadHandlerBuffer() };
					req.timeout = 5;
					await req.SendWebRequest();
					if (req.result == UnityWebRequest.Result.Success)
						return new Uri($"{protocol}://{domain}");
				} catch {
					// ignored
				}

			return null;
		}
	}

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
}