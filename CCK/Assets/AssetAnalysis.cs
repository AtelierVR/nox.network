using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Nox.Network.Assets;

namespace Nox.CCK.Network.Assets {
	/// <summary>Result of the server-side validation of an uploaded file.</summary>
	[Serializable]
	public class AssetAnalysis : IAssetAnalysis {
		[JsonProperty("engine")]
		public string Engine { get; internal set; }

		[JsonProperty("platform")]
		public string Platform { get; internal set; }

		[JsonProperty("version")]
		public string Version { get; internal set; }

		[JsonProperty("descriptors")]
		public int Descriptors { get; internal set; }

		[JsonProperty("metrics")]
		public Dictionary<string, object> Metrics { get; internal set; }

		[JsonIgnore]
		IReadOnlyDictionary<string, object> IAssetAnalysis.Metrics
			=> Metrics;

		public override string ToString()
			=> $"{nameof(AssetAnalysis)}[engine={Engine}, platform={Platform}, descriptors={Descriptors}]";
	}
}
