using System;
using System.IO;
using Newtonsoft.Json.Linq;
using UnityEngine.Networking;
using Logger = Nox.CCK.Utils.Logger;

namespace Nox.CCK.Network {
	/// <summary>
	/// Reads the node envelope of a failed download: the machine-readable <c>error.code</c> (e.g.
	/// <c>ANALYSIS_PENDING</c>) and its message.
	/// <para>
	/// A streamed download writes the body of the error answer to the target file of its
	/// <see cref="DownloadHandlerFile"/>, which is where the envelope lives; a buffered request
	/// exposes it directly.
	/// </para>
	/// </summary>
	public static class DownloadEnvelope {
		private const string LogTag = "Download";

		/// <summary>The envelope of a failed download is small; anything bigger is not one.</summary>
		private const long MaxBodySize = 64 * 1024;

		/// <summary>
		/// Reads <c>error.code</c> of the failed request, or <c>null</c> when the answer carried no
		/// readable envelope.
		/// </summary>
		/// <param name="request">Request that failed (used for its buffered body, when any).</param>
		/// <param name="bodyPath">File the streamed body was written to, when buffering is not used.</param>
		/// <param name="message"><c>error.message</c>, or <c>null</c>.</param>
		public static string ReadCode(UnityWebRequest request, string bodyPath, out string message) {
			message = null;

			var body = request?.downloadHandler is DownloadHandlerBuffer buffer
				? buffer.text
				: ReadBody(bodyPath);

			if (string.IsNullOrEmpty(body))
				return null;

			try {
				var payload = JObject.Parse(body);
				var error   = payload["error"];

				message = error?["message"]?.ToString();
				return error?["code"]?.ToString();
			} catch (Exception e) {
				Logger.LogDebug($"Cannot parse the body of a failed download: {e.Message}", tag: LogTag);
				return null;
			}
		}

		private static string ReadBody(string path) {
			try {
				if (string.IsNullOrEmpty(path) || !File.Exists(path))
					return null;

				var info = new FileInfo(path);
				if (info.Length is <= 0 or > MaxBodySize)
					return null;

				using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
				using var reader = new StreamReader(stream);
				return reader.ReadToEnd();
			} catch (Exception e) {
				Logger.LogDebug($"Cannot read the body of a failed download: {e.Message}", tag: LogTag);
				return null;
			}
		}
	}
}
