using System;
using System.Text;
using Nox.CCK.Network;
using Nox.CCK.Scripting;
using Nox.Scripting;
using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Nox.CCK.Utils;

namespace Nox.Network.Runtime.Modules {
	/// <summary>
	/// Scripting module <c>"http"</c> — HTTP client with an undici-like API.
	///
	/// <para>
	/// All request methods are async and return a <c>Promise&lt;HttpResponse&gt;</c>.
	/// Every request automatically receives standard headers injected by the network layer
	/// (<c>user-agent</c>, <c>x-uuid</c>, <c>x-powered-by</c>, <c>x-nox-mods</c>).
	/// </para>
	///
	/// <code>
	/// import { get, post, put, del, patch, request, query } from 'http';
	///
	/// // Simple GET
	/// const res = await get('https://api.example.com/data');
	/// console.log(res.statusCode, await res.body.text());
	///
	/// // GET with query parameters
	/// const res2 = await get('https://api.example.com/search', {
	///   query: { q: 'hello', page: '1' },
	///   headers: { 'Authorization': 'Bearer token' }
	/// });
	///
	/// // GET with {name} path params (arrays: "." in host, "/" in path)
	/// const res2b = await get('https://{region}.api.example.com/users/{id}/posts', {
	///   params: { region: ['eu', 'west'], id: [123, 456] }
	/// });
	/// // → https://eu.west.api.example.com/users/123/456/posts
	///
	/// // POST with JSON body
	/// const res3 = await post('https://api.example.com/items', {
	///   body: { name: 'foo', value: 42 }
	/// });
	///
	/// // POST with raw string body
	/// const res4 = await post('https://api.example.com/items', {
	///   body: 'raw string',
	///   headers: { 'content-type': 'text/plain' }
	/// });
	///
	/// // Generic request
	/// const res5 = await request('PUT', 'https://api.example.com/items/1', {
	///   body: { id: 1, name: 'updated' }
	/// });
	///
	/// // RFC 10008 — HTTP QUERY (safe & idempotent body-processing method)
	/// const res6 = await query('https://api.example.com/search', {
	///   body: { term: 'hello' }
	/// });
	///
	/// // Response shape (mirrors undici request):
	/// // { statusCode, statusText, headers, body, certificate }
	/// // body is a lazy reader (single-use):
	/// const text  = await res.body.text();   // Promise<string>
	/// const json  = await res.body.json();   // Promise<JToken>
	/// const bytes = await res.body.bytes();  // Promise<byte[]>
	/// // Status helper:
	/// res.ok();
	/// // HTTPS server certificate (when the request is over HTTPS):
	/// const cert = res.certificate;
	/// cert.fingerprint; cert.isValid;
	/// </code>
	/// </summary>
	public static class HttpModule {
		public static readonly IScriptingModuleDefinition Module =
			ScriptingModuleBuilder.Create("http")
				.WithTags("session")
				// ── Verb-based helpers (matches undici pattern) ────────────────
				.AddAsyncMethod("get",     (_, args) => RequestAsync("GET", args))
				.AddAsyncMethod("post",    (_, args) => RequestAsync("POST", args))
				.AddAsyncMethod("put",     (_, args) => RequestAsync("PUT", args))
				.AddAsyncMethod("del",     (_, args) => RequestAsync("DELETE", args))
				.AddAsyncMethod("patch",   (_, args) => RequestAsync("PATCH", args))
				.AddAsyncMethod("head",    (_, args) => RequestAsync("HEAD", args))
				.AddAsyncMethod("options", (_, args) => RequestAsync("OPTIONS", args))
				.AddAsyncMethod("query",   (_, args) => RequestAsync("QUERY", args))
				// ── Generic request (matches undici request(method, url, opts)) ─
				.AddAsyncMethod("request", (_, args) => {
				    if (args.Length < 1)
				        return UniTask.FromResult<object>(null);

				    string method;
				    string url;
				    object opts = null;

				    if (args.Length == 1) {
				        // request(url)
				        method = "GET";
				        url = args[0]?.ToString();
				    } else if (args.Length == 2) {
				        // request(method, url)
				        // request(url, opts)
				        if (args[0] is string first) {
				            method = first;
				            url = args[1]?.ToString();
				        } else {
				            // request(url, opts)
				            method = "GET";
				            url = args[0]?.ToString();
				            opts = args[1];
				        }
				    }
				    else {
				        // request(method, url, opts)
				        method = args[0]?.ToString();
				        url = args[1]?.ToString();
				        opts = args[2];
				    }

				    if (string.IsNullOrEmpty(method) || string.IsNullOrEmpty(url))
				        return UniTask.FromResult<object>(null);

				    return DoRequest(method, url, opts);
				})
				.Build();

