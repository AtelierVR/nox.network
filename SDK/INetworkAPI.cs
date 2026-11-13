using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Nox.Network.Assets;
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
		/// <para>
		/// The download never waits: an asset file that is not analyzed yet is not served, the node
		/// answers a <c>409</c> with <c>error.code = ANALYSIS_PENDING</c>, and that is reported as a
		/// failure in the <see cref="IDownloadResult"/>. The caller decides whether to refetch the
		/// file status and retry (see <see cref="IDownloadResult.IsAnalysisPending"/>).
		/// </para>
		/// </summary>
		/// <param name="address">Download URL of the file.</param>
		/// <param name="options">
		/// Expected hash (required), progress callback. A download without a hash is refused: the
		/// bytes could not be verified.
		/// </param>
		/// <param name="token"></param>
		/// <returns>The download outcome — local path, HTTP status and envelope error code.</returns>
		public UniTask<IDownloadResult> DownloadFile(string address, IDownloadOptions options = null, CancellationToken token = default);
	}
}