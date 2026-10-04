namespace Nox.Network.Assets {
	/// <summary>Filters accepted by the collection route (<c>GET /{endpoint}</c>).</summary>
	public interface IAssetSearchRequest {
		/// <summary><c>query</c>: free-text search, or <c>null</c>.</summary>
		string Query { get; set; }

		/// <summary><c>offset</c>: zero-based index of the first result. The API defaults to <c>0</c>.</summary>
		uint Offset { get; set; }

		/// <summary>
		/// <c>limit</c>: maximum number of results, between <c>1</c> and <c>100</c>.
		/// <c>0</c> leaves the field out, so the server uses its default (<c>20</c>).
		/// </summary>
		uint Limit { get; set; }

		/// <summary><c>full=1</c>: expands <c>url</c> and the release preference of the items.</summary>
		bool Full { get; set; }

		/// <summary>
		/// Query string (including the leading <c>?</c>) appended to the collection route,
		/// or an empty string when no filter is set.
		/// </summary>
		string QueryString
			=> string.Empty;

		/// <summary>Returns a copy of this request pointing at another page.</summary>
		IAssetSearchRequest WithOffset(uint offset)
			=> null;
	}
}
