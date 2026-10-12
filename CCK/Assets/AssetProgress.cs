using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Nox.Network.Assets;

namespace Nox.CCK.Network.Assets {
	/// <summary>
	/// Live progress of a pending file (<c>{ "current": 45, "total": 227 }</c>), or the
	/// <c>null</c> the API sends while the steps cannot be estimated.
	/// </summary>
	[Serializable]
	[JsonConverter(typeof(AssetProgressConverter))]
	public class AssetProgress : IAssetProgress {
		[JsonProperty("current")]
		public uint Current { get; internal set; }

		[JsonProperty("total")]
		public uint Total { get; internal set; }

		[JsonIgnore]
		public float Ratio
			=> Total == 0 ? 0f : Math.Min(1f, (float)Current / Total);

		public override string ToString()
			=> $"{nameof(AssetProgress)}[{Current}/{Total}]";

		private class AssetProgressConverter : JsonConverter<AssetProgress> {
			public override void WriteJson(JsonWriter writer, AssetProgress value, JsonSerializer serializer) {
				if (value == null) {
					writer.WriteNull();
					return;
				}

				writer.WriteStartObject();
				writer.WritePropertyName("current");
				writer.WriteValue(value.Current);
				writer.WritePropertyName("total");
				writer.WriteValue(value.Total);
				writer.WriteEndObject();
			}

			public override AssetProgress ReadJson(JsonReader reader, Type objectType, AssetProgress existingValue, bool hasExistingValue, JsonSerializer serializer) {
				switch (reader.TokenType) {
					case JsonToken.Null:
					case JsonToken.Undefined:
						return null;

					case JsonToken.StartObject:
						var obj = JObject.Load(reader);
						return new AssetProgress {
							Current = obj["current"]?.ToObject<uint>() ?? 0,
							Total   = obj["total"]?.ToObject<uint>() ?? 0
						};

					default:
						throw new JsonSerializationException($"Invalid token type for {nameof(AssetProgress)}: {reader.TokenType}");
				}
			}
		}
	}
}
