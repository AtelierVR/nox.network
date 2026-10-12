namespace Nox.Network.Assets {
	/// <summary>
	/// Status of a file or release slot. The API sends either a plain string
	/// (<c>"completed"</c>) or an object
	/// (<c>{ "status": "processing", "progress": { "current": 45, "total": 227 }, "message": "…" }</c>).
	/// </summary>
	public interface IAssetStatus {
		/// <summary>
		/// <c>status</c>: the API sends <c>awaiting</c>, <c>queued</c>, <c>processing</c>,
		/// <c>completed</c> or <c>failed</c>.
		/// </summary>
		AssetState Status { get; }

		/// <summary>
		/// <c>progress</c>: live <c>{ current, total }</c> steps, or <c>null</c> while they cannot
		/// be estimated (right after the upload, while the bundle loads, when the scan is done, …).
		/// </summary>
		IAssetProgress Progress { get; }

		/// <summary><c>message</c>: optional detail about the current step, or <c>null</c>.</summary>
		string Message { get; }
	}
}
