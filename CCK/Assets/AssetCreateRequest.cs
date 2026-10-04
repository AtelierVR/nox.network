using System;
using Newtonsoft.Json;
using Nox.CCK.Convertors;
using Nox.Network.Assets;

namespace Nox.CCK.Network.Assets {
	/// <summary>
	/// Body of <c>PUT /{endpoint}</c>. Only assigned fields are sent; assigning <c>0</c> to
	/// <see cref="Id"/> is treated as "not set" so the server allocates the id.
	/// </summary>
	[Serializable]
	public class AssetCreateRequest : AssetRequestBody, IAssetCreateRequest {
		private uint             _id;
		private string           _name;
		private TranslatedString _title;
		private TranslatedString _description;
		private string[]         _tags;

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

		public bool ShouldSerializeId()
			=> IsAssigned("id") && _id > 0;

		public bool ShouldSerializeName()
			=> IsAssigned("name");

		public bool ShouldSerializeTitle()
			=> IsAssigned("title");

		public bool ShouldSerializeDescription()
			=> IsAssigned("description");

		public bool ShouldSerializeTags()
			=> IsAssigned("tags");
	}
}
