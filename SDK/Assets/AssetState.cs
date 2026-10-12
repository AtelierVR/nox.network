using System;
using System.Collections.Generic;

namespace Nox.Network.Assets {
	/// <summary>
	/// Lifecycle of a file or release slot, as sent in the API's <c>status</c> field
	/// (<c>'awaiting' | 'queued' | 'processing' | 'completed' | 'failed'</c>). Usable as a bit
	/// set, so a listing can filter on several states at once (see <see cref="All"/>).
	/// </summary>
	[Flags]
	public enum AssetState {
		/// <summary>The API sent no status, or a value this SDK does not know.</summary>
		Unknown = 0,

		/// <summary>
		/// The slot is reserved and the bytes have not been received yet. This is the only state
		/// (with <see cref="Failed"/>) that carries an <c>expires_at</c> deadline.
		/// </summary>
		Awaiting = 1 << 0,

		/// <summary>The bytes have been received and are waiting for the background validation.</summary>
		Queued = 1 << 1,

		/// <summary>The uploaded bundle is being analyzed.</summary>
		Processing = 1 << 2,

		/// <summary>
		/// The file is analyzed and validated; its <c>url</c> has switched to the download URL and
		/// the resource is visible to the listings by default.
		/// </summary>
		Completed = 1 << 3,

		/// <summary>
		/// The upload or the analysis failed; the reason is in <c>status.message</c> and the slot
		/// can be retried until its <c>expires_at</c> is reached.
		/// </summary>
		Failed = 1 << 4,

		/// <summary>
		/// The resource is still being received or analyzed (<see cref="Awaiting"/>,
		/// <see cref="Queued"/> or <see cref="Processing"/>): the client should keep polling
		/// <c>refetch_at</c>. Used as a filter only; it is never sent as the state of a resource.
		/// </summary>
		Pending = Awaiting | Queued | Processing,

		/// <summary>
		/// Every state at once (<c>?status=all</c>), i.e. the union of all the lifecycle values.
		/// Used as a filter only; it is never sent as the state of a resource.
		/// </summary>
		All = Awaiting | Queued | Processing | Completed | Failed
	}

	public static class AssetStateExtensions {
		/// <summary>Reads the <c>status</c> string sent by the API.</summary>
		public static AssetState FromString(string value)
			=> (value?.Trim().ToLowerInvariant()) switch {
				"awaiting"   => AssetState.Awaiting,
				"queued"     => AssetState.Queued,
				"processing" => AssetState.Processing,
				"completed"  => AssetState.Completed,
				"failed"     => AssetState.Failed,
				"all"        => AssetState.All,
				_            => AssetState.Unknown
			};

		/// <summary>Wire value expected by the API, or <c>null</c> for <see cref="AssetState.Unknown"/>.</summary>
		public static string ToValue(this AssetState state)
			=> state switch {
				AssetState.Awaiting   => "awaiting",
				AssetState.Queued     => "queued",
				AssetState.Processing => "processing",
				AssetState.Completed  => "completed",
				AssetState.Failed     => "failed",
				AssetState.All        => "all",
				_                     => null
			};

		/// <summary>
		/// <c>true</c> when the state contains a pending bit (<see cref="AssetState.Pending"/>:
		/// <see cref="AssetState.Awaiting"/>, <see cref="AssetState.Queued"/> or
		/// <see cref="AssetState.Processing"/>) — the client should keep polling <c>refetch_at</c>.
		/// </summary>
		public static bool IsPending(this AssetState state)
			=> (state & AssetState.Pending) != 0;

		/// <summary>
		/// Wire values (<c>status</c>) of a state set: a single state yields itself, a combined
		/// value (<see cref="AssetState.Pending"/>) is expanded into its individual states, and
		/// <see cref="AssetState.All"/> collapses to <c>all</c>. Undefined bits are skipped.
		/// </summary>
		public static IEnumerable<string> ToValues(this AssetState state) {
			if (state == AssetState.All) {
				yield return "all";
				yield break;
			}

			if ((state & AssetState.Awaiting) != 0) yield return "awaiting";
			if ((state & AssetState.Queued) != 0) yield return "queued";
			if ((state & AssetState.Processing) != 0) yield return "processing";
			if ((state & AssetState.Completed) != 0) yield return "completed";
			if ((state & AssetState.Failed) != 0) yield return "failed";
		}
	}
}
