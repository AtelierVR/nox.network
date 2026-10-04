using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using Nox.Network.Assets;

namespace Nox.CCK.Network.Assets {
	/// <summary>
	/// Paginated search result of one asset type: <typeparamref name="TItem"/> is the model the
	/// items are deserialized as (<see cref="Asset"/> for the generic pipeline, a <c>Mod</c>,
	/// <c>World</c>, … for a typed collection). The navigator is set by the client that produced
	/// the response so that <see cref="Next"/> and <see cref="Previous"/> can re-issue the search
	/// without the model knowing about the network stack.
	/// </summary>
	[Serializable]
	public class AssetSearchResponse<TItem> : IAssetSearchResponse, IAssetSearchNavigator where TItem : Asset {
		[JsonProperty("query")]
		public string Query { get; internal set; }

		[JsonProperty("total")]
		public uint Total { get; internal set; }

		[JsonProperty("limit")]
		public uint Limit { get; internal set; }

		[JsonProperty("offset")]
		public uint Offset { get; internal set; }

		[JsonProperty("items")]
		public TItem[] Items { get; internal set; } = Array.Empty<TItem>();

		[JsonIgnore]
		public AssetSearchRequest Request { get; set; }

		[JsonIgnore]
		public Func<IAssetSearchRequest, CancellationToken, UniTask<IAssetSearchResponse>> Navigator { get; set; }

		[JsonIgnore]
		IAsset[] IAssetSearchResponse.Items
			=> Items;

		[JsonIgnore]
		public bool HasNext
			=> Offset + Limit < Total;

		[JsonIgnore]
		public bool HasPrevious
			=> Offset > 0;

		public UniTask<IAssetSearchResponse> Next()
			=> HasNext && Navigator != null && Request != null
				? Navigator(Request.WithOffset(Offset + Limit), CancellationToken.None)
				: UniTask.FromResult<IAssetSearchResponse>(null);

		public UniTask<IAssetSearchResponse> Previous()
			=> HasPrevious && Navigator != null && Request != null
				? Navigator(Request.WithOffset(Offset > Limit ? Offset - Limit : 0), CancellationToken.None)
				: UniTask.FromResult<IAssetSearchResponse>(null);

		public override string ToString()
			=> $"{GetType().Name}[offset={Offset}, limit={Limit}, total={Total}, items={Items?.Length ?? 0}]";
	}

	/// <summary>
	/// Search result of the generic pipeline, its items typed as <see cref="Asset"/>.
	/// Use <see cref="AssetSearchResponse{TItem}"/> to deserialize the items as a specific model.
	/// </summary>
	[Serializable]
	public class AssetSearchResponse : AssetSearchResponse<Asset> { }
}