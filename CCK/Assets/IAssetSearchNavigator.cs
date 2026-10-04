using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Nox.Network.Assets;

namespace Nox.CCK.Network.Assets {
	/// <summary>
	/// Paging state of a search response, filled in by the client that produced it. It stays out
	/// of <see cref="IAssetSearchResponse"/> so a typed response model can be deserialized and
	/// paged by any client without the SDK knowing about the network stack.
	/// </summary>
	public interface IAssetSearchNavigator {
		/// <summary>Filters that produced the response.</summary>
		AssetSearchRequest Request { get; set; }

		/// <summary>Re-issues the search for another page.</summary>
		Func<IAssetSearchRequest, CancellationToken, UniTask<IAssetSearchResponse>> Navigator { get; set; }
	}
}
