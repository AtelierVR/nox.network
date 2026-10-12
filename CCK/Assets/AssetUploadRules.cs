using System;
using Newtonsoft.Json;
using Nox.Network.Assets;

namespace Nox.CCK.Network.Assets {
	/// <summary>
	/// Upload rules of one asset type, as declared by the instance (<c>GET /configs</c> →
	/// <c>assets[].rules</c>).
	/// </summary>
	[Serializable]
	public class AssetUploadRules : IAssetUploadRules {
		/// <summary>Shared empty rules, returned when an asset type declares none.</summary>
		public static readonly AssetUploadRules Empty = new();

		/// <summary>Attribute keys every file of a release must declare.</summary>
		[JsonProperty("required")]
		public string[] Required { get; internal set; } = Array.Empty<string>();

		/// <summary>Attribute key groups whose combination must stay unique across a release.</summary>
		[JsonProperty("unique")]
		public string[][] Unique { get; internal set; } = Array.Empty<string[]>();

		public override string ToString()
			=> $"{nameof(AssetUploadRules)}[required={(Required?.Length ?? 0)}, unique={(Unique?.Length ?? 0)}]";
	}
}
