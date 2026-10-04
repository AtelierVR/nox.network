namespace Nox.Network.Assets {
	/// <summary>
	/// Status of a file or release slot. The API sends either a plain string
	/// (<c>"completed"</c>) or an object (<c>{ "status": "queued", "progress": 0, "message": "…" }</c>).
	/// </summary>
	public interface IAssetStatus {
		/// <summary>
		/// <c>status</c>: the API sends <c>queued</c>, <c>processing</c>, <c>completed</c> or
		/// <c>failed</c>.
		/// </summary>
		AssetState Status { get; }

		/// <summary><c>progress</c>: completion percentage, from <c>0</c> to <c>100</c>.</summary>
		uint Progress { get; }

		/// <summary><c>message</c>: optional detail about the current step, or <c>null</c>.</summary>
		string Message { get; }
	}
}
