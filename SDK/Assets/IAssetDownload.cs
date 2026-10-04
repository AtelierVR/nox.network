using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine.Events;

namespace Nox.Network.Assets {
	/// <summary>
	/// A cache-backed download: progress, cancellation and de-duplication of concurrent
	/// requests targeting the same URL/hash.
	/// </summary>
	public interface IAssetDownload {
		string Url { get; }

		/// <summary>Expected hash (algorithm + digest), or <c>null</c> when unknown.</summary>
		IAssetHash Hash { get; }

		float Progress { get; }

		/// <summary>Failure message, or <c>null</c>.</summary>
		string Error { get; }

		/// <summary><c>true</c> while the transfer is in flight.</summary>
		bool IsRunning { get; }

		/// <summary><c>true</c> once the file has been stored in the cache.</summary>
		bool IsCompleted { get; }

		/// <summary>Unity event raised with the progress ratio on every update.</summary>
		UnityEvent<float> ProgressEvent { get; }

		void Cancel();

		/// <summary>Runs the download and returns the cached file path, or <c>null</c> on failure.</summary>
		UniTask<string> Start();

		/// <summary>Waits for the download started by another caller and returns its cached path.</summary>
		UniTask<string> Wait();
	}
}
