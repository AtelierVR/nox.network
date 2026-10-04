namespace Nox.Network.Assets {
	/// <summary>
	/// Body of <c>POST /{endpoint}/{asset}/releases/{release}/files</c>: reserves a slot and
	/// returns it with the upload URL to <c>PUT</c> the bytes to.
	/// </summary>
	public interface IAssetFileReservation {
		/// <summary>File name, unique within the release. When omitted, the server falls back to
		/// the <c>Content-Disposition</c> header sent with the upload.</summary>
		string Name { get; set; }

		/// <summary>Slot MIME type. When omitted the effective MIME comes from the upload
		/// <c>Content-Type</c>, defaulting to <c>application/octet-stream</c>.</summary>
		string Mime { get; set; }

		IAssetAttribute[] Attributes { get; set; }
	}
}
