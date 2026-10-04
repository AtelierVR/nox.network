using System.Collections.Generic;

namespace Nox.Network.Assets {
	/// <summary>
	/// Result of the server-side validation of an uploaded file
	/// (<c>{ engine, platform, version, descriptors, metrics }</c>), or <c>null</c> when the type
	/// has no validator.
	/// </summary>
	public interface IAssetAnalysis {
		string Engine { get; }

		string Platform { get; }

		string Version { get; }

		int Descriptors { get; }

		/// <summary>
		/// Free-form metrics reported by the validator. Values are the raw deserialized JSON
		/// nodes, so nested objects are kept as-is.
		/// </summary>
		IReadOnlyDictionary<string, object> Metrics { get; }
	}
}
