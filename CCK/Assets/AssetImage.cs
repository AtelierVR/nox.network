using System;
using Newtonsoft.Json;
using Nox.Network.Assets;

namespace Nox.CCK.Network.Assets {
	/// <summary>Image attached to an asset (cover, gallery, …).</summary>
	[Serializable]
	public class AssetImage : IAssetImage {
		public AssetImage() { }

		public AssetImage(string url, int priority = 0) {
			Url      = url;
			Priority = priority;
		}

		[JsonProperty("url")]
		public string Url { get; internal set; }

		[JsonProperty("priority")]
		public int Priority { get; internal set; }

		[JsonProperty("width")]
		public int Width { get; internal set; }

		[JsonProperty("height")]
		public int Height { get; internal set; }

		[JsonProperty("alt")]
		public string Alt { get; internal set; }

		public override string ToString()
			=> $"{nameof(AssetImage)}[{Width}x{Height}]";
	}
}
