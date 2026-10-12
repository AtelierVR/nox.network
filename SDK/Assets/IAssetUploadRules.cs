namespace Nox.Network.Assets {
	/// <summary>
	/// Upload rules the instance declares for an asset type (<c>GET /configs</c>,
	/// <c>assets[].rules</c>) and enforces server-side when a file slot is reserved: the attributes
	/// every file of a release must declare, and the combinations that must stay unique across it.
	/// </summary>
	public interface IAssetUploadRules {
		/// <summary>
		/// Attribute keys every file of a release must declare. Empty when the type declares no rule.
		/// </summary>
		string[] Required { get; }

		/// <summary>
		/// Attribute key groups whose <b>combination</b> must stay unique across a release. Empty when
		/// the type declares no rule.
		/// </summary>
		string[][] Unique { get; }
	}
}
