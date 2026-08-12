using System;
using System.Text.RegularExpressions;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

namespace Nox.CCK.Network {

	/// <summary>
	/// Downloads an image from a URL and applies it to an <see cref="Image"/> or <see cref="RawImage"/>
	/// on the same GameObject. Dynamically appends <c>?size=N</c> to the URL based on the on-screen
	/// pixel size of the target component.
	/// </summary>
	[RequireComponent(typeof(RectTransform))]
	public class NetworkImage : MonoBehaviour {

		[SerializeField]
		[Tooltip("URL to the remote image (e.g. https://example.com/image.png).")]
		private string _url;

		[SerializeField]
		[Tooltip("Multiplier applied to the on-screen size before requesting (default 1 = exact pixels).")]
		private float _sizeMultiplier = 1f;

		private Image _image;
		private RawImage _rawImage;
		private RectTransform _rectTransform;
		private CancellationTokenSource _cts;
		private string _lastResolvedUrl;
		private int _lastRequestedSize = -1;

		// ------------------------------------------------------------------
		// Public API
		// ------------------------------------------------------------------

		/// <summary>Current URL.</summary>
		public string Url {
			get => _url;
			set {
				if (_url == value) return;
				_url = value;
				Refresh();
			}
		}

		/// <summary>Multiplier applied to the on-screen size.</summary>
		public float SizeMultiplier {
			get => _sizeMultiplier;
			set {
				// ReSharper disable once CompareOfFloatsByEqualityOperator
				if (_sizeMultiplier == value) return;
				_sizeMultiplier = value;
				Refresh();
			}
		}

		/// <summary>
		/// (Re-)download the image. Called automatically when <see cref="Url"/> or
		/// <see cref="SizeMultiplier"/> changes. Can also be called manually.
		/// </summary>
		public void Refresh()
			=> LoadAsync().Forget();

		// ------------------------------------------------------------------
		// Unity Lifecycle
		// ------------------------------------------------------------------

		private void Awake() {
			_rectTransform = GetComponent<RectTransform>();
			_image         = GetComponent<Image>();
			_rawImage      = GetComponent<RawImage>();
		}

		private void OnEnable()
			=> LoadAsync().Forget();

		private void OnDisable() {
			CancelCurrent();
			ClearTexture();
		}

		private void OnDestroy()
			=> CancelCurrent();

		#if UNITY_EDITOR
		private void OnValidate() {
			// Auto-resolve component references in the editor
			if (!_rectTransform) _rectTransform = GetComponent<RectTransform>();
			if (!_image)         _image         = GetComponent<Image>();
			if (!_rawImage)      _rawImage      = GetComponent<RawImage>();
		}
		#endif

		// ------------------------------------------------------------------
		// Core logic
		// ------------------------------------------------------------------

		private async UniTask LoadAsync() {
			if (!isActiveAndEnabled) return;
			if (string.IsNullOrEmpty(_url)) {
				ClearTexture();
				return;
			}

			CancelCurrent();
			_cts = new CancellationTokenSource();

			// Wait for end of frame so the Canvas layout has been calculated
			// (otherwise GetWorldCorners returns zero/incorrect values)
			await UniTask.WaitForEndOfFrame(this, _cts.Token);

			if (_cts.IsCancellationRequested) return;

			try {
				var resolvedUrl = ResolveUrl(_url);

				var req = RequestExtension.To(resolvedUrl);
				req.downloadHandler = new DownloadHandlerTexture();
				req.SetRequestHeader("Accept", "image/png, image/jpeg");

				if (await req.Send(_cts.Token) && !_cts.IsCancellationRequested) {
					var texture = await req.Texture(_cts.Token);
					if (texture) {
						ApplyTexture(texture);
						_lastResolvedUrl = resolvedUrl;
						return;
					}
				}

				ClearTexture();
			} catch (OperationCanceledException) {
				// expected when cancelled
			} catch (Exception ex) {
				Debug.LogWarning($"[NetworkImage] Failed to load '{_url}': {ex.Message}", this);
				ClearTexture();
			}
		}

		/// <summary>
		/// Resolves the final URL with a <c>?size=N</c> (or <c>&amp;size=N</c>) parameter
		/// determined by the on-screen pixel dimensions of the target component.
		/// </summary>
		private string ResolveUrl(string baseUrl) {
			if (string.IsNullOrEmpty(baseUrl)) return baseUrl;

			var size = CalculateTargetSize();
			if (size <= 0) return baseUrl; // can't determine size, use raw URL

			// Remove any existing ?size= / &size= parameter
			var cleaned = Regex.Replace(baseUrl, @"[?&]size=\d+", "");
			// Re-add ? or & appropriately
			var separator = cleaned.Contains('?') ? "&" : "?";
			return $"{cleaned}{separator}size={size}";
		}

		/// <summary>
		/// Calculates the pixel size needed based on the actual on-screen pixel dimensions
		/// of the target component. Uses the corners of the RectTransform converted to screen
		/// space, which correctly accounts for Canvas scaling, anchors, and layout.
		/// Returns the longest edge (width or height) multiplied by <see cref="_sizeMultiplier"/>.
		/// </summary>
		private int CalculateTargetSize() {
			if (!_rectTransform) return -1;

			var canvas = _rectTransform.GetComponentInParent<Canvas>();
			if (!canvas) return -1;

			// Get the 4 corners of the rect in world space, then convert to screen pixels
			var corners = new Vector3[4];
			_rectTransform.GetWorldCorners(corners);

			Camera cam = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;

			for (var i = 0; i < 4; i++)
				corners[i] = RectTransformUtility.WorldToScreenPoint(cam, corners[i]);

			var pixelWidth  = Mathf.Abs(corners[2].x - corners[0].x); // top-right.x - bottom-left.x
			var pixelHeight = Mathf.Abs(corners[2].y - corners[0].y); // top-right.y - bottom-left.y
			var maxPixels   = Mathf.Max(pixelWidth, pixelHeight);

			if (maxPixels <= 0f) return -1;

			var size = Mathf.CeilToInt(maxPixels * _sizeMultiplier);

			// Clamp to sane sizes (16 - 4096)
			return Mathf.Clamp(size, 16, 4096);
		}

		private void ApplyTexture(Texture2D texture) {
			if (_image) {
				_image.sprite = Sprite.Create(
					texture,
					new Rect(0, 0, texture.width, texture.height),
					new Vector2(0.5f, 0.5f)
				);
				_image.enabled = true;
				if (_rawImage) _rawImage.enabled = false;
			} else if (_rawImage) {
				_rawImage.texture = texture;
				_rawImage.enabled = true;
			}
		}

		private void ClearTexture() {
			if (_image) {
				_image.sprite = null;
				_image.enabled = false;
			}
			if (_rawImage) {
				_rawImage.texture = null;
				_rawImage.enabled = false;
			}
			_lastResolvedUrl  = null;
			_lastRequestedSize = -1;
		}

		private void CancelCurrent() {
			if (_cts != null) {
				_cts.Cancel();
				_cts.Dispose();
				_cts = null;
			}
		}
	}
}
