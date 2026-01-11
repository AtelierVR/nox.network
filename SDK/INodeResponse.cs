namespace Nox.Network {
	/// <summary>
	/// Represents a response from a Nox Network Node API.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public interface INodeResponse<out T> {
		/// <summary>
		/// Error returned by the Node API, if any.
		/// </summary>
		public INodeError Error { get; }

		/// <summary>
		/// Indicates whether the response contains an error.
		/// </summary>
		/// <returns></returns>
		public bool HasError();

		/// <summary>
		/// Data returned by the Node API, if any.
		/// </summary>
		public T Data { get; }

		/// <summary>
		/// Indicates whether the response contains data.
		/// </summary>
		/// <returns></returns>
		public bool HasData();
	}
}