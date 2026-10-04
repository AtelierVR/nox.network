using Nox.CCK.Convertors;

namespace Nox.Network.Assets {
	/// <summary>
	/// Body of the release routes (<c>PUT /{endpoint}/{asset}/releases</c> and
	/// <c>PATCH .../releases/{release}</c>). On patch, only assigned fields are sent.
	/// Status and expiration are never edited directly: they derive from the release files.
	/// </summary>
	public interface IAssetReleaseRequest {
		/// <summary>Custom numeric release id; <c>0</c> (the default) lets the server allocate one.</summary>
		uint Id { get; set; }

		/// <summary>Version name, unique within the asset (e.g. <c>v1.0.0</c>).</summary>
		string Name { get; set; }

		TranslatedString Title { get; set; }

		TranslatedString Changelog { get; set; }

		/// <summary>Release channel; <see cref="AssetChannel.Unknown"/> leaves it untouched.</summary>
		AssetChannel Channel { get; set; }
	}
}
