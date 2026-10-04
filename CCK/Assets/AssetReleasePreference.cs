using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Nox.Network.Assets;

namespace Nox.CCK.Network.Assets {
	/// <summary>
	/// Preferred release of an asset. The API sends either a bare release id (public view) or the
	/// expanded <c>{ auto, value, resolved }</c> object returned with <c>?full=1</c>;
	/// <c>-1</c> is the "no release" sentinel.
	/// </summary>
	[Serializable]
	[JsonConverter(typeof(AssetReleasePreferenceConverter))]
	public class AssetReleasePreference : IAssetReleasePreference {
		/// <summary>Sentinel used by the API when there is no release.</summary>
		public const int None = -1;

		[JsonIgnore]
		public bool Auto { get; internal set; }

		[JsonIgnore]
		public int Value { get; internal set; } = None;

		[JsonIgnore]
		public int Resolved { get; internal set; } = None;

		public override string ToString()
			=> $"{nameof(AssetReleasePreference)}[auto={Auto}, value={Value}, resolved={Resolved}]";

		private class AssetReleasePreferenceConverter : JsonConverter<AssetReleasePreference> {
			public override void WriteJson(JsonWriter writer, AssetReleasePreference value, JsonSerializer serializer) {
				if (value == null) {
					writer.WriteNull();
					return;
				}

				// Collapsed form: a named, pinned release is just its id.
				if (!value.Auto && value.Value > 0 && value.Value == value.Resolved) {
					writer.WriteValue(value.Value);
					return;
				}

				writer.WriteStartObject();
				writer.WritePropertyName("auto");
				writer.WriteValue(value.Auto);
				writer.WritePropertyName("value");
				writer.WriteValue(value.Value);
				writer.WritePropertyName("resolved");
				writer.WriteValue(value.Resolved);
				writer.WriteEndObject();
			}

			public override AssetReleasePreference ReadJson(JsonReader reader, Type objectType, AssetReleasePreference existingValue, bool hasExistingValue, JsonSerializer serializer) {
				switch (reader.TokenType) {
					case JsonToken.Null:
					case JsonToken.Undefined:
						return null;

					case JsonToken.Integer:
					case JsonToken.Float:
						var id = Convert.ToInt32(Convert.ToDouble(reader.Value));
						return new AssetReleasePreference { Auto = false, Value = id, Resolved = id };

					case JsonToken.StartObject:
						var obj = JObject.Load(reader);
						return new AssetReleasePreference {
							Auto     = obj["auto"]?.ToObject<bool>() ?? false,
							Value    = ReadId(obj["value"], None),
							Resolved = ReadId(obj["resolved"], None)
						};

					default:
						throw new JsonSerializationException($"Invalid token type for {nameof(AssetReleasePreference)}: {reader.TokenType}");
				}
			}

			private static int ReadId(JToken token, int fallback)
				=> token == null || token.Type is JTokenType.Null or JTokenType.Undefined
					? fallback
					: token.ToObject<int>();
		}
	}
}
