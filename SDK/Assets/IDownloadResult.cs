namespace Nox.Network.Assets {
	/// <summary>
	/// Outcome of a file download: the local file when it succeeded, the HTTP status and the error
	/// code of the node envelope when it did not.
	/// <para>
	/// The download itself never waits: a file that is not available yet (the node answers
	/// <c>409</c> with <c>error.code = ANALYSIS_PENDING</c> while it is analyzed) is reported as a
	/// failure, and the caller decides whether to refetch the asset and retry.
	/// </para>
	/// </summary>
	public interface IDownloadResult {
		/// <summary>Local path of the verified file, or <c>null</c> when the download failed.</summary>
		string Path { get; }

		/// <summary>HTTP status of the request, or <c>0</c> when the server gave no answer at all.</summary>
		long StatusCode { get; }

		/// <summary><c>true</c> when the file was downloaded and verified against the expected hash.</summary>
		bool Success { get; }

		/// <summary>
		/// Machine-readable <c>error.code</c> of the node envelope (e.g. <c>ANALYSIS_PENDING</c>),
		/// or <c>null</c> when the failure carried no envelope (connection error, hash mismatch...).
		/// </summary>
		string ErrorCode { get; }

		/// <summary>Failure detail — the server message, the request error or the hash mismatch — or <c>null</c>.</summary>
		string Error { get; }

		/// <summary>
		/// <c>true</c> when the node refused the bytes because the file is still being analyzed
		/// (<c>409</c> + <c>ANALYSIS_PENDING</c>): the caller should refetch the file status, wait
		/// for the analysis and retry.
		/// </summary>
		bool IsAnalysisPending { get; }
	}
}
