using System;
using Nox.CCK.Convertors;
using Nox.CCK.Utils;

namespace Nox.Network.Assets {
	/// <summary>
	/// An asset published on a node: the shared shape behind avatars, worlds, mods, props and
	/// any other type declared with <c>@AssetController(type, endpoint)</c>.
	/// </summary>
	public interface IAsset {
		/// <summary>Logical asset type (<c>avatar</c>, <c>world</c>, <c>mod</c>, …).</summary>
		string Type { get; }

		uint Id { get; }

		/// <summary>URL-safe identifier (<c>^[a-zA-Z0-9][a-zA-Z0-9._-]*$</c>), or <c>null</c>.</summary>
		string Name { get; }

		TranslatedString Title { get; }

		TranslatedString Description { get; }

		/// <summary>Nox tags (not file attributes), e.g. <c>usr:demo</c>.</summary>
		string[] Tags { get; }

		/// <summary>Owner reference (<c>id@server</c>), or <see cref="Identifier.Invalid"/>.</summary>
		Identifier Owner { get; }

		/// <summary>Server the asset lives on.</summary>
		string Server { get; }

		/// <summary>Contributors allowed to manage releases, uploads and deletions.</summary>
		Identifier[] Contributors { get; }

		/// <summary>
		/// Alternative references of the asset: <c>api</c> (canonical URL), <c>iid</c>
		/// (<c>id@server</c>) and <c>nid</c> (<c>name@server</c>) when the asset is named.
		/// </summary>
		IAssetAttribute[] Alias { get; }

		/// <summary>
		/// Canonical reference of the asset (<c>&lt;type&gt;:&lt;id&gt;@&lt;server&gt;</c>).
		/// The concrete model supplies the type (<c>w</c> for a world, …); the generic
		/// <c>Asset</c> leaves it <c>null</c>.
		/// </summary>
		Identifier Identifier { get; }

		IAssetReleasePreference Release { get; }

		IAssetImage[] Images { get; }

		DateTime CreatedAt { get; }

		DateTime UpdatedAt { get; }
	}
}
