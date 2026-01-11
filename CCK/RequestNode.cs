using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Nox.CCK.Events;
using UnityEngine.Networking;

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
		public static async UniTask<UnityWebRequest> To(string address, string path) {
			var gateway = await NodeDiscover.GetGateway(address);
			if (string.IsNullOrEmpty(gateway)) return null;
			var req = RequestExtension.To(RequestExtension.MergeUrl(gateway, path));
			req.downloadHandler = new DownloadHandlerBuffer();
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