using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Nox.CCK.Utils;

namespace Nox.CCK.Network {
	public static class NodeDiscover {
		/// <summary>In-memory gateway cache. Entries are valid until ExpiresAt.</summary>
		private static readonly Dictionary<string, DiscoveredGateway> Cache = new();

		public static async UniTask<string> GetGateway(string server) {
			// Check in-memory cache first (respects TTL)
			if (Cache.TryGetValue(server, out var cached) && cached.ExpiresAt > DateTime.UtcNow)
				return cached.GatewayUrl;

			// Run the four-strategy discovery pipeline
			var discovered = await NodeGateway.Discover(server);
			if (discovered == null)
				return Config.Load().Get<string>(new[] { "servers", server, "gateway" });

			Cache[server] = discovered;
			var config = Config.Load();
			config.Set(new[] { "servers", server, "gateway" }, discovered.GatewayUrl);
			config.Save();
			return discovered.GatewayUrl;
		}

		/// <summary>
		/// Returns the cached NoxWellKnown for the given server, triggering discovery if needed.
		/// </summary>
		public static async UniTask<NoxWellKnown> GetWellKnown(string server, CancellationToken cancellationToken = default) {
			if (Cache.TryGetValue(server, out var cached) && cached.ExpiresAt > DateTime.UtcNow)
				return cached.WellKnown;
			await GetGateway(server).AttachExternalCancellation(cancellationToken); // populates cache
			return Cache.TryGetValue(server, out var fresh) ? fresh.WellKnown : null;
		}
	}
}