using System;

namespace Nox.Network.Assets {
	/// <summary>Image attached to an asset (cover, gallery, …).</summary>
	public interface IAssetImage {
		string Url { get; }

		int Priority { get; }

		int Width { get; }

		int Height { get; }

		string Alt { get; }
	}
}
