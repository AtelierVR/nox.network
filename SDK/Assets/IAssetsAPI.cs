using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Nox.Network.Assets {
	/// <summary>
	/// Generic client for the Nox asset pipeline: it drives the same routes for every asset type
	/// (avatars, worlds, mods, props, …) declared with <c>@AssetController(type, endpoint)</c>,
	/// and caches downloaded files by hash.
	///
	/// Upload flow (per variant):
	/// <list type="number">
	/// <item><description><see cref="Create"/> — <c>PUT /{endpoint}</c></description></item>
	/// <item><description><see cref="CreateRelease"/> — <c>PUT /{endpoint}/{asset}/releases</c></description></item>
	/// <item><description><see cref="ReserveFile"/> — <c>POST …/releases/{release}/files</c></description></item>
	/// <item><description><see cref="UploadFile"/> — <c>PUT {slot.Url}</c>, or <see cref="Upload"/> for both steps</description></item>
	/// <item><description><see cref="FetchFile"/> — poll <c>…/files/{file}/status</c> until <c>completed</c>/<c>failed</c></description></item>
	/// </list>
	/// </summary>
	public interface IAssetsAPI {
		/// <summary>Local file cache shared by every asset type.</summary>
		IAssetCache Cache { get; }

		/// <summary>Searches the collection (<c>GET /{endpoint}</c>).</summary>
		UniTask<IAssetSearchResponse> Search(
			string server,
			AssetEndpoint endpoint,
			IAssetSearchRequest request = null,
			CancellationToken token = default
		);

		/// <summary>
		/// Searches the collection and deserializes the items as the model of the asset type
		/// (<c>AssetSearchResponse&lt;Mod&gt;</c>, …) instead of the generic <see cref="Asset"/>.
		/// </summary>
		UniTask<TResponse> Search<TResponse>(
			string server,
			AssetEndpoint endpoint,
			IAssetSearchRequest request = null,
			CancellationToken token = default
		) where TResponse : IAssetSearchResponse;

		/// <summary>Fetches a single asset (<c>GET /{endpoint}/{asset}</c>).</summary>
		/// <param name="full">Expands <c>url</c> and the release preference (adds <c>?full=1</c>).</param>
		UniTask<IAsset> Fetch(
			string server,
			AssetEndpoint endpoint,
			string asset,
			bool full = false,
			CancellationToken token = default
		);

		/// <summary>
		/// Fetches a single asset and deserializes it as the model of the asset type
		/// (<c>World</c>, <c>Mod</c>, …) instead of the generic <see cref="Asset"/>.
		/// </summary>
		/// <param name="full">Expands <c>url</c> and the release preference (adds <c>?full=1</c>).</param>
		UniTask<TAsset> Fetch<TAsset>(
			string server,
			AssetEndpoint endpoint,
			string asset,
			bool full = false,
			CancellationToken token = default
		) where TAsset : IAsset;

		/// <summary>Fetches a single release (<c>GET /{endpoint}/{asset}/releases/{release}</c>).</summary>
		UniTask<IAssetRelease> FetchRelease(
			string server,
			AssetEndpoint endpoint,
			string asset,
			string release,
			bool full = false,
			CancellationToken token = default
		);

		/// <summary>
		/// Fetches the release the asset resolves to, resolving <c>auto</c> to the newest release
		/// (<c>GET /{endpoint}/{asset}?full=1</c> then <c>GET …/releases/{id}</c>).
		/// </summary>
		UniTask<IAssetRelease> FetchPreferredRelease(
			string server,
			AssetEndpoint endpoint,
			string asset,
			CancellationToken token = default
		);

		/// <summary>Reads a file slot without streaming its bytes (<c>GET …/files/{file}/status</c>).</summary>
		UniTask<IAssetFile> FetchFile(
			string server,
			AssetEndpoint endpoint,
			string asset,
			string release,
			string file,
			CancellationToken token = default
		);

		/// <summary>
		/// Lists the files of a release (<c>GET /{endpoint}/{asset}/releases/{release}/files</c>),
		/// walking the pages until <c>total</c> is reached. A release never embeds its files, so this
		/// is the route used to read the variants already published — typically to resolve the
		/// instance's <c>unique</c> rules before reserving a new slot.
		/// </summary>
		UniTask<IAssetFile[]> FetchFiles(
			string server,
			AssetEndpoint endpoint,
			string asset,
			string release,
			CancellationToken token = default
		);

		/// <summary>
		/// Reads the upload rules the instance declares for the asset type served by
		/// <paramref name="endpoint"/> (<c>GET /configs</c> → <c>assets[].rules</c>). Returns
		/// <c>null</c> when the config could not be read; a type that declares no rule yields empty
		/// rules (never <c>null</c>).
		/// </summary>
		UniTask<IAssetUploadRules> FetchRules(
			string server,
			AssetEndpoint endpoint,
			CancellationToken token = default
		);

		/// <summary>Creates the asset (<c>PUT /{endpoint}</c>).</summary>
		UniTask<IAsset> Create(
			string server,
			AssetEndpoint endpoint,
			IAssetCreateRequest request,
			CancellationToken token = default
		);

		/// <summary>
		/// Creates the asset and deserializes the reply as the model of the asset type
		/// (<c>WorldCreateRequest</c> with a <c>World</c>, …).
		/// </summary>
		UniTask<TAsset> Create<TAsset>(
			string server,
			AssetEndpoint endpoint,
			IAssetCreateRequest request,
			CancellationToken token = default
		) where TAsset : IAsset;

		/// <summary>Patches the asset metadata (<c>PATCH /{endpoint}/{asset}</c>).</summary>
		UniTask<IAsset> Update(
			string server,
			AssetEndpoint endpoint,
			string asset,
			IAssetUpdateRequest request,
			CancellationToken token = default
		);

		/// <summary>
		/// Patches the asset metadata and deserializes the reply as the model of the asset type.
		/// </summary>
		UniTask<TAsset> Update<TAsset>(
			string server,
			AssetEndpoint endpoint,
			string asset,
			IAssetUpdateRequest request,
			CancellationToken token = default
		) where TAsset : IAsset;

		/// <summary>Deletes the asset with its releases, files and images (<c>DELETE /{endpoint}/{asset}</c>).</summary>
		UniTask<bool> Delete(
			string server,
			AssetEndpoint endpoint,
			string asset,
			CancellationToken token = default
		);

		/// <summary>Creates a release (<c>PUT /{endpoint}/{asset}/releases</c>).</summary>
		UniTask<IAssetRelease> CreateRelease(
			string server,
			AssetEndpoint endpoint,
			string asset,
			IAssetReleaseRequest request,
			CancellationToken token = default
		);

		/// <summary>Patches a release (<c>PATCH /{endpoint}/{asset}/releases/{release}</c>).</summary>
		UniTask<IAssetRelease> UpdateRelease(
			string server,
			AssetEndpoint endpoint,
			string asset,
			string release,
			IAssetReleaseRequest request,
			CancellationToken token = default
		);

		/// <summary>Deletes a release and its files (<c>DELETE /{endpoint}/{asset}/releases/{release}</c>).</summary>
		UniTask<bool> DeleteRelease(
			string server,
			AssetEndpoint endpoint,
			string asset,
			string release,
			CancellationToken token = default
		);

		/// <summary>
		/// Reserves a file slot and returns it with its upload URL
		/// (<c>POST /{endpoint}/{asset}/releases/{release}/files</c>).
		/// </summary>
		UniTask<IAssetFile> ReserveFile(
			string server,
			AssetEndpoint endpoint,
			string asset,
			string release,
			IAssetFileReservation reservation = null,
			CancellationToken token = default
		);

		/// <summary>
		/// Streams the file bytes into a reserved slot (<c>PUT {slot.Url}</c>). The server receives
		/// and verifies the bytes, answers <c>queued</c> immediately and runs the bundle analysis
		/// out of band, so the returned file is usually still pending: poll it through
		/// <see cref="FetchFile"/> while <c>Status</c> is <c>awaiting</c>/<c>queued</c>/<c>processing</c>
		/// (use <c>RefetchAt</c> as the delay) until it settles on <c>completed</c> or <c>failed</c>.
		/// A failed file keeps its slot and can be retried by re-uploading to the same slot until
		/// it expires.
		/// </summary>
		UniTask<IAssetFile> UploadFile(
			string server,
			AssetEndpoint endpoint,
			IAssetFile slot,
			string filePath,
			IAssetUploadOptions options = null,
			Action<float, ulong> progress = null,
			CancellationToken token = default
		);

		/// <summary>Reserves a slot then uploads the file (<c>POST</c> then <c>PUT</c>).</summary>
		UniTask<IAssetFile> Upload(
			string server,
			AssetEndpoint endpoint,
			string asset,
			string release,
			string filePath,
			IAssetFileReservation reservation = null,
			IAssetUploadOptions options = null,
			Action<float, ulong> progress = null,
			CancellationToken token = default
		);

		/// <summary>Deletes a file (<c>DELETE …/files/{file}</c>).</summary>
		UniTask<bool> DeleteFile(
			string server,
			AssetEndpoint endpoint,
			string asset,
			string release,
			string file,
			CancellationToken token = default
		);

		/// <summary>
		/// Downloads a file into the cache and returns its local path
		/// (<c>GET /{endpoint}/{asset}/releases/{release}/files/{file}</c>).
		/// The cache is consulted first using the hash reported by the slot status.
		/// </summary>
		UniTask<string> Download(
			string server,
			AssetEndpoint endpoint,
			string asset,
			string release,
			string file,
			Action<float, ulong> progress = null,
			CancellationToken token = default
		);

		/// <summary>Downloads an arbitrary URL (thumbnail, image, …) into the shared cache.</summary>
		UniTask<string> DownloadUrl(
			string url,
			IAssetHash hash = null,
			Action<float, ulong> progress = null,
			CancellationToken token = default
		);

		/// <summary>Adds an image to the asset (<c>POST /{endpoint}/{asset}/images</c>).</summary>
		UniTask<IAsset> AddImage(
			string server,
			AssetEndpoint endpoint,
			string asset,
			string filePath,
			Action<float, ulong> progress = null,
			CancellationToken token = default
		);

		/// <summary>Removes the image at the given index (<c>DELETE /{endpoint}/{asset}/images/{index}</c>).</summary>
		UniTask<IAsset> RemoveImage(
			string server,
			AssetEndpoint endpoint,
			string asset,
			int index,
			CancellationToken token = default
		);
	}
}
