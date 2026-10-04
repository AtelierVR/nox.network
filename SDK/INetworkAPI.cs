using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace Nox.Network {
	public interface INetworkAPI {
		/// <summary>
		/// Fetches a texture from the given address.
		/// </summary>
		/// <param name="address"></param>
		/// <param name="req"></param>
		/// <param name="progress"></param>
		/// <param name="token"></param>
		/// <returns></returns>
		public UniTask<Texture2D> FetchTexture(string address, UnityWebRequest req = null, Action<float, ulong> progress = null, CancellationToken token = default);

		/// <summary>
		/// Downloads a file from the given address and saves it to the cache.
		/// </summary>
		/// <param name="address">Signed download URL of the file.</param>
		/// <param name="hash">
		/// Expected content hash, as declared by the asset pipeline: <c>&lt;type&gt;:&lt;hex&gt;</c>,
		/// <c>&lt;type&gt;-&lt;hex&gt;</c> (the cache file name) or a bare hexadecimal digest.
		/// </param>
		/// <param name="req"></param>
		/// <param name="progress"></param>
		/// <param name="token"></param>
		/// <returns>The downloaded file path, or <c>null</c> when it could not be verified.</returns>
		public UniTask<string> DownloadFile(string address, string hash, UnityWebRequest req = null, Action<float, ulong> progress = null, CancellationToken token = default);
	}
}