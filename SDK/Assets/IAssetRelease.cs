using System;
using Nox.CCK.Convertors;

namespace Nox.Network.Assets {
	/// <summary>
	/// A release (version) of an asset. A release is <c>queued</c> while it holds no completed
	/// file and becomes <c>completed</c> as soon as one of its files is uploaded; deleting its
	/// last file puts it back to <c>queued</c>.
	/// </summary>
	public interface IAssetRelease {
		uint Id { get; }

		string Name { get; }

		TranslatedString Title { get; }

		TranslatedString Changelog { get; }

		AssetChannel Channel { get; }

		/// <summary>Alternative references of the release, e.g. <c>{ key: "nid", value: … }</c>.</summary>
		IAssetAttribute[] Alias { get; }

		IAssetStatus Status { get; }

		IAssetFile[] Files { get; }

		/// <summary>
		/// Expiration of a release without any completed file, or <c>default</c>
		/// (<see cref="DateTime.MinValue"/>).
		/// </summary>
		DateTime ExpiresAt { get; }

		/// <summary>Time after which the client should GET the release again, or <c>default</c>.</summary>
		DateTime RefetchAt { get; }
	}
}