		// ── HTTP methods ──────────────────────────────────────────────────────

		/// <summary>
		/// Shared implementation for verb-specific methods (get, post, put, del, …).
		/// args[0] = url, args[1] = optional options object.
		/// </summary>
		private static UniTask<object> RequestAsync(string method, object[] args) {
			if (args.Length == 0 || string.IsNullOrEmpty(args[0]?.ToString()))
				return UniTask.FromResult<object>(null);
			return DoRequest(
				method.ToUpperInvariant(), 
				args[0].ToString(), 
				args.Length > 1 ? args[1] : null
			);
		}

		/// <summary>
		/// Core request implementation. Mirrors undici's <c>request(method, url, opts)</c>.
		///
		/// <para>Options object properties:</para>
		/// <list type="bullet">
		///   <item><c>headers</c> — <c>Record&lt;string, string&gt;</c> or null</item>
		///   <item><c>params</c> — <c>Record&lt;string, string|array&gt;</c> or null; replaces
		///     <c>{name}</c> placeholders in the URL. Arrays join with <c>.</c> in the host
		///     and <c>/</c> in the pathname.</item>
		///   <item><c>body</c> — string (raw), object (auto-JSON-serialized), or null</item>
		///   <item><c>query</c> — <c>Record&lt;string, string&gt;</c> or null (auto-encoded into URL)</item>
		/// </list>
		/// </summary>
		private static async UniTask<object> DoRequest(string method, string url, object opts) {
			var headers = ExtractHeaders(opts);
			var body    = ExtractBody(opts);
			var prms    = ExtractParams(opts);
			var query   = ExtractQuery(opts);

			// Replace {name} placeholders in the URL with params values
			url = ApplyParams(url, prms);

			// Append query parameters to URL
			if (query != null && query.Count > 0)
				url += BuildQueryString(query);

			// Create request via RequestExtension (fires OnCreated → default headers injected)
			var request = RequestExtension.To(url, method);

			// Apply script-provided headers (may override defaults)
			foreach (var header in headers)
				request.SetRequestHeader(header.Key, header.Value);

			// Attach request body
			if (body != null)
				switch (body) {
					case string s:
						request.SetBody(s);
						break;
					case byte[] b:
						request.SetBody(b);
						break;
					default:
						// Auto-serialize object to JSON (matches undici body: JSON.stringify(obj))
						request.SetBody(JsonConvert.SerializeObject(body), "application/json");
						break;
				}

			// Send the request
			await RequestExtension.Send(request);

			// `Body` is a lazy reader object (undici-style body mixins): scripts
			// consume it via `await res.body.text()`, `.json()` or `.bytes()`.
			return request;
		}

		/// <summary>
		/// Lazy reader for the response body (mirrors undici's body mixins).
		/// Each method returns a <c>UniTask&lt;object&gt;</c> so scripting backends wrap it
		/// in a native Promise. Only call <b>one</b> consumer — the body is a single-use stream.
		/// <code>
		/// const text  = await res.body.text();   // Promise&lt;string&gt;
		/// const json  = await res.body.json();   // Promise&lt;JToken&gt;
		/// const bytes = await res.body.bytes();  // Promise&lt;byte[]&gt;
		/// </code>
		/// </summary>
		public sealed class HttpResponseBody {
			private readonly Request _request;

			internal HttpResponseBody(Request request)
				=> _request = request;

			/// <summary>Read the raw response body as a UTF-8 string → Promise&lt;string&gt;.</summary>
			public async UniTask<string> Text()
				=> await _request.Text();

			/// <summary>Parse the response body as JSON → Promise&lt;JToken&gt;.</summary>
			public async UniTask<JToken> Json()
				=> await _request.Json();

			/// <summary>Read the raw response body as bytes → Promise&lt;byte[]&gt;.</summary>
			public async UniTask<byte[]> Bytes()
				=> await _request.Data();
		}

		// ── Type converter ─────────────────────────────────────────────────────

