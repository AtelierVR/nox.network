using System;
using Newtonsoft.Json;
using Nox.CCK.Convertors;
using Nox.CCK.Utils;
using Nox.Network.Assets;

namespace Nox.CCK.Network.Assets {
	/// <summary>
	/// An asset published on a node: the shared shape behind avatars, worlds, mods, props and
	/// any other type declared with <c>@AssetController(type, endpoint)</c>.
	/// </summary>
	[Serializable]
	public class Asset : IAsset {
		[JsonProperty("type")]
		public string Type { get; internal set; }

		[JsonProperty("id")]
		public uint Id { get; internal set; }

		[JsonProperty("name")]
		public string Name { get; internal set; }

		[JsonProperty("title"), JsonConverter(typeof(TranslatedStringConverter))]
		public TranslatedString Title { get; internal set; }

		[JsonProperty("description"), JsonConverter(typeof(TranslatedStringConverter))]
		public TranslatedString Description { get; internal set; }

		[JsonProperty("tags")]
		public string[] Tags { get; internal set; } = Array.Empty<string>();

		[JsonProperty("owner"), JsonConverter(typeof(StringToIdentifierConverter))]
		public Identifier Owner { get; internal set; } = Identifier.Invalid;

		[JsonProperty("server")]
		public string Server { get; set; }

		[JsonProperty("contributors"), JsonConverter(typeof(ArrayConverter<StringToIdentifierConverter>))]
		public Identifier[] Contributors { get; internal set; } = Array.Empty<Identifier>();

		[JsonProperty("alias")]
		public AssetAttribute[] Alias { get; internal set; } = Array.Empty<AssetAttribute>();

		/// <summary>
		/// Canonical reference of the asset. The generic model carries no type: a typed model
		/// (<c>World</c>, <c>Avatar</c>, …) overrides it with its own single-character type.
		/// </summary>
		[JsonIgnore]
		public virtual Identifier Identifier
			=> new(null, Id, null, Server);

		[JsonProperty("release")]
		public AssetReleasePreference Release { get; internal set; }

		[JsonProperty("images")]
		public AssetImage[] Images { get; set; } = Array.Empty<AssetImage>();

		[JsonProperty("created_at"), JsonConverter(typeof(UnixTimestampToDateTime))]
		public DateTime CreatedAt { get; internal set; }

		[JsonProperty("updated_at"), JsonConverter(typeof(UnixTimestampToDateTime))]
		public DateTime UpdatedAt { get; internal set; }

		[JsonIgnore]
		IAssetAttribute[] IAsset.Alias
			=> Alias;

		[JsonIgnore]
		IAssetReleasePreference IAsset.Release
			=> Release;

		[JsonIgnore]
		IAssetImage[] IAsset.Images
			=> Images;

		public override string ToString()
			=> $"{nameof(Asset)}[{Id}, {Name ?? "<no-name>"}, owner={Owner}]";
	}
}
