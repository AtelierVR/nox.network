using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Nox.Network.Assets;

namespace Nox.CCK.Network.Assets {
	/// <summary>
	/// Status of a file or release slot. The API sends either a plain string
	/// (<c>"completed"</c>) or an object (<c>{ "status": "queued", "progress": 0, "message": "…" }</c>),
	/// so the converter normalizes both shapes.
	/// </summary>
	[Serializable]
	[JsonConverter(typeof(AssetStatusConverter))]
	public class AssetStatus : IAssetStatus {
		[JsonProperty("status")]
		public AssetState Status { get; internal set; }

		[JsonProperty("progress")]
		public uint Progress { get; internal set; }

		[JsonProperty("message")]
		public string Message { get; internal set; }

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
				writer.WriteValue(value.Progress);
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
							Progress = obj["progress"]?.ToObject<uint>() ?? 0,
							Message  = obj["message"]?.ToString()
						};

					default:
						throw new JsonSerializationException($"Invalid token type for {nameof(AssetStatus)}: {reader.TokenType}");
				}
			}
		}
	}
}