		/// <summary>
		/// Type converter for <see cref="HttpResponseBody"/> so scripting backends expose
		/// the body reader methods (<c>text</c>, <c>json</c>, <c>bytes</c>) explicitly
		/// rather than via generic reflection. Register with
		/// <see cref="IScriptingAPI.RegisterConverter"/>.
		/// </summary>
		public static readonly IScriptingTypeConverter BodyConverter =
			ScriptingTypeConverterBuilder<HttpResponseBody>.Create()
				.AddAsyncMethod("text", async (body, _) => await body.Text())
				.AddAsyncMethod("json", async (body, _) => await body.Json())
				.AddAsyncMethod("bytes", async (body, _) => await body.Bytes())
				.SetDefault((HttpResponseBody)null)
				.Build();

		/// <summary>
		/// Type converter for <see cref="ResponseCertificate"/> exposing the essentials:
		/// the SHA-256 <c>fingerprint</c> and <c>isValid</c> (valid at the current time).
		/// <code>
		/// const cert = res.certificate;
		/// console.log(cert.fingerprint, cert.isValid);
		/// </code>
		/// </summary>
		public static readonly IScriptingTypeConverter CertificateConverter =
			ScriptingTypeConverterBuilder<ResponseCertificate>.Create()
				.AddProperty("fingerprint", cert => cert.PublicKeyFingerprint, flags: ScriptingTypePropertyFlags.InspectGetter)
				.AddProperty("isValid", cert => cert.IsValidNow, flags: ScriptingTypePropertyFlags.InspectGetter)
				.SetDefault((ResponseCertificate)null)
				.Build();

		/// <summary>
		/// Type converter for <see cref="Request"/> that exposes the <b>response</b>
		/// surface on the request object returned by the http helpers
		/// (<c>statusCode</c>, <c>statusText</c>, <c>headers</c>, <c>body</c>, <c>ok</c>).
		/// <code>
		/// const res = await get('https://api.example.com/data');
		/// console.log(res.statusCode, await res.body.text(), res.ok());
		/// </code>
		/// </summary>
		public static readonly IScriptingTypeConverter RequestConverter =
			ScriptingTypeConverterBuilder<Request>.Create()
				.AddProperty("statusCode", (_, req) => (int)req.responseCode)
				.AddProperty("statusText", (_, req) => req.error ?? "")
				.AddProperty("headers",    (_, req) => req.GetResponseHeaders())
				.AddProperty("body",       (_, req) => new HttpResponseBody(req))
				.AddProperty("certificate", (_, req) => req.GetCertificate())
				.AddMethod("ok", 		   (_, req, _2) => req.Ok())
				.SetDefault((Request)null)
				.Build();


		// ── Options-object reading ────────────────────────────────────────────
		//
		// The options object reaches the handler as a live `IDictionary<string, object>` view
		// over the backing JS object via JintTypeAdapter.FromJsValue (PropertyDictionary).
		// No copying, no recursion — reads/writes are forwarded to the JS object in place.
		// These helpers operate on standard dictionaries — no backend-specific
		// duck-typing required.

		/// <summary>Extract headers from the options object.</summary>
		private static Dictionary<string, string> ExtractHeaders(object opts) {
			var result = new Dictionary<string, string>();
			if (opts == null) return result;

			// headers must be a { name: value } map
			if (TryGetProperty(opts, "headers", out var h) && h != null)
				foreach (var kv in EnumerateStringMap(h))
					result[kv.Key] = kv.Value;

			return result;
		}

		/// <summary>Extract body from the options object.</summary>
		private static object ExtractBody(object opts) {
			if (opts == null) return null;
			return TryGetProperty(opts, "body", out var body) ? body : null;
		}

		/// <summary>Extract query parameters from the options object.</summary>
		private static Dictionary<string, string> ExtractQuery(object opts) {
			if (opts == null) return null;
			if (TryGetProperty(opts, "query", out var q) && q != null)
				return EnumerateStringMap(q);
			return null;
		}

		/// <summary>
		/// Extract <c>params</c> from the options object. Unlike <c>query</c>, values are
		/// kept as-is (string or array) because they are used for <c>{name}</c> URL
		/// replacement where arrays join differently by URL part.
		/// </summary>
		private static Dictionary<string, object> ExtractParams(object opts) {
			if (opts == null) return null;
			if (TryGetProperty(opts, "params", out var p) && p != null)
				return EnumerateObjectMap(p);
			return null;
		}

