using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Nox.Network.Assets {
	/// <summary>
	/// Content-addressed file cache shared by every asset type. Entries are stored under the
	/// filesystem-safe form of their hash, so the same bundle downloaded for two releases or two
	/// asset types is only fetched once.
	/// </summary>
	public interface IAssetCache {
		/// <summary>Absolute path of the cache directory.</summary>
		string Root { get; }

		/// <summary>Absolute path a hash is (or would be) stored at, or <c>null</c> for an empty hash.</summary>
		string PathFor(IAssetHash hash);

		bool Has(IAssetHash hash);

		/// <summary>Hashes currently stored, algorithm included (scans the cache directory).</summary>
		IAssetHash[] Hashes { get; }

		void Clear();

		void Clear(IAssetHash hash);

		/// <summary>Copies an already downloaded file into the cache under <paramref name="hash"/>.</summary>
		UniTask<string> Store(IAssetHash hash, string sourcePath);

		/// <summary>
		/// Returns the download handle for the given URL, reusing the running one when the same
		/// URL or hash is already being downloaded.
		/// </summary>
		IAssetDownload Download(
			string url,
			IAssetHash hash = null,
			IReadOnlyDictionary<string, string> headers = null,
			Action<float, ulong> progress = null,
			CancellationToken token = default
		);
	}
}
