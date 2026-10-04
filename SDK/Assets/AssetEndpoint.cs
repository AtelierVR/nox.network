using System;

namespace Nox.Network.Assets {
	/// <summary>
	/// Identifies a remote asset collection: its logical type (e.g. <c>avatar</c>, <c>world</c>,
	/// <c>mod</c>) and the REST endpoint serving it (e.g. <c>avatars</c>, <c>worlds</c>, <c>mods</c>).
	/// Both are declared server-side with <c>@AssetController(type, endpoint)</c>.
	/// </summary>
	public readonly struct AssetEndpoint : IEquatable<AssetEndpoint> {
		/// <summary>Logical asset type, used by the server-side type validator.</summary>
		public readonly string Type;

		/// <summary>REST endpoint holding the collection (relative to the node gateway).</summary>
		public readonly string Endpoint;

		public AssetEndpoint(string type, string endpoint) {
			Type     = type;
			Endpoint = endpoint;
		}

		public bool IsValid
			=> !string.IsNullOrEmpty(Type) && !string.IsNullOrEmpty(Endpoint);

		public bool Equals(AssetEndpoint other)
			=> string.Equals(Type, other.Type, StringComparison.Ordinal)
				&& string.Equals(Endpoint, other.Endpoint, StringComparison.Ordinal);

		public override bool Equals(object obj)
			=> obj is AssetEndpoint other && Equals(other);

		public override int GetHashCode() {
			unchecked {
				return ((Type?.GetHashCode() ?? 0) * 397) ^ (Endpoint?.GetHashCode() ?? 0);
			}
		}

		public static bool operator ==(AssetEndpoint left, AssetEndpoint right)
			=> left.Equals(right);

		public static bool operator !=(AssetEndpoint left, AssetEndpoint right)
			=> !left.Equals(right);

		public override string ToString()
			=> $"{Type}@{Endpoint}";
	}
}