		/// <summary>
		/// Replace <c>{name}</c> placeholders found in <paramref name="url"/> with the
		/// matching <paramref name="params"/> values.
		///
		/// <para>Array values are joined together according to where the placeholder
		/// appears: with <c>.</c> in the host portion, and with <c>/</c> in the
		/// path portion. A plain scalar is used as-is. Missing values leave the
		/// placeholder untouched.</para>
		/// </summary>
		private static string ApplyParams(string url, Dictionary<string, object> prms) {
			if (string.IsNullOrEmpty(url) || prms == null || prms.Count == 0)
				return url;

			// Split the authority (scheme://host[:port]) from the rest (path?query#fragment)
			// so arrays can be joined with "." in the host and "/" in the path.
			int schemeEnd = url.IndexOf("://", StringComparison.Ordinal);
			int authorityStart = schemeEnd < 0 ? 0 : schemeEnd + 3;
			int authorityEnd = url.IndexOfAny(new[] { '/', '?', '#' }, authorityStart);
			if (authorityEnd < 0) authorityEnd = url.Length;

			string authority = url[authorityStart..authorityEnd];
			string rest      = url[authorityEnd..];

			foreach (var kv in prms) {
				var token = "{" + kv.Key + "}";
				if (authority.Contains(token))
					authority = authority.Replace(token, FormatParam(kv.Value, "."));
				if (rest.Contains(token))
					rest = rest.Replace(token, FormatParam(kv.Value, "/"));
			}

			return url[..authorityStart] + authority + rest;
		}

		/// <summary>
		/// Format a single param value for URL replacement. Joins array/collection
		/// elements with <paramref name="separator"/>, or uses the scalar value as-is.
		/// </summary>
		private static string FormatParam(object value, string separator) {
			if (value == null) return string.Empty;

			// Backend arrays arrive as object[]
			if (value is object[] arrObj) {
				var parts = new List<string>();
				foreach (var item in arrObj)
					parts.Add(item?.ToString() ?? "");
				return string.Join(separator, parts);
			}

			// .NET arrays / generic collections (strings excluded — handled below)
			if (value is not string && value is IEnumerable enumerable) {
				var parts = new List<string>();
				foreach (var item in enumerable)
					parts.Add(item?.ToString() ?? "");
				return parts.Count > 0 ? string.Join(separator, parts) : string.Empty;
			}

			return value.ToString();
		}

		/// <summary>
		/// Enumerate a map whose values may be scalars or arrays (params).
		/// </summary>
		private static Dictionary<string, object> EnumerateObjectMap(object map) {
			var result = new Dictionary<string, object>();
			if (map == null) return result;

			switch (map) {
				case IDictionary<string, object> sd:
					foreach (var kv in sd)
						result[kv.Key] = kv.Value;
					return result;
				case IDictionary gd:
					foreach (DictionaryEntry e in gd)
						result[e.Key?.ToString()] = e.Value;
					return result;
			}

			return result;
		}

		/// <summary>
		/// Read a named property from the options dictionary.
		/// </summary>
		private static bool TryGetProperty(object obj, string name, out object value) {
			value = null;
			if (obj == null) return false;

			if (obj is IDictionary<string, object> dict) {
				if (dict.TryGetValue(name, out value))
					return true;
				return false;
			}

			if (obj is IDictionary generic) {
				if (generic.Contains(name)) {
					value = generic[name];
					return true;
				}
				return false;
			}

			return false;
		}

		/// <summary>
		/// Enumerate a string map (headers / query) from a dictionary.
		/// </summary>
		private static Dictionary<string, string> EnumerateStringMap(object map) {
			var result = new Dictionary<string, string>();
			if (map == null) return result;

			switch (map) {
				case IDictionary<string, object> sd:
					foreach (var kv in sd)
						result[kv.Key] = kv.Value?.ToString();
					return result;
				case IDictionary gd:
					foreach (DictionaryEntry e in gd)
						result[e.Key?.ToString()] = e.Value?.ToString();
					return result;
			}

			return result;
		}

		/// <summary>
		/// Build a URL query string from key-value pairs.
		/// Keys and values are URL-encoded via <see cref="Uri.EscapeDataString"/>.
		/// </summary>
		private static string BuildQueryString(Dictionary<string, string> query) {
			if (query == null || query.Count == 0)
				return string.Empty;
			var sb = new StringBuilder();
			sb.Append('?');
			var first = true;
			foreach (var kv in query) {
				if (!first) sb.Append('&');
				sb.Append(Uri.EscapeDataString(kv.Key));
				sb.Append('=');
				sb.Append(Uri.EscapeDataString(kv.Value));
				first = false;
			}
			return sb.ToString();
		}
	}
}
