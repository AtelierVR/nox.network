using System;
using Newtonsoft.Json;
using Nox.CCK.Convertors;
using Nox.Network.Assets;

namespace Nox.CCK.Network.Assets {
	/// <summary>
	/// Body of the release routes. Only assigned fields are sent, so the same object works for
	/// <c>PUT …/releases</c> (create) and <c>PATCH …/releases/{release}</c> (update).
	/// </summary>
	[Serializable]
	public class AssetReleaseRequest : AssetRequestBody, IAssetReleaseRequest {
		private uint             _id;
		private string           _name;
		private TranslatedString _title;
		private TranslatedString _changelog;
		private AssetChannel     _channel;

		[JsonProperty("id")]
		public uint Id {
			get => _id;
			set {
				_id = value;
				Assign("id");
			}
		}

		[JsonProperty("name")]
		public string Name {
			get => _name;
			set {
				_name = value;
				Assign("name");
			}
		}

		[JsonProperty("title"), JsonConverter(typeof(TranslatedStringConverter))]
		public TranslatedString Title {
			get => _title;
			set {
				_title = value;
				Assign("title");
			}
		}

		[JsonProperty("changelog"), JsonConverter(typeof(TranslatedStringConverter))]
		public TranslatedString Changelog {
			get => _changelog;
			set {
				_changelog = value;
				Assign("changelog");
			}
		}

		/// <summary>Release channel; <see cref="AssetChannel.Unknown"/> leaves it untouched.</summary>
		[JsonProperty("channel"), JsonConverter(typeof(AssetChannelConverter))]
		public AssetChannel Channel {
			get => _channel;
			set {
				_channel = value;
				Assign("channel");
			}
		}

		public bool ShouldSerializeId()
			=> IsAssigned("id") && _id > 0;

		public bool ShouldSerializeName()
			=> IsAssigned("name");

		public bool ShouldSerializeTitle()
			=> IsAssigned("title");

		public bool ShouldSerializeChangelog()
			=> IsAssigned("changelog");

		public bool ShouldSerializeChannel()
			=> IsAssigned("channel") && _channel != AssetChannel.Unknown;

		/// <summary>Writes the wire value of a channel, or <c>null</c> when it is unknown.</summary>
		private class AssetChannelConverter : JsonConverter<AssetChannel> {
			public override void WriteJson(JsonWriter writer, AssetChannel value, JsonSerializer serializer) {
				var wire = value.ToValue();

				if (wire == null)
					writer.WriteNull();
				else
					writer.WriteValue(wire);
			}

			public override AssetChannel ReadJson(JsonReader reader, Type objectType, AssetChannel existingValue, bool hasExistingValue, JsonSerializer serializer)
				=> reader.TokenType == JsonToken.String
					? AssetChannelExtensions.FromString((string)reader.Value)
					: AssetChannel.Unknown;
		}
	}
}
