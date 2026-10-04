using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Nox.CCK.Convertors;
using Nox.CCK.Utils;
using Nox.Network.Assets;

namespace Nox.CCK.Network.Assets {
	/// <summary>
	/// A single file (slot) of a release. While the slot is pending, <see cref="Url"/> is the
	/// upload URL; once completed it switches to the storage/download URL.
	/// </summary>
	[Serializable]
	public class AssetFile : IAssetFile {
		[JsonProperty("name")]
		public string Name { get; internal set; }

		[JsonProperty("mime")]
		public string Mime { get; internal set; }

		[JsonProperty("attributes")]
		public AssetAttribute[] Attributes { get; internal set; } = Array.Empty<AssetAttribute>();

		[JsonProperty("alias")]
		public AssetAttribute[] Alias { get; internal set; } = Array.Empty<AssetAttribute>();

		[JsonProperty("status")]
		public AssetStatus Status { get; internal set; }

		[JsonProperty("url"), JsonConverter(typeof(AssetUrlConverter))]
		public string Url { get; internal set; }

		[JsonProperty("size")]
		public long Size { get; internal set; }

		[JsonProperty("hashs")]
		public AssetHash[] Hashs { get; internal set; } = Array.Empty<AssetHash>();

		[JsonProperty("uploader"), JsonConverter(typeof(StringToIdentifierConverter))]
		public Identifier Uploader { get; internal set; } = Identifier.Invalid;

		[JsonProperty("analysis")]
		public AssetAnalysis Analysis { get; internal set; }

		[JsonProperty("signature")]
		public string Signature { get; internal set; }

		[JsonProperty("created_at"), JsonConverter(typeof(UnixTimestampToDateTime))]
		public DateTime CreatedAt { get; internal set; }

		[JsonProperty("updated_at"), JsonConverter(typeof(UnixTimestampToDateTime))]
		public DateTime UpdatedAt { get; internal set; }

		[JsonProperty("expires_at"), JsonConverter(typeof(UnixTimestampToDateTime))]
		public DateTime ExpiresAt { get; internal set; }

		[JsonProperty("refetch_at"), JsonConverter(typeof(UnixTimestampToDateTime))]
		public DateTime RefetchAt { get; internal set; }

		[JsonIgnore]
		IAssetAttribute[] IAssetFile.Attributes
			=> Attributes;

		[JsonIgnore]
		IAssetAttribute[] IAssetFile.Alias
			=> Alias;

		[JsonIgnore]
		IAssetStatus IAssetFile.Status
			=> Status;

		[JsonIgnore]
		IAssetAnalysis IAssetFile.Analysis
			=> Analysis;

		/// <summary>First declared hash, or <c>null</c>.</summary>
		[JsonIgnore]
		public AssetHash Hash
			=> Hashs is { Length: > 0 } ? Hashs[0] : null;

		[JsonIgnore]
		IAssetHash[] IAssetFile.Hashs
			=> Hashs;

		public override string ToString()
			=> $"{nameof(AssetFile)}[{Name}, status={Status?.Status}, size={Size}]";

		/// <summary>Reads <c>url</c> as a plain string or as the <c>{ proxified, original }</c> object.</summary>
		private class AssetUrlConverter : JsonConverter<string> {
			public override void WriteJson(JsonWriter writer, string value, JsonSerializer serializer)
				=> writer.WriteValue(value);

			public override string ReadJson(JsonReader reader, Type objectType, string existingValue, bool hasExistingValue, JsonSerializer serializer) {
				switch (reader.TokenType) {
					case JsonToken.Null:
					case JsonToken.Undefined:
						return null;

					case JsonToken.String:
						return (string)reader.Value;

					case JsonToken.StartObject:
						var obj = JObject.Load(reader);
						return obj["proxified"]?.ToString() ?? obj["original"]?.ToString();

					default:
						throw new JsonSerializationException($"Invalid token type for a url: {reader.TokenType}");
				}
			}
		}
	}
}
