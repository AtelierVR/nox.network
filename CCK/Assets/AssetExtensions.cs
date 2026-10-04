using System;
using System.Collections.Generic;
using System.Linq;
using Nox.CCK.Utils;
using Nox.Network.Assets;

namespace Nox.CCK.Network.Assets {
	/// <summary>Helpers over the generic asset shape (<see cref="IAsset"/>).</summary>
	public static class AssetExtensions {
		/// <summary>
		/// Image of the asset that best fits <paramref name="aspect"/> (width / height), or
		/// <c>null</c> when it has no usable image. Closest ratio wins, then the highest
		/// <see cref="IAssetImage.Priority"/>; images without dimensions are used only as a
		/// last resort, and a non-positive <paramref name="aspect"/> picks by priority alone.
		/// </summary>
		public static IAssetImage BestImage(this IAsset asset, float aspect) {
			IAssetImage best = null;
			var bestDelta = float.MaxValue;
			var bestPriority = int.MinValue;
			var bestSized = false;

			foreach (var image in asset?.Images ?? Array.Empty<IAssetImage>()) {
				if (string.IsNullOrEmpty(image?.Url))
					continue;

				var sized = image.Width > 0 && image.Height > 0;
				var delta = !sized || aspect <= 0f
					? 0f
					: Math.Abs((float)image.Width / image.Height - aspect);

				var better = best == null
					|| (sized && !bestSized)
					|| (sized == bestSized && (delta < bestDelta || (delta == bestDelta && image.Priority > bestPriority)));

				if (!better)
					continue;

				best         = image;
				bestDelta    = delta;
				bestPriority = image.Priority;
				bestSized    = sized;
			}

			return best;
		}

		/// <summary><c>true</c> when the given user owns the asset or is one of its contributors.</summary>
		public static bool IsContributor(this IAsset asset, Identifier identifier)
			=> asset != null
				&& (asset.Owner.Equals(identifier)
					|| (asset.Contributors != null && Array.Exists(asset.Contributors, contributor => contributor.Equals(identifier))));

		/// <summary>
		/// Value of an alternative reference of the asset (<c>api</c> for its canonical URL,
		/// <c>iid</c>, <c>nid</c>, <c>github</c> when it is backed by a repository, …),
		/// or <c>null</c> when the asset has no such reference.
		/// </summary>
		public static string Alias(this IAsset asset, string key)
			=> asset?.Alias?.FirstOrDefault(alias => alias?.Key == key)?.Value;

		/// <summary>
		/// Value of a file attribute (<c>platform</c>, <c>arch</c>, <c>engine</c>, …),
		/// or <c>null</c> when the file does not declare it.
		/// </summary>
		public static string Attribute(this IAssetFile file, string key)
			=> file?.Attributes?.FirstOrDefault(
				attribute => string.Equals(attribute?.Key, key, StringComparison.OrdinalIgnoreCase)
			)?.Value;

		/// <summary>
		/// Hash of the file computed with <paramref name="type"/> (default <c>sha256</c>),
		/// falling back to the first declared hash, or <c>null</c> when the file has none.
		/// </summary>
		public static IAssetHash HashOf(this IAssetFile file, string type = AssetHash.Sha256) {
			if (file?.Hashs is not { Length: > 0 })
				return null;

			return file.Hashs.FirstOrDefault(hash => AssetHash.NormalizeType(hash?.Type) == AssetHash.NormalizeType(type))
				?? file.Hashs[0];
		}

		/// <summary>
		/// Filesystem-safe cache name of the file, built from its hash
		/// (<c>&lt;type&gt;-&lt;hex&gt;</c>), or <c>null</c> when it carries no usable hash.
		/// </summary>
		public static string CacheKey(this IAssetFile file)
			=> AssetHash.ToFileName(file?.HashOf());

		/// <summary>
		/// File of the release that fits <paramref name="platform"/> and <paramref name="engine"/>, or
		/// <c>null</c> when none is compatible. Files declaring the other platform, another engine or
		/// an incompatible engine version are rejected; the most specific match wins (platform and
		/// engine declared, then the one carrying an engine version), so a variant built for the
		/// current target always beats a generic one. A <c>null</c> constraint accepts any value, and
		/// <paramref name="engineVersion"/> defaults to the running engine version.
		/// </summary>
		public static IAssetFile BestFile(
			this IAssetRelease release,
			Platform? platform = null,
			Engine? engine = null,
			string engineVersion = null
		) {
			IAssetFile best = null;
			var bestScore = int.MinValue;

			foreach (var file in release?.Files ?? Array.Empty<IAssetFile>()) {
				if (file == null)
					continue;

				var score = 0;

				var filePlatform = file.Attribute("platform");
				if (filePlatform != null) {
					// Without a platform constraint any platform is accepted; with one it must match.
					if (platform != null && PlatformExtensions.GetPlatformFromName(filePlatform) != platform.Value)
						continue;

					score += 2;
				}

				var fileEngine = file.Attribute("engine");
				if (fileEngine != null) {
					var (name, version) = SplitEngine(fileEngine);

					// Without an engine constraint any engine is accepted; with one, the name must
					// match and a declared version must fit the requested (or running) engine.
					if (engine != null) {
						if (!engine.Value.Equals(Engine.From(name)))
							continue;
						if (version != null) {
							if (!IsEngineVersionCompatible(version, engineVersion))
								continue;
							score++;
						}
					}

					score++;
				}

				if (best == null || score > bestScore) {
					best      = file;
					bestScore = score;
				}
			}

			return best;
		}

		/// <summary>Splits an <c>engine</c> attribute (<c>&lt;engine&gt;[:&lt;version&gt;]</c>).</summary>
		private static (string name, string version) SplitEngine(string value) {
			var index = value?.IndexOf(':') ?? -1;
			return index < 0
				? (value, null)
				: (value.Substring(0, index), value.Substring(index + 1));
		}

		/// <summary>
		/// Compares an engine version of a file (<c>6000.4</c>, <c>6000.4.4f1</c>) with
		/// <paramref name="engineVersion"/> (the running engine by default): only the leading numeric
		/// components are compared, and the file version must be a prefix of the current one, so
		/// <c>6000.4</c> matches <c>6000.4.4</c>.
		/// </summary>
		private static bool IsEngineVersionCompatible(string version, string engineVersion = null) {
			var parts   = NumericParts(version);
			var current = NumericParts(engineVersion ?? EngineExtensions.CurrentVersion.ToString());

			if (parts.Length > current.Length)
				return false;

			for (var i = 0; i < parts.Length; i++)
				if (parts[i] != current[i])
					return false;

			return true;
		}

		/// <summary>Leading dotted numeric components of a version (<c>"6000.4.4f1"</c> → <c>[6000, 4, 4]</c>).</summary>
		private static int[] NumericParts(string version) {
			if (string.IsNullOrWhiteSpace(version))
				return Array.Empty<int>();

			var parts = new List<int>();

			foreach (var chunk in version.Split('.')) {
				var digits = new string(chunk.TakeWhile(char.IsDigit).ToArray());

				if (digits.Length == 0)
					break;

				int value;

				if (!int.TryParse(digits, out value))
					break;

				parts.Add(value);
			}

			return parts.ToArray();
		}
	}
}
