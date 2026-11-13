using System;
using Nox.Network.Assets;

namespace Nox.CCK.Network.Assets {
	/// <summary>
	/// Settings of a file download (<c>GET .../files/{file}</c>): expected hash and progress callback.
	/// </summary>
	public class AssetDownloadOptions : IDownloadOptions {
		/// <summary>
		/// Expected content hash, verified against the downloaded bytes (a mismatch discards the
		/// file). The download is refused when it is <c>null</c> or carries no digest.
		/// </summary>
		public IAssetHash Hash { get; set; }

		/// <summary>Receives the download ratio (<c>0</c>–<c>1</c>) and the bytes received, or <c>null</c>.</summary>
		public Action<float, ulong> Progress { get; set; }

		/// <summary>Copies any options implementation into the mutable model.</summary>
		public static AssetDownloadOptions From(IDownloadOptions options)
			=> options switch {
				null                        => new AssetDownloadOptions(),
				AssetDownloadOptions native => native,
				_ => new AssetDownloadOptions {
					Hash             = options.Hash,
					Progress = options.Progress
				}
			};

		/// <summary>
		/// Builds options from a hash value (<c>&lt;type&gt;:&lt;hex&gt;</c>,
		/// <c>&lt;type&gt;-&lt;hex&gt;</c> cache name, or a bare hexadecimal digest).
		/// </summary>
		public static AssetDownloadOptions For(string hash, Action<float, ulong> progress = null)
			=> new() {
				Hash             = AssetHash.Parse(hash),
				Progress = progress
			};
	}
}
