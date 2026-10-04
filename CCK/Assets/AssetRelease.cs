using System;
using Newtonsoft.Json;
using Nox.CCK.Convertors;
using Nox.Network.Assets;

namespace Nox.CCK.Network.Assets {
	/// <summary>
	/// A release (version) of an asset. Title and changelog are stored as
	/// <see cref="TranslatedString"/> so both the plain string sent by the API and a
	/// multi-language object are readable.
	/// </summary>
	[Serializable]
	public class AssetRelease : IAssetRelease {
		[JsonProperty("id")]
		public uint Id { get; internal set; }

		[JsonProperty("name")]
		public string Name { get; internal set; }

		[JsonProperty("title"), JsonConverter(typeof(TranslatedStringConverter))]
		public TranslatedString Title { get; internal set; }

		[JsonProperty("changelog"), JsonConverter(typeof(TranslatedStringConverter))]
		public TranslatedString Changelog { get; internal set; }

		[JsonProperty("channel"), JsonConverter(typeof(AssetChannelConverter))]
		public AssetChannel Channel { get; internal set; }

		[JsonProperty("alias")]
		public AssetAttribute[] Alias { get; internal set; } = Array.Empty<AssetAttribute>();

		[JsonProperty("status")]
		public AssetStatus Status { get; internal set; }

		[JsonProperty("files")]
		public AssetFile[] Files { get; internal set; } = Array.Empty<AssetFile>();

		[JsonProperty("expires_at"), JsonConverter(typeof(UnixTimestampToDateTime))]
		public DateTime ExpiresAt { get; internal set; }

		[JsonProperty("refetch_at"), JsonConverter(typeof(UnixTimestampToDateTime))]
		public DateTime RefetchAt { get; internal set; }

		[JsonIgnore]
		IAssetAttribute[] IAssetRelease.Alias
			=> Alias;

		[JsonIgnore]
		IAssetStatus IAssetRelease.Status
			=> Status;

		[JsonIgnore]
		IAssetFile[] IAssetRelease.Files
			=> Files;

		public override string ToString()
			=> $"{nameof(AssetRelease)}[{Id}, {Name}, channel={Channel}, status={Status?.Status}, files={Files?.Length ?? 0}]";

		/// <summary>Reads and writes the release channel (<c>stable</c>, <c>candidate</c>, <c>nightly</c>).</summary>
		private class AssetChannelConverter : JsonConverter<AssetChannel> {
			public override void WriteJson(JsonWriter writer, AssetChannel value, JsonSerializer serializer)
				=> writer.WriteValue(value.ToValue());

			public override AssetChannel ReadJson(JsonReader reader, Type objectType, AssetChannel existingValue, bool hasExistingValue, JsonSerializer serializer)
				=> reader.TokenType == JsonToken.String
					? AssetChannelExtensions.FromString((string)reader.Value)
					: AssetChannel.Unknown;
		}
	}
}
