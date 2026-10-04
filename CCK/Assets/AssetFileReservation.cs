using System;
using Newtonsoft.Json;
using Nox.Network.Assets;

namespace Nox.CCK.Network.Assets {
	/// <summary>
	/// Body of <c>POST /{endpoint}/{asset}/releases/{release}/files</c>. Unassigned <c>name</c>
	/// and <c>mime</c> are omitted so the server falls back to the upload headers.
	/// </summary>
	[Serializable]
	public class AssetFileReservation : AssetRequestBody, IAssetFileReservation {
		private string _name;
		private string _mime;

		[JsonProperty("name")]
		public string Name {
			get => _name;
			set {
				_name = value;
				Assign("name");
			}
		}

		[JsonProperty("mime")]
		public string Mime {
			get => _mime;
			set {
				_mime = value;
				Assign("mime");
			}
		}

		[JsonProperty("attributes", NullValueHandling = NullValueHandling.Ignore)]
		public AssetAttribute[] Attributes { get; set; } = Array.Empty<AssetAttribute>();

		[JsonIgnore]
		IAssetAttribute[] IAssetFileReservation.Attributes {
			get => Attributes;
			set => Attributes = Copy(value);
		}

		public bool ShouldSerializeName()
			=> IsAssigned("name");

		public bool ShouldSerializeMime()
			=> IsAssigned("mime");

		/// <summary>Copies any reservation implementation into the mutable model.</summary>
		public static AssetFileReservation From(IAssetFileReservation reservation) {
			var result = new AssetFileReservation {
				Attributes = Copy(reservation?.Attributes)
			};

			if (reservation?.Name != null) result.Name = reservation.Name;
			if (reservation?.Mime != null) result.Mime = reservation.Mime;

			return result;
		}

		private static AssetAttribute[] Copy(IAssetAttribute[] attributes) {
			if (attributes == null)
				return Array.Empty<AssetAttribute>();

			var result = new AssetAttribute[attributes.Length];
			for (var i = 0; i < attributes.Length; i++)
				result[i] = attributes[i] == null
					? null
					: attributes[i] as AssetAttribute ?? new AssetAttribute(attributes[i].Key, attributes[i].Value);

			return result;
		}
	}
}
