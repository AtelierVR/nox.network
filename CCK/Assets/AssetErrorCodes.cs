namespace Nox.Network.Assets {
	/// <summary>
	/// Machine-readable <c>error.code</c> values sent in the node envelope, so a client can react
	/// to a failure instead of only logging it.
	/// </summary>
	public static class AssetErrorCodes {
		/// <summary>
		/// The file is not downloadable yet: it is still being analyzed (fresh upload or
		/// re-analysis), or it was analyzed by an older analyzer version and is queued for
		/// re-analysis on the spot. The download route answers a <c>409</c>; the client must poll
		/// the file status route (<c>.../files/{file}/status</c>) until it is <c>completed</c>, then
		/// retry the download.
		/// </summary>
		public const string AnalysisPending = "ANALYSIS_PENDING";

		/// <summary>
		/// HTTP status carried by the <c>ANALYSIS_PENDING</c> answer: the file exists but its bytes
		/// are not served until the analysis completes.
		/// </summary>
		public const long AnalysisPendingStatus = 409;
	}
}
