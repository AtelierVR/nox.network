namespace Nox.Network.Assets {
	/// <summary>
	/// Optional headers sent with the raw upload (<c>PUT {slot.Url}</c>).
	/// <c>Content-Length</c> is always computed by the client and is not configurable here.
	/// </summary>
	public interface IAssetUploadOptions {
		/// <summary>
		/// Content hash sent as <c>Content-Hash: &lt;type&gt;:&lt;hex&gt;</c> and checked
		/// server-side. When <c>null</c> (or empty) the client computes the SHA-256 of the file;
		/// a hash carrying no algorithm is sent as the pipeline default (<c>sha256</c>).
		/// </summary>
		IAssetHash Hash { get; set; }

		/// <summary>Expected size in bytes, or <c>0</c> when unknown (only used to warn on a mismatch).</summary>
		long Length { get; set; }

		/// <summary>
		/// Full <c>Content-Signature</c> envelope sent as-is (e.g. <c>ed25519:MEQC…</c>),
		/// or <c>null</c>.
		/// </summary>
		string Signature { get; set; }

		/// <summary>Overrides the slot name through <c>Content-Disposition</c>.</summary>
		string Name { get; set; }

		/// <summary>Overrides the upload <c>Content-Type</c>.</summary>
		string Mime { get; set; }
	}
}
