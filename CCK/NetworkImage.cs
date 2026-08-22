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
			=> LoadAsync(forceReload: true).Forget();

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
		private string _prevUrl;
		private float _prevMultiplier;

		private void OnValidate() {
			// Auto-resolve component references in the editor
			if (!_rectTransform) _rectTransform = GetComponent<RectTransform>();
			if (!_image)         _image         = GetComponent<Image>();
			if (!_rawImage)      _rawImage      = GetComponent<RawImage>();

			// Force reload if URL or multiplier changed in editor
			if (_prevUrl != _url || !Mathf.Approximately(_prevMultiplier, _sizeMultiplier)) {
				_prevUrl = _url;
				_prevMultiplier = _sizeMultiplier;
				
				// Only reload if URL is not empty
				if (!string.IsNullOrEmpty(_url)) {
					Refresh();
				}
			}
		}

		private void OnDrawGizmos() {
			if (!_rectTransform) return;
			
			var size = CalculateTargetSize();
			if (size <= 0) return;

			// Draw label at the center of the RectTransform
			var style = new GUIStyle(GUI.skin.label) {
				alignment = TextAnchor.MiddleCenter,
				fontSize = 12,
				normal = { textColor = Color.cyan }
			};

			var center = _rectTransform.position;
			UnityEditor.Handles.Label(center, $"{size}px", style);
		}
		#endif

		// ------------------------------------------------------------------
		// Core logic
		// ------------------------------------------------------------------

		private async UniTask LoadAsync(bool forceReload = false) {
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
				var size = CalculateTargetSize();
				
				// Skip download if we already have this size or better (unless forced)
				if (!forceReload && size > 0 && size <= _lastRequestedSize && !string.IsNullOrEmpty(_lastResolvedUrl)) {
					return;
				}

				var resolvedUrl = ResolveUrl(_url, size);

				var req = RequestExtension.To(resolvedUrl);
				req.downloadHandler = new DownloadHandlerTexture();
				req.SetRequestHeader("Accept", "image/png, image/jpeg");

				if (await req.Send(_cts.Token) && !_cts.IsCancellationRequested) {
					var texture = await req.Texture(_cts.Token);
					if (texture) {
						ApplyTexture(texture);
						_lastResolvedUrl = resolvedUrl;
						_lastRequestedSize = size;
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
		/// Resolves the final URL with a <c>?size=N</c> (or <c>&amp;size=N</c>) parameter.
		/// </summary>
		private string ResolveUrl(string baseUrl, int size) {
			if (string.IsNullOrEmpty(baseUrl)) return baseUrl;
			if (size <= 0) return baseUrl; // can't determine size, use raw URL

			// Remove any existing ?size= / &size= parameter
			var cleaned = Regex.Replace(baseUrl, @"[?&]size=\d+", "");
			// Re-add ? or & appropriately
			var separator = cleaned.Contains('?') ? "&" : "?";
			return $"{cleaned}{separator}size={size}";
		}

		/// <summary>
		/// Calculates the pixel size needed based on the actual on-screen pixel dimensions
		/// of the target component. Uses the rect size multiplied by the canvas scale factor.
		/// Returns the longest edge (width or height) multiplied by <see cref="_sizeMultiplier"/>.
		/// </summary>
		private int CalculateTargetSize() {
			if (!_rectTransform) return -1;

			var canvas = _rectTransform.GetComponentInParent<Canvas>();
			if (!canvas) return -1;

			// Get the canvas scale factor (handles Canvas Scaler properly)
			float scaleFactor = GetCanvasScaleFactor(canvas);

			// Use the rect's size directly, multiplied by the scale factor
			var rectSize = _rectTransform.rect.size;
			var pixelWidth  = Mathf.Abs(rectSize.x * scaleFactor);
			var pixelHeight = Mathf.Abs(rectSize.y * scaleFactor);
			var maxPixels   = Mathf.Max(pixelWidth, pixelHeight);

			if (maxPixels <= 0f) return -1;

			var size = Mathf.CeilToInt(maxPixels * _sizeMultiplier);

			// Clamp to sane sizes (16 - 4096)
			return Mathf.Clamp(size, 16, 4096);
		}

		/// <summary>
		/// Gets the scale factor that converts RectTransform units to screen pixels.
		/// Handles all Canvas render modes and Canvas Scaler configurations.
		/// </summary>
		private float GetCanvasScaleFactor(Canvas canvas) {
			if (canvas.renderMode == RenderMode.ScreenSpaceOverlay) {
				// For overlay canvas, use the canvas scale directly
				return canvas.scaleFactor;
			}

			// For Screen Space - Camera and World Space, calculate from camera
			var cam = canvas.worldCamera;
			if (!cam) return 1f;

			// Get corners in world space
			var corners = new Vector3[4];
			_rectTransform.GetWorldCorners(corners);

			// Convert to screen points and measure
			var bl = RectTransformUtility.WorldToScreenPoint(cam, corners[0]);
			var tr = RectTransformUtility.WorldToScreenPoint(cam, corners[2]);

			var screenDist = Vector2.Distance(bl, tr);
			var worldDist = Vector3.Distance(corners[0], corners[2]);

			if (worldDist <= 0f) return 1f;
			return screenDist / worldDist;
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
