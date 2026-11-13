using System;

namespace Nox.Network.Assets {
	/// <summary>
	/// A snapshot of a file status: the status itself plus the instant the API asks the client to
	/// poll again.
	/// <para>
	/// The API exposes <c>status</c> and <c>refetch_at</c> on the <b>file</b>, not on the status
	/// object, so a poll loop needs both together — this is what the file status endpoint
	/// (<c>GET .../files/{fileRef}/status</c>) yields once reduced to what the client consumes.
	/// </para>
	/// </summary>
	public interface IAssetStatusSnapshot {
		/// <summary>
		/// <c>status</c>: the lifecycle state, the analyzer steps (<c>progress</c>, <c>null</c> while
		/// they cannot be estimated) and the server message.
		/// </summary>
		IAssetStatus Status { get; }

		/// <summary>
		/// <c>refetch_at</c>: when to GET the status again, or <c>default</c> once the file is stable
		/// (the API omits the field).
		/// </summary>
		DateTime RefetchAt { get; }

		/// <summary><c>true</c> once the file is analyzed and its bytes are downloadable.</summary>
		bool IsCompleted { get; }
	}
}
