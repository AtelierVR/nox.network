namespace Nox.Network {
	/// <summary>
	/// Represents an error returned by a Nox Network Node API.
	/// </summary>
	public interface INodeError {
		/// <summary>
		/// Is the specific error code of the Message.
		/// </summary>
		public uint Code { get; }
		
		/// <summary>
		/// Error message.
		/// </summary>
		public string Message { get; }
		
		/// <summary>
		/// Status code of the error.
		/// </summary>
		public ushort Status { get; }
	}
}