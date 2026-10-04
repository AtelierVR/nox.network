using System;
using Newtonsoft.Json;
using Nox.Network.Assets;

namespace Nox.CCK.Network.Assets {
	/// <summary>Free-form file attribute (<c>{ "key", "value" }</c>).</summary>
	[Serializable]
	public class AssetAttribute : IAssetAttribute {
		[JsonProperty("key")]
		public string Key { get; set; }

		[JsonProperty("value")]
		public string Value { get; set; }

		public AssetAttribute() { }

		public AssetAttribute(string key, string value) {
			Key   = key;
			Value = value;
		}

		public override string ToString()
			=> $"{Key}={Value}";
	}
}
