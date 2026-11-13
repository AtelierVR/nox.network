using Nox.Network.Assets;

namespace Nox.CCK.Network.Assets {
	/// <summary>
	/// Outcome of a file download (<see cref="IDownloadResult"/>): local path when it succeeded,
	/// HTTP status and node error code when it did not.
	/// </summary>
	public readonly struct DownloadResult : IDownloadResult {
		/// <summary>A verified download.</summary>
		public DownloadResult(string path)
			: this() {
			Path      = path;
			StatusCode = 200;
			Success   = path != null;
		}

		/// <summary>A download that failed with the given HTTP status (and envelope code, when any).</summary>
		public DownloadResult(long statusCode, string error, string errorCode = null)
			: this() {
			StatusCode = statusCode;
			Error      = error;
			ErrorCode  = errorCode;
			Success    = false;
		}

		/// <inheritdoc />
		public string Path { get; }

		/// <inheritdoc />
		public long StatusCode { get; }

		/// <inheritdoc />
		public bool Success { get; }

		/// <inheritdoc />
		public string ErrorCode { get; }

		/// <inheritdoc />
		public string Error { get; }

		/// <inheritdoc />
		public bool IsAnalysisPending
			=> !Success
				&& StatusCode == AssetErrorCodes.AnalysisPendingStatus
				&& string.Equals(
                    ErrorCode, 
                    AssetErrorCodes.AnalysisPending, 
                    System.StringComparison.OrdinalIgnoreCase
                );

		public override string ToString()
			=> Success
				? $"{nameof(DownloadResult)}[success, {Path}]"
				: $"{nameof(DownloadResult)}[{StatusCode}, {ErrorCode ?? "no code"}, {Error}]";
	}
}
