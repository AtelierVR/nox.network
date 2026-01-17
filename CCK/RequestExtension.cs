using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Nox.CCK.Events;
using Nox.CCK.Utils;
using UnityEngine;
using UnityEngine.Networking;
using Logger = Nox.CCK.Utils.Logger;
namespace Nox.CCK.Network {
	public static class RequestExtension {
		/// <summary>
		/// Fusionne une URL de base avec un chemin relatif.
		/// Gère automatiquement les slashes pour éviter les doublons.
		/// </summary>
		public static string MergeUrl(string url, string path) {
			if (string.IsNullOrEmpty(url)) return path ?? string.Empty;
			if (string.IsNullOrEmpty(path)) return url;

			var urlEndsWithSlash = url[^1] == '/';
			var pathStartsWithSlash = path[0] == '/';

			var builder = new StringBuilder();

			lock (builder) {
				builder.Clear();
				builder.EnsureCapacity(url.Length + path.Length + 1);

				if (urlEndsWithSlash) {
					builder.Append(url, 0, url.Length - 1);
				}
				else builder.Append(url);

				builder.Append('/');

				switch (pathStartsWithSlash) {
					case true when path.Length > 1:
						builder.Append(path, 1, path.Length - 1);
						break;
					case false:
						builder.Append(path);
						break;
				}

				return builder.ToString();
			}
		}

		public static class Method {
			public const string GET = "GET";
			public const string POST = "POST";
			public const string PUT = "PUT";
			public const string DELETE = "DELETE";
			public const string HEAD = "HEAD";
			public const string OPTIONS = "OPTIONS";
			public const string PATCH = "PATCH";
		}

		public static readonly NoxEvent<Request> OnCreated = new();
		public static readonly NoxEventAsync<UnityWebRequest> OnBeforeSend = new();
		public static readonly NoxEventAsync<UnityWebRequest> OnCompleted = new();

		public static Request To(string url) {
			var req = new Request(url) { method = Method.GET, downloadHandler = new DownloadHandlerBuffer() };
			OnCreated.Invoke(req);
			return req;
		}

		public static void SetBody(this UnityWebRequest request, JObject json, string contentType = "application/json")
			=> request.SetBody((JToken)json, contentType);

		public static void SetBody(this UnityWebRequest request, JToken json, string contentType = "application/json") {
			if (json == null) throw new ArgumentNullException(nameof(json));
			request.SetBody(json.ToString(), contentType);
		}

		public static void SetBody(this UnityWebRequest request, string body, string contentType = "text/plain") {
			if (body == null) throw new ArgumentNullException(nameof(body));
			request.SetBody(Encoding.UTF8.GetBytes(body), contentType);
		}

		public static void SetBody(this UnityWebRequest request, byte[] body, string contentType = "application/octet-stream") {
			if (body == null) throw new ArgumentNullException(nameof(body));
			request.uploadHandler = new UploadHandlerRaw(body);
			request.SetRequestHeader("Content-Type", contentType);
		}

		public static void SetBody(this UnityWebRequest request, List<IMultipartFormSection> formData) {
			if (formData == null) throw new ArgumentNullException(nameof(formData));

			var boundary = UnityWebRequest.GenerateBoundary();
			var formSections = UnityWebRequest.SerializeFormSections(formData, boundary);
			var contentType = $"multipart/form-data; boundary={Encoding.UTF8.GetString(boundary)}";

			request.uploadHandler = new UploadHandlerRaw(formSections);
			request.SetRequestHeader("Content-Type", contentType);
		}


		public static void SetBody<T>(this UnityWebRequest request, T obj, string contentType = "application/json") {
			if (obj == null) throw new ArgumentNullException(nameof(obj));
			request.SetBody(JsonConvert.SerializeObject(obj), contentType);
		}

		public static void SetHeaders(this UnityWebRequest request, IReadOnlyDictionary<string, string> headers) {
			if (headers == null) return;
			foreach (var header in headers)
				request.SetRequestHeader(header.Key, header.Value);
		}

		public static bool IsError(this UnityWebRequest request)
			=> request.result is UnityWebRequest.Result.ConnectionError
				or UnityWebRequest.Result.ProtocolError
				or UnityWebRequest.Result.DataProcessingError;

		public static bool IsSuccess(this UnityWebRequest request)
			=> request.result == UnityWebRequest.Result.Success;

		public static bool IsProcessing(this UnityWebRequest request)
			=> request.result == UnityWebRequest.Result.InProgress;

		public static async UniTask<bool> Send(this UnityWebRequest request, CancellationToken token = default) {
			if (request.IsSent()) {
				Logger.LogDebug("Request has already been sent. Waiting for completion...");
				await request.Wait(token);
				return request.IsSuccess();
			}

			await OnBeforeSend.InvokeAsync(request);

			if (token.IsCancellationRequested) {
				request.Abort();
				Logger.LogWarning("Request was cancelled before sending.");
				return false;
			}

			try {
				await request.SendWebRequest()
					.ToUniTask(cancellationToken: token);

				if (!request.IsSuccess())
					throw new Exception("The request completed with an error.");

				return true;
			} catch (Exception ex) {
				Logger.LogError(new Exception(
					$"Request to {request.method} {request.url} failed with exception: {request.result} - {request.error}",
					ex
				));
				return false;
			}
			finally {
				await OnCompleted.InvokeAsync(request);
			}
		}

