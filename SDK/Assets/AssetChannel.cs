namespace Nox.Network.Assets {
	/// <summary>Release channel, used to distinguish stable releases from preview ones.</summary>
	public enum AssetChannel {
		Unknown = 0,
		Stable,
		Candidate,
		Nightly
	}

	public static class AssetChannelExtensions {
		/// <summary>Reads the channel sent by the API (<c>stable</c>, <c>candidate</c>, <c>nightly</c>).</summary>
		public static AssetChannel FromString(string value)
            => (value?.Trim().ToLowerInvariant()) switch {
                "stable"    => AssetChannel.Stable,
                "candidate" => AssetChannel.Candidate,
                "nightly"   => AssetChannel.Nightly,
                _           => AssetChannel.Unknown,
            };

		/// <summary>Wire value expected by the API (<c>stable</c>, <c>candidate</c>, <c>nightly</c>).</summary>
		public static string ToValue(this AssetChannel channel)
			=> channel switch {
				AssetChannel.Stable    => "stable",
				AssetChannel.Candidate => "candidate",
				AssetChannel.Nightly   => "nightly",
				_                      => null
			};
	}
}
