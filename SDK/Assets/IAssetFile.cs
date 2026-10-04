using System;
using Nox.CCK.Utils;

namespace Nox.Network.Assets {
	/// <summary>
	/// A single file (slot) of a release. While the slot is pending its <see cref="Url"/> is the
	/// <b>upload</b> URL; once completed it switches to the storage/download URL. Files are
	/// immutable: they are only created (reserve + upload) or deleted.
	/// </summary>
	public interface IAssetFile {
		/// <summary>File name, unique within the release, or <c>null</c> for an unnamed slot.</summary>
		string Name { get; }

		string Mime { get; }

		IAssetAttribute[] Attributes { get; }

		/// <summary>Alternative references of the file, e.g. <c>{ key: "api", value: … }</c>.</summary>
		IAssetAttribute[] Alias { get; }

		IAssetStatus Status { get; }

		/// <summary>
		/// Upload URL while the slot is pending, download URL once completed. With <c>?full=1</c>
		/// the API sends <c>{ proxified, original }</c>: the proxified URL is exposed here.
		/// </summary>
		string Url { get; }

		/// <summary>File size in bytes, <c>0</c> until the bytes have been received.</summary>
		long Size { get; }

		/// <summary>
		/// Content hashes, kept with their algorithm (<c>sha256:0123…</c>). Empty until computed.
		/// </summary>
		IAssetHash[] Hashs { get; }

		/// <summary>Uploader reference, or <see cref="Identifier.Invalid"/>.</summary>
		Identifier Uploader { get; }

		IAssetAnalysis Analysis { get; }

		/// <summary>Detached signature over the file hash (<c>ed25519:…</c>), or <c>null</c>.</summary>
		string Signature { get; }

		DateTime CreatedAt { get; }

		DateTime UpdatedAt { get; }

		/// <summary>
		/// Expiration of an incomplete slot, or <c>default</c> (<see cref="DateTime.MinValue"/>)
		/// once the file is completed (the API omits the field).
		/// </summary>
		DateTime ExpiresAt { get; }

		/// <summary>
		/// When the resource is pending, the time after which the client should GET it again.
		/// <c>default</c> while the resource is stable (the API omits the field).
		/// </summary>
		DateTime RefetchAt { get; }
	}
}
