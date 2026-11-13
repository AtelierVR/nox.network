using System;

namespace Nox.Network.Assets {
	/// <summary>
	/// Immutable <see cref="IAssetStatusSnapshot"/>: the status of a file and the instant the API
	/// asks to poll it again.
	/// </summary>
	public sealed class AssetStatusSnapshot : IAssetStatusSnapshot {
		public AssetStatusSnapshot(IAssetStatus status, DateTime refetchAt = default) {
			Status    = status;
			RefetchAt = refetchAt;
		}

		/// <inheritdoc />
		public IAssetStatus Status { get; }

		/// <inheritdoc />
		public DateTime RefetchAt { get; }

		/// <inheritdoc />
		public bool IsCompleted
			=> Status?.Status == AssetState.Completed;

		public override string ToString()
			=> $"{nameof(AssetStatusSnapshot)}[{Status}, refetch={RefetchAt:O}]";
	}
}
