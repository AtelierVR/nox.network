using System;
using Newtonsoft.Json;

namespace Nox.CCK.Network.Assets {
	/// <summary>
	/// Body of <c>PATCH /{endpoint}/{asset}/images/{index}</c>: the metadata of an image the asset
	/// already carries (higher <see cref="Priority"/> is picked first, <see cref="Alt"/> is its
	/// alternative text).
	/// </summary>
	[Serializable]
	public class AssetImageUpdateRequest {
		[JsonProperty("priority")]
		public int Priority { get; set; }

		[JsonProperty("alt")]
		public string Alt { get; set; }
	}
}
