using System;
using Newtonsoft.Json;
using Nox.Network.Assets;

namespace Nox.CCK.Network.Assets {
	/// <summary>
	/// Public instance configuration (<c>GET /configs</c>), restricted to what the asset pipeline
	/// needs: the asset kinds served by the instance and the upload rules each one declares.
	/// </summary>
	[Serializable]
	public class InstanceConfig {
		[JsonProperty("assets")]
		public AssetConfigEntry[] Assets { get; internal set; } = Array.Empty<AssetConfigEntry>();

		/// <summary>
		/// Upload rules declared for the asset type served by <paramref name="endpoint"/>, or empty
		/// rules when the type is unknown or declares none.
		/// </summary>
		public IAssetUploadRules RulesFor(AssetEndpoint endpoint)
			=> Find(endpoint)?.Rules ?? AssetUploadRules.Empty;

		/// <summary>Entry describing <paramref name="endpoint"/>, or <c>null</c> when it is unknown.</summary>
		public AssetConfigEntry Find(AssetEndpoint endpoint) {
			if (Assets is not { Length: > 0 } || !endpoint.IsValid)
				return null;

			// The endpoint is the routing key: it is matched first, then the logical type.
			foreach (var asset in Assets)
				if (asset != null && string.Equals(asset.Endpoint, endpoint.Endpoint, StringComparison.OrdinalIgnoreCase))
					return asset;

			foreach (var asset in Assets)
				if (asset != null && string.Equals(asset.Type, endpoint.Type, StringComparison.OrdinalIgnoreCase))
					return asset;

			return null;
		}
	}

	/// <summary>One asset kind served by the instance: its logical type, Nox identifier key, endpoint and rules.</summary>
	[Serializable]
	public class AssetConfigEntry {
		/// <summary>Logical asset type expected by the server-side validator (<c>world</c>, <c>avatar</c>, <c>mod</c>).</summary>
		[JsonProperty("type")]
		public string Type { get; internal set; }

		/// <summary>Single-character Nox identifier key (<c>w</c>, <c>a</c>, <c>m</c>).</summary>
		[JsonProperty("key")]
		public string Key { get; internal set; }

		/// <summary>REST endpoint holding the collection (<c>worlds</c>, <c>avatars</c>, <c>mods</c>).</summary>
		[JsonProperty("endpoint")]
		public string Endpoint { get; internal set; }

		/// <summary>Upload rules declared for this type.</summary>
		[JsonProperty("rules")]
		public AssetUploadRules Rules { get; internal set; }

		public override string ToString()
			=> $"{nameof(AssetConfigEntry)}[{Type}@{Endpoint}]";
	}
}
