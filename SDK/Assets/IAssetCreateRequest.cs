using Nox.CCK.Convertors;

namespace Nox.Network.Assets {
	/// <summary>
	/// Body of <c>PUT /{endpoint}</c>. Every field is optional: unassigned fields are not sent,
	/// so the server keeps its defaults.
	/// </summary>
	public interface IAssetCreateRequest {
		/// <summary>Custom numeric asset id; <c>0</c> (the default) lets the server allocate one.</summary>
		uint Id { get; set; }

		/// <summary>URL-safe identifier. Without it the asset can only be addressed by id.</summary>
		string Name { get; set; }

		/// <summary>Display title. Defaults to the name. Serialized as a plain string.</summary>
		TranslatedString Title { get; set; }

		TranslatedString Description { get; set; }

		string[] Tags { get; set; }
	}
}
