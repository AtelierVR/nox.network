using System;
using System.IO;
using Newtonsoft.Json;
using Nox.CCK.Convertors;
using Nox.Network.Assets;

namespace Nox.CCK.Network.Assets {
	/// <summary>
	/// A content hash kept with its algorithm. The API sends <c>&lt;type&gt;:&lt;hex&gt;</c>
	/// (e.g. <c>sha256:0123…</c>): the algorithm is preserved in <see cref="Type"/> and the
	/// hexadecimal payload is decoded to <see cref="Digest"/> through
	/// <see cref="HexaToBytes"/>. A Base64 payload is still tolerated
	/// (<see cref="Base64ToBytes"/>) so a non-hex digest cannot break deserialization.
	/// </summary>
	[Serializable]
	[JsonConverter(typeof(AssetHashConverter))]
	public class AssetHash : IAssetHash {
		/// <summary>Algorithm used by the asset pipeline.</summary>
		public const string Sha256 = "sha256";

		private const string ValueSeparator = ":";
		private const string FileSeparator  = "-";

		private static readonly HexaToBytes    Hexa       = new HexaToBytes();
		private static readonly Base64ToBytes  Base64     = new Base64ToBytes();
		private static readonly JsonSerializer Serializer = JsonSerializer.CreateDefault();

		private static readonly string[] HexAlgorithms = {
			"md5", "sha1", "sha224", "sha256", "sha384", "sha512",
			"sha3", "blake2b", "blake2s", "xxhash", "crc32"
		};

		public AssetHash() { }

		public AssetHash(string type, byte[] digest) {
			Type   = NormalizeType(type);
			Digest = digest ?? Array.Empty<byte>();
		}

		/// <summary>Algorithm before the colon, or <c>null</c> when the value carried none.</summary>
		[JsonIgnore]
		public string Type { get; internal set; }

		/// <summary>Raw digest decoded from the hexadecimal payload.</summary>
		[JsonIgnore]
		public byte[] Digest { get; internal set; } = Array.Empty<byte>();

		/// <summary>Lowercase hexadecimal digest, or <c>null</c>.</summary>
		[JsonIgnore]
		public string Hex
			=> ToHex(Digest);

		/// <summary>Canonical <c>&lt;type&gt;:&lt;hex&gt;</c> value.</summary>
		[JsonIgnore]
		public string Value
			=> Hex == null ? null : Type == null ? Hex : Type + ValueSeparator + Hex;

		[JsonIgnore]
		public bool IsValid
			=> Digest is { Length: > 0 };

		public override string ToString()
			=> Value ?? $"{nameof(AssetHash)}[empty]";

		#region Statics

		/// <summary>
		/// Parses a <c>&lt;type&gt;:&lt;hex&gt;</c> value, a bare hexadecimal digest, or the
		/// <c>&lt;type&gt;-&lt;hex&gt;</c> cache file name. Returns <c>null</c> for an empty value.
		/// </summary>
		public static AssetHash Parse(string value) {
			if (string.IsNullOrWhiteSpace(value))
				return null;

			var raw   = value.Trim();
			var index = raw.IndexOf(ValueSeparator, StringComparison.Ordinal);

			// Cache entries are named "<type>-<hex>" because ':' is not filesystem-safe.
			if (index <= 0)
				index = raw.IndexOf(FileSeparator, StringComparison.Ordinal);

			var type    = index > 0 ? raw.Substring(0, index) : null;
			var payload = index > 0 ? raw.Substring(index + 1) : raw;

			return new AssetHash {
				Type   = type == null ? null : NormalizeType(type),
				Digest = Decode(type, payload) ?? Array.Empty<byte>()
			};
		}

		public static AssetHash FromHex(string type, string hex)
			=> new AssetHash(type, HexToBytes(hex));

		public static AssetHash FromDigest(string type, byte[] digest)
			=> new AssetHash(type, digest);

		public static string ToHex(byte[] digest)
			=> digest is { Length: > 0 } ? Nox.CCK.Utils.Hashing.ToHex(digest) : null;

