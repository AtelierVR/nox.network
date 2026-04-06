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
			=> error != null && !string.IsNullOrEmpty(error.Code);

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