namespace Nox.Network.Assets {
	/// <summary>
	/// Live progress of a pending file, sent as the <c>progress</c> field of the status route
	/// (<c>{ "current": 45, "total": 227 }</c>). The API sends <c>null</c> while the steps cannot
	/// be estimated: right after the <c>PUT</c> (waiting for the analysis), while the bundle is
	/// loaded, when the scan is finished, etc.
	/// </summary>
	public interface IAssetProgress {
		/// <summary><c>current</c>: number of steps already done.</summary>
		uint Current { get; }

		/// <summary><c>total</c>: total number of steps announced by the analyzer.</summary>
		uint Total { get; }

		/// <summary>
		/// Completion between <c>0</c> and <c>1</c>, or <c>0</c> when <see cref="Total"/> is unknown.
		/// Handy to drive a progress bar.
		/// </summary>
		float Ratio { get; }
	}
}
