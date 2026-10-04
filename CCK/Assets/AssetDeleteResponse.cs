using System;
using Newtonsoft.Json;

namespace Nox.CCK.Network.Assets {
	/// <summary>Body of the cascading delete routes: <c>{ "success": true }</c>.</summary>
	[Serializable]
	public class AssetDeleteResponse {
		[JsonProperty("success")]
		public bool Success { get; internal set; }

		public override string ToString()
			=> $"{nameof(AssetDeleteResponse)}[success={Success}]";
	}
}
