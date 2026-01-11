using System;
using Newtonsoft.Json;
using Nox.Network;

namespace Nox.CCK.Network {
	[Serializable]
	public class NodeError : INodeError, IEquatable<INodeError> {
		[JsonProperty("message")]
		private string message;

		[JsonProperty("code")]
		private uint code;

		[JsonProperty("status")]
		private ushort status;

		[JsonIgnore]
		public uint Code
			=> code;

		[JsonIgnore]
		public string Message
			=> message;

		[JsonIgnore]
		public ushort Status
			=> status;

		public override string ToString()
			=> $"{GetType().Name}[status={Status}, code={Code}, message={Message}]";

		public override int GetHashCode()
			=> HashCode.Combine(Code, Status);

		public bool Equals(INodeError other)
			=> other != null
				&& other.Code == Code
				&& other.Status == Status;

		public override bool Equals(object obj)
			=> obj is INodeError other
				&& other.Code == Code
				&& other.Status == Status;
	}
}