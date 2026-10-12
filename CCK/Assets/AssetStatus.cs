using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Nox.Network.Assets;

namespace Nox.CCK.Network.Assets {
	/// <summary>
	/// Status of a file or release slot. The API sends either a plain string
	/// (<c>"completed"</c>) or an object
	/// (<c>{ "status": "processing", "progress": { "current": 45, "total": 227 }, "message": "…" }</c>),
	/// so the converter normalizes both shapes.
	/// </summary>
	[Serializable]
	[JsonConverter(typeof(AssetStatusConverter))]
	public class AssetStatus : IAssetStatus {
		[JsonProperty("status")]
		public AssetState Status { get; internal set; }

		[JsonProperty("progress")]
		public AssetProgress Progress { get; internal set; }

		[JsonProperty("message")]
		public string Message { get; internal set; }

		[JsonIgnore]
		IAssetProgress IAssetStatus.Progress
			=> Progress;

		public override string ToString()
			=> $"{nameof(AssetStatus)}[{Status}, progress={Progress}, message={Message}]";

		private class AssetStatusConverter : JsonConverter<AssetStatus> {
			public override void WriteJson(JsonWriter writer, AssetStatus value, JsonSerializer serializer) {
				if (value == null) {
					writer.WriteNull();
					return;
				}

				writer.WriteStartObject();
				writer.WritePropertyName("status");
				writer.WriteValue(value.Status.ToValue() ?? "unknown");
				writer.WritePropertyName("progress");
				if (value.Progress == null)
					writer.WriteNull();
				else
					serializer.Serialize(writer, value.Progress);
				if (!string.IsNullOrEmpty(value.Message)) {
					writer.WritePropertyName("message");
					writer.WriteValue(value.Message);
				}

				writer.WriteEndObject();
			}

			public override AssetStatus ReadJson(JsonReader reader, Type objectType, AssetStatus existingValue, bool hasExistingValue, JsonSerializer serializer) {
				switch (reader.TokenType) {
					case JsonToken.Null:
					case JsonToken.Undefined:
						return null;

					// Shorthand form: the whole status object is replaced by its status string.
					case JsonToken.String:
						return new AssetStatus { Status = AssetStateExtensions.FromString((string)reader.Value) };

					case JsonToken.StartObject:
						var obj = JObject.Load(reader);
						return new AssetStatus {
							Status   = AssetStateExtensions.FromString(obj["status"]?.ToString()),
							Progress = ReadProgress(obj["progress"]),
							Message  = obj["message"]?.ToString()
						};

					default:
						throw new JsonSerializationException($"Invalid token type for {nameof(AssetStatus)}: {reader.TokenType}");
				}
			}

			/// <summary>
			/// Reads the <c>progress</c> field: an object (<c>{ current, total }</c>) or the
			/// <c>null</c> the API sends while the steps cannot be estimated.
			/// </summary>
			private static AssetProgress ReadProgress(JToken token) {
				if (token is not { Type: JTokenType.Object })
					return null;

				return new AssetProgress {
					Current = token["current"]?.ToObject<uint>() ?? 0,
					Total   = token["total"]?.ToObject<uint>() ?? 0
				};
			}
		}
	}
}