		public static bool Ok(this UnityWebRequest request)
			=> request.IsSuccess() && request.responseCode is >= 200 and < 300;

		public static UniTask Wait(this UnityWebRequest request, CancellationToken token = default)
			=> UniTask.WaitUntil(() => request.isDone || token.IsCancellationRequested, cancellationToken: token);

		public static bool IsSent(this UnityWebRequest request)
			=> request.isDone || request.result != UnityWebRequest.Result.InProgress;

		public static async UniTask<string> Text(this UnityWebRequest request, CancellationToken token = default) {
			if (request.downloadHandler == null)
				throw new InvalidOperationException($"The {nameof(request.downloadHandler)} is null. Ensure the request was properly initialized with a {typeof(DownloadHandler)}.");
			if (request.IsProcessing()) await request.Wait(token);
			return request.downloadHandler.text;
		}

		public static async UniTask<T> Json<T>(this UnityWebRequest request, CancellationToken token = default)
			=> JsonConvert.DeserializeObject<T>(await request.Text(token));

		public static async UniTask<JToken> Json(this UnityWebRequest request, CancellationToken token = default)
			=> JToken.Parse(await request.Text(token));

		public static async UniTask<byte[]> Data(this UnityWebRequest request, CancellationToken token = default) {
			if (request.downloadHandler == null)
				throw new InvalidOperationException($"The {nameof(request.downloadHandler)} is null. Ensure the request was properly initialized with a {typeof(DownloadHandler)}.");
			if (request.IsProcessing()) await request.Wait(token);
			return request.downloadHandler.data;
		}

		public static async UniTask<Texture2D> Texture(this UnityWebRequest request, CancellationToken token = default) {
			if (request.downloadHandler is not DownloadHandlerTexture textureHandler)
				throw new InvalidOperationException($"{nameof(request.downloadHandler)} is not of type {typeof(DownloadHandlerTexture)}.");
			if (request.IsProcessing()) await request.Wait(token);
			return textureHandler.texture;
		}

		public static async UniTask<AudioClip> AudioClip(this UnityWebRequest request, CancellationToken token = default) {
			if (request.downloadHandler is not DownloadHandlerAudioClip audioHandler)
				throw new InvalidOperationException($"{nameof(request.downloadHandler)} is not of type {typeof(DownloadHandlerAudioClip)}.");
			if (request.IsProcessing()) await request.Wait(token);
			return audioHandler.audioClip;
		}

		public static async UniTask<AssetBundle> AssetBundle(this UnityWebRequest request, CancellationToken token = default) {
			if (request.downloadHandler is not DownloadHandlerAssetBundle bundleHandler)
				throw new InvalidOperationException($"{nameof(request.downloadHandler)} is not of type {typeof(DownloadHandlerAssetBundle)}.");
			if (request.IsProcessing()) await request.Wait(token);
			return bundleHandler.assetBundle;
		}

		public static void HandleDownloadProgress(this UnityWebRequest request, Action<float, ulong> callback, CancellationToken token = default) {
			if (callback == null) return;

			OnProgress().Forget();
			return;

			async UniTask OnProgress() {
				await UniTask.SwitchToMainThread();

				var lastProgress = -1f;
				var crtProgress = 0f;
				var lastBytes = 0ul;
				var crtBytes = 0ul;

				while (!request.isDone) {
					if (token.IsCancellationRequested) break;

					crtProgress = request.downloadProgress;
					crtBytes = request.downloadedBytes;

					if (Math.Abs(crtProgress - lastProgress) < 0.001f && crtBytes == lastBytes) {
						await UniTask.NextFrame();
						continue;
					}

					lastProgress = crtProgress;
					lastBytes = crtBytes;
					callback.Invoke(crtProgress, crtBytes);

					await UniTask.Yield();
				}
			}
		}

		public static void HandleUploadProgress(this UnityWebRequest request, Action<float, ulong> callback, CancellationToken token = default) {
			if (callback == null) return;

			OnProgress().Forget();
			return;

			async UniTask OnProgress() {
				await UniTask.SwitchToMainThread();

				var lastProgress = -1f;
				var crtProgress = 0f;
				var lastBytes = 0ul;
				var crtBytes = 0ul;

				while (!request.isDone) {
					if (token.IsCancellationRequested) break;

					crtProgress = request.uploadProgress;
					crtBytes = request.uploadedBytes;

					if (Math.Abs(crtProgress - lastProgress) < 0.001f && crtBytes == lastBytes) {
						await UniTask.NextFrame();
						continue;
					}

					lastProgress = crtProgress;
					lastBytes = crtBytes;
					callback.Invoke(crtProgress, crtBytes);

					await UniTask.Yield();
				}
			}
		}
	}
}