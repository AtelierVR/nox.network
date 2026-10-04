using Nox.Network.Assets;

namespace Nox.CCK.Network.Assets {
	/// <summary>Optional headers sent with the raw upload (<c>PUT {slot.Url}</c>).</summary>
	public class AssetUploadOptions : IAssetUploadOptions {
		/// <summary>
		/// Content hash sent as <c>Content-Hash: &lt;type&gt;:&lt;hex&gt;</c>. Computed (SHA-256)
		/// when <c>null</c> or empty; a hash carrying no algorithm is sent as the default one.
		/// </summary>
		public IAssetHash Hash { get; set; }

		/// <summary>Expected size in bytes, or <c>0</c> when unknown.</summary>
		public long Length { get; set; }

		/// <summary>Full <c>Content-Signature</c> envelope (<c>ed25519:MEQC…</c>), or <c>null</c>.</summary>
		public string Signature { get; set; }

		/// <summary>Slot name override sent through <c>Content-Disposition</c>.</summary>
		public string Name { get; set; }

		/// <summary>Upload <c>Content-Type</c> override.</summary>
		public string Mime { get; set; }

		/// <summary>Copies any options implementation into the mutable model.</summary>
		public static AssetUploadOptions From(IAssetUploadOptions options)
			=> options == null
				? new AssetUploadOptions()
				: options as AssetUploadOptions ?? new AssetUploadOptions {
					Hash      = options.Hash,
					Length    = options.Length,
					Signature = options.Signature,
					Name      = options.Name,
					Mime      = options.Mime
				};
	}
}
