using System;
using Nox.Network.Assets;

namespace Nox.CCK.Network.Assets {
	/// <summary>
	/// Builds the REST paths of the asset pipeline (<c>/{endpoint}/…</c>) relative to the node
	/// gateway. <c>{reference}</c> accepts an id or a name; names and versions are escaped.
	/// </summary>
	public static class AssetRoutes {
		public static string Collection(AssetEndpoint endpoint)
			=> $"/{endpoint.Endpoint}";

		public static string Asset(AssetEndpoint endpoint, string asset)
			=> $"/{endpoint.Endpoint}/{Escape(asset)}";

		public static string Releases(AssetEndpoint endpoint, string asset)
			=> $"{Asset(endpoint, asset)}/releases";

		public static string Release(AssetEndpoint endpoint, string asset, string release)
			=> $"{Releases(endpoint, asset)}/{Escape(release)}";

		public static string Files(AssetEndpoint endpoint, string asset, string release)
			=> $"{Release(endpoint, asset, release)}/files";

		public static string File(AssetEndpoint endpoint, string asset, string release, string file)
			=> $"{Files(endpoint, asset, release)}/{Escape(file)}";

		public static string FileStatus(AssetEndpoint endpoint, string asset, string release, string file)
			=> $"{File(endpoint, asset, release, file)}/status";

		public static string Images(AssetEndpoint endpoint, string asset)
			=> $"{Asset(endpoint, asset)}/images";

		public static string Image(AssetEndpoint endpoint, string asset, int index)
			=> $"{Images(endpoint, asset)}/{index}";

		private static string Escape(string value)
			=> string.IsNullOrEmpty(value) ? string.Empty : Uri.EscapeDataString(value);
	}
}
