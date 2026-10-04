namespace Nox.Network.Assets {
	/// <summary>
	/// Lifecycle of a file or release slot, as sent in the API's <c>status</c> field
	/// (<c>'queued' | 'processing' | 'completed' | 'failed'</c>).
	/// </summary>
	public enum AssetState {
		/// <summary>The API sent no status, or a value this SDK does not know.</summary>
		Unknown = 0,

		/// <summary>The slot is reserved and the bytes have not been received yet.</summary>
		Queued,

		/// <summary>The file is being received, analyzed or validated.</summary>
		Processing,

		/// <summary>The file is uploaded, analyzed and validated.</summary>
		Completed,

		/// <summary>The upload or the analysis failed; the slot can be retried.</summary>
		Failed
	}

	public static class AssetStateExtensions {
		/// <summary>Reads the <c>status</c> string sent by the API.</summary>
		public static AssetState FromString(string value)
			=> (value?.Trim().ToLowerInvariant()) switch {
				"queued"     => AssetState.Queued,
				"processing" => AssetState.Processing,
				"completed"  => AssetState.Completed,
				"failed"     => AssetState.Failed,
				_            => AssetState.Unknown
			};

		/// <summary>Wire value expected by the API, or <c>null</c> for <see cref="AssetState.Unknown"/>.</summary>
		public static string ToValue(this AssetState state)
			=> state switch {
				AssetState.Queued     => "queued",
				AssetState.Processing => "processing",
				AssetState.Completed  => "completed",
				AssetState.Failed     => "failed",
				_                     => null
			};
	}
}
