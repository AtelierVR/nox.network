using System;
using System.Collections.Generic;

namespace Nox.CCK.Network.Assets {
	/// <summary>
	/// Base class of the writable asset requests. It tracks which properties were explicitly
	/// assigned so the PATCH (and PUT) bodies only contain those, keeping the "omitted field is
	/// left untouched, explicit <c>null</c> clears it" contract of the API.
	/// </summary>
	[Serializable]
	public abstract class AssetRequestBody {
		private readonly HashSet<string> _assigned = new(StringComparer.Ordinal);

		protected void Assign(string property)
			=> _assigned.Add(property);

		protected bool IsAssigned(string property)
			=> _assigned.Contains(property);

		/// <summary>Marks a property as assigned without changing its value.</summary>
		public void MarkAssigned(string property)
			=> Assign(property);
	}
}
