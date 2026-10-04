namespace Nox.Network.Assets {
	/// <summary>
	/// Preferred release of an asset: a bare release id (public view) or the expanded
	/// <c>{ auto, value, resolved }</c> object returned with <c>?full=1</c>.
	/// <c>-1</c> is the "no release" sentinel used by the API.
	/// </summary>
	public interface IAssetReleasePreference {
		/// <summary><c>true</c> when the asset always resolves to its newest release.</summary>
		bool Auto { get; }

		/// <summary>Pinned release id, or <c>-1</c> when the asset is on <c>auto</c>.</summary>
		int Value { get; }

		/// <summary>Release the asset currently resolves to, or <c>-1</c> when there is none.</summary>
		int Resolved { get; }
	}
}
