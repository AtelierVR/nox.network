using System;
using Newtonsoft.Json;
using Nox.CCK.Convertors;
using Nox.Network.Assets;

namespace Nox.CCK.Network.Assets {
	/// <summary>
	/// Body of <c>PATCH /{endpoint}/{asset}</c>. Only assigned fields are sent: assigning
	/// <c>null</c> clears the value, leaving a field untouched keeps it.
	/// </summary>
	[Serializable]
	public class AssetUpdateRequest : AssetRequestBody, IAssetUpdateRequest {
		private string           _name;
		private TranslatedString _title;
		private TranslatedString _description;
		private string[]         _tags;
		private int              _release;

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

		[JsonProperty("description"), JsonConverter(typeof(TranslatedStringConverter))]
		public TranslatedString Description {
			get => _description;
			set {
				_description = value;
				Assign("description");
			}
		}

		[JsonProperty("tags")]
		public string[] Tags {
			get => _tags;
			set {
				_tags = value;
				Assign("tags");
			}
		}

		/// <summary>
		/// Preferred release. A positive id pins it; <c>0</c> (or any negative value) sends
		/// <c>null</c> to go back to <c>auto</c>. Only sent when it was assigned.
		/// </summary>
		[JsonProperty("release"), JsonConverter(typeof(AutoReleaseConverter))]
		public int Release {
			get => _release;
			set {
				_release = value;
				Assign("release");
			}
		}

		public bool ShouldSerializeName()
			=> IsAssigned("name");

		public bool ShouldSerializeTitle()
			=> IsAssigned("title");

		public bool ShouldSerializeDescription()
			=> IsAssigned("description");

		public bool ShouldSerializeTags()
			=> IsAssigned("tags");

		public bool ShouldSerializeRelease()
			=> IsAssigned("release");

		/// <summary>Maps <c>0</c> to the JSON <c>null</c> that resets the preference to <c>auto</c>.</summary>
		private class AutoReleaseConverter : JsonConverter<int> {
			public override void WriteJson(JsonWriter writer, int value, JsonSerializer serializer) {
				if (value > 0)
					writer.WriteValue(value);
				else
					writer.WriteNull();
			}

			public override int ReadJson(JsonReader reader, Type objectType, int existingValue, bool hasExistingValue, JsonSerializer serializer)
				=> reader.TokenType is JsonToken.Null or JsonToken.Undefined
					? AssetReleasePreference.None
					: Convert.ToInt32(reader.Value);
		}
	}
}
