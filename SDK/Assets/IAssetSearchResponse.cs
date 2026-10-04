using Cysharp.Threading.Tasks;

namespace Nox.Network.Assets {
	/// <summary>Paginated result of an asset search (<c>{ query, offset, limit, total, items }</c>).</summary>
	public interface IAssetSearchResponse {
		/// <summary>Echo of the <c>query</c> filter, or <c>null</c> when none was given.</summary>
		string Query { get; }

		uint Total { get; }

		uint Limit { get; }

		uint Offset { get; }

		IAsset[] Items { get; }

		bool HasNext { get; }

		bool HasPrevious { get; }

		/// <summary>Fetches the next page, or <c>null</c> when there is none.</summary>
		UniTask<IAssetSearchResponse> Next();

		/// <summary>Fetches the previous page, or <c>null</c> when there is none.</summary>
		UniTask<IAssetSearchResponse> Previous();
	}
}
