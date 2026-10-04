using Nox.CCK.Convertors;

namespace Nox.Network.Assets {
	/// <summary>
	/// Body of <c>PATCH /{endpoint}/{asset}</c>. Only the fields that were assigned are sent:
	/// assigning <c>null</c> explicitly clears the value, leaving a field untouched keeps it.
	/// </summary>
	public interface IAssetUpdateRequest {
		/// <summary>New identifier, or <c>null</c> to clear it.</summary>
		string Name { get; set; }

		TranslatedString Title { get; set; }

		TranslatedString Description { get; set; }

		/// <summary>Replaces the Nox tags (invalid ones are dropped).</summary>
		string[] Tags { get; set; }

		/// <summary>
		/// Preferred release: a positive id pins it, <c>0</c> (or any negative value) sends
		/// <c>null</c> to go back to <c>auto</c>.
		/// </summary>
		int Release { get; set; }
	}
}
