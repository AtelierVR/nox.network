using System;
using Newtonsoft.Json;
using Nox.Network;

namespace Nox.CCK.Network {
	[Serializable]
	public class NodeResponse<T> : INodeResponse<T> {
		[JsonProperty("error")]
		private NodeError error;

		[JsonProperty("data")]
		private T data;

		[JsonIgnore]
		public INodeError Error
			=> error;

		public bool HasError()
			=> error is { Status: > 0 } or { Code: > 0 };

		[JsonIgnore]
		public T Data
			=> data;

		public bool HasData()
			=> data != null;

		public override string ToString()
			=> $"{GetType().Name}["
				+ (HasError() ? $"Error={error}" : null)
				+ (HasError() && HasData() ? ", " : null)
				+ (HasData() ? $"Data={data}" : null)
				+ "]";
	}
}