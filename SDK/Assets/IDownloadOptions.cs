using System;

namespace Nox.Network.Assets {
	/// <summary>
	/// Settings of a file download (<c>GET /{endpoint}/{asset}/releases/{release}/files/{file}</c>).
	/// <para>
	/// The expected hash is what makes the download trustworthy: the bytes are verified against
	/// it and discarded on a mismatch, so it is required.
	/// </para>
	/// </summary>
	public interface IDownloadOptions {
		/// <summary>
		/// Expected content hash, verified against the downloaded bytes (a mismatch discards the
		/// file). The download is refused when it is <c>null</c> or carries no digest.
		/// </summary>
		IAssetHash Hash { get; set; }

		/// <summary>
		/// Receives the download ratio (<c>0</c>–<c>1</c>) and the bytes received, or <c>null</c>
		/// when the caller does not display the download progress.
		/// </summary>
		Action<float, ulong> Progress { get; set; }
	}
}
