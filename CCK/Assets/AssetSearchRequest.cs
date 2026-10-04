using System;
using System.Text;
using Newtonsoft.Json;
using Nox.Network.Assets;

namespace Nox.CCK.Network.Assets {
	/// <summary>
	/// Filters of the collection route (<c>query</c>, <c>offset</c>, <c>limit</c>, <c>full</c>).
	/// <see cref="QueryString"/> returns the query string (including the leading <c>?</c>)
	/// appended to <c>/{endpoint}</c>.
	/// </summary>
	[Serializable]
	public class AssetSearchRequest : IAssetSearchRequest {
		[JsonProperty("query")]
		public string Query { get; set; }

		[JsonProperty("offset")]
		public uint Offset { get; set; }

		[JsonProperty("limit")]
		public uint Limit { get; set; }

		[JsonProperty("full")]
		public bool Full { get; set; }

		[JsonIgnore]
		public string QueryString
			=> Build();

		/// <summary>Copies any request implementation into the serializable model.</summary>
		public static AssetSearchRequest From(IAssetSearchRequest request) {
			if (request == null)
				return new AssetSearchRequest();

			if (request is AssetSearchRequest native)
				return native;

			return new AssetSearchRequest {
				Query  = request.Query,
				Offset = request.Offset,
				Limit  = request.Limit,
				Full   = request.Full
			};
		}

		public IAssetSearchRequest WithOffset(uint offset)
			=> new AssetSearchRequest {
				Query  = Query,
				Offset = offset,
				Limit  = Limit,
				Full   = Full
			};

		private string Build() {
			var builder = new StringBuilder();

			void Add(string key, string value) {
				if (string.IsNullOrEmpty(value)) return;
				if (builder.Length > 0) builder.Append('&');
				builder.Append(key).Append('=').Append(Uri.EscapeDataString(value));
			}

			Add("query", Query);
			if (Full) Add("full", "1");
			if (Offset > 0) Add("offset", Offset.ToString());
			if (Limit > 0) Add("limit", Limit.ToString());

			return builder.Length == 0 ? string.Empty : "?" + builder;
		}

		public override string ToString()
			=> QueryString;
	}
}
