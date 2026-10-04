using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Nox.CCK.Events;
using Nox.CCK.Utils;
using UnityEngine.Networking;
using static Nox.CCK.Network.RequestExtension;

namespace Nox.CCK.Network {
	/// <summary>
	/// Helper class to make requests to a node's API.
	/// </summary>
	public static class RequestNode {
		/// <summary>
		/// Event invoked when a UnityWebRequest to a node's gateway is created.
		/// </summary>
		public static readonly NoxEventAsync<string, Request> OnCreated = new();

		/// <summary>
		/// Creates a UnityWebRequest to a node's gateway for the given address and path.
		/// </summary>
		/// <param name="address"></param>
		/// <param name="path"></param>
		/// <returns></returns>
		/// <exception cref="Exception"></exception>
		public static async UniTask<UnityWebRequest> To(string address, string path, string method = Method.GET) {
			if (string.IsNullOrEmpty(address))
				return null;
			var gateway = await NodeDiscover.GetGateway(address);
			if (string.IsNullOrEmpty(gateway))
				return null;
			return await Create(address, MergeUrl(gateway, path), method);
		}

		/// <summary>
		/// Creates a request to an absolute URL. URLs served by the node gateway are rebuilt from
		/// it (keeping the discovery cache and the session token); any other URL is used as-is.
		/// </summary>
		/// <param name="address"></param>
		/// <param name="url"></param>
		/// <param name="method"></param>
		/// <returns></returns>
		public static async UniTask<UnityWebRequest> ToUrl(string address, string url, string method = Method.GET) {
			if (string.IsNullOrEmpty(url))
				return null;

			var gateway = string.IsNullOrEmpty(address) ? null : await NodeDiscover.GetGateway(address);
			var target  = url;

			if (!string.IsNullOrEmpty(gateway)) {
				var prefix = gateway.EndsWith("/") ? gateway : gateway + "/";

				if (url.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
					target = "/" + url.Substring(prefix.Length);
				else if (url.StartsWith(gateway, StringComparison.OrdinalIgnoreCase))
					target = url.Substring(gateway.Length);

				target = MergeUrl(gateway, target);
			}

			return await Create(address, target, method);
		}

		/// <summary>
		/// Session token formatted for the <c>Authorization</c> header, or <c>null</c> when the
		/// server has no stored session.
		/// </summary>
		public static string GetAuthorization(string address) {
			if (string.IsNullOrEmpty(address))
				return null;
			var token = Config.Load().Get<string>(new[] { "servers", address, "_token" });
			return string.IsNullOrEmpty(token) ? null : $"Bearer {token}";
		}

		private static async UniTask<UnityWebRequest> Create(string address, string url, string method) {
			var req = RequestExtension.To(url, method);
			req.downloadHandler = new DownloadHandlerBuffer();
			var authorization = GetAuthorization(address);
			if (!string.IsNullOrEmpty(authorization))
				req.SetRequestHeader("Authorization", authorization);
			await OnCreated.InvokeAsync(address, req);
			return req;
		}

		/// <summary>
		/// Parses a NodeResponse from a Response.
		/// </summary>
		/// <param name="response"></param>
		/// <param name="token"></param>
		/// <typeparam name="T"></typeparam>
		/// <returns></returns>
		public static async UniTask<NodeResponse<T>> Node<T>(this UnityWebRequest response, CancellationToken token = default)
			=> await response.Json<NodeResponse<T>>(token);
	}
}