namespace Nox.Network.Assets {
	/// <summary>
	/// A content hash kept with its algorithm, as exchanged by the API in the
	/// <c>&lt;type&gt;:&lt;hex&gt;</c> form (e.g. <c>sha256:0123…</c>).
	/// </summary>
	public interface IAssetHash {
		/// <summary>Algorithm declared before the colon (<c>sha256</c>, <c>md5</c>, …), or <c>null</c>.</summary>
		string Type { get; }

		/// <summary>Raw digest decoded from the hexadecimal payload.</summary>
		byte[] Digest { get; }

		/// <summary>Lowercase hexadecimal digest, or <c>null</c>.</summary>
		string Hex { get; }

		/// <summary>
		/// Canonical <c>&lt;type&gt;:&lt;hex&gt;</c> value — the form sent in the
		/// <c>Content-Hash</c> header — or the bare hex when the value carried no algorithm.
		/// </summary>
		string Value { get; }
	}
}