		/// <summary>Filesystem-safe name of a hash (<c>&lt;type&gt;-&lt;hex&gt;</c>).</summary>
		public static string ToFileName(IAssetHash hash) {
			if (hash?.Hex == null)
				return null;

			return hash.Type == null ? hash.Hex : hash.Type + FileSeparator + hash.Hex;
		}

		public static bool Has(IAssetHash hash)
			=> hash?.Digest is { Length: > 0 };

		public static bool Matches(IAssetHash expected, IAssetHash actual)
			=> Has(expected) && Has(actual) && expected.Hex == actual.Hex && MatchesType(expected, actual);

		/// <summary>Compares a hash against a hexadecimal digest or a full <c>&lt;type&gt;:&lt;hex&gt;</c> value.</summary>
		public static bool Matches(IAssetHash expected, string value)
			=> Matches(expected, Parse(value));

		public static string NormalizeType(string type)
			=> Nox.CCK.Utils.Hashing.NormalizeType(type);

		private static bool MatchesType(IAssetHash expected, IAssetHash actual)
			=> expected.Type == null || actual.Type == null || expected.Type == actual.Type;

		/// <summary>Decodes a hexadecimal payload (<see cref="HexaToBytes"/>), or Base64 as a fallback.</summary>
		private static byte[] Decode(string algorithm, string payload) {
			if (string.IsNullOrEmpty(payload))
				return null;

			// Quoted so a digest starting with a digit is read as a string and not as a number.
			using var reader = new JsonTextReader(new StringReader("\"" + payload + "\""));

			if (!reader.Read())
				return null;

			try {
				return IsHexadecimal(algorithm, payload)
					? Hexa.ReadJson(reader, typeof(byte[]), null, false, Serializer)
					: Base64.ReadJson(reader, typeof(byte[]), null, false, Serializer);
			} catch (Exception ex) when (ex is FormatException or JsonException) {
				throw new JsonSerializationException($"Invalid hash payload '{payload}'", ex);
			}
		}

		private static bool IsHexadecimal(string algorithm, string payload) {
			if (payload.Length % 2 != 0)
				return false;

			foreach (var character in payload)
				if (!Uri.IsHexDigit(character))
					return false;

			return string.IsNullOrEmpty(algorithm)
				|| Array.Exists(HexAlgorithms, known => known == NormalizeType(algorithm));
		}

		private static byte[] HexToBytes(string hex) {
			var value = hex?.Trim();
			if (string.IsNullOrEmpty(value))
				return Array.Empty<byte>();

			var index = value.IndexOf(ValueSeparator, StringComparison.Ordinal);
			if (index >= 0)
				value = value.Substring(index + 1);

			if (value.Length % 2 != 0)
				return Array.Empty<byte>();

			foreach (var character in value)
				if (!Uri.IsHexDigit(character))
					return Array.Empty<byte>();

			var bytes = new byte[value.Length / 2];
			for (var i = 0; i < bytes.Length; i++)
				bytes[i] = Convert.ToByte(value.Substring(i * 2, 2), 16);

			return bytes;
		}

		#endregion

		private class AssetHashConverter : JsonConverter<AssetHash> {
			public override void WriteJson(JsonWriter writer, AssetHash value, JsonSerializer serializer) {
				if (value?.Value == null)
					writer.WriteNull();
				else
					writer.WriteValue(value.Value);
			}

			public override AssetHash ReadJson(JsonReader reader, Type objectType, AssetHash existingValue, bool hasExistingValue, JsonSerializer serializer) {
				switch (reader.TokenType) {
					case JsonToken.Null:
					case JsonToken.Undefined:
						return null;

					case JsonToken.String:
						return Parse((string)reader.Value);

					case JsonToken.StartArray:
						// A raw JSON array of bytes is accepted as well (read by hand so the
						// converter is not re-entered).
						var bytes = new System.Collections.Generic.List<byte>();
						while (reader.Read() && reader.TokenType != JsonToken.EndArray)
							bytes.Add(Convert.ToByte(reader.Value));
						return new AssetHash(null, bytes.ToArray());

					default:
						throw new JsonSerializationException($"Invalid token type for a hash: {reader.TokenType}");
				}
			}
		}
	}
}
