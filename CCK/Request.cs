using System;
using System.Collections.Generic;
using Nox.CCK.Utils;
using UnityEngine.Networking;

namespace Nox.CCK.Network {
	public class Request : UnityWebRequest {
		public readonly Dictionary<string, string> Headers = new();

		public Request(string url) : base(url)
			=> downloadHandler = new DownloadHandlerBuffer();

		public Request(string url, string method) : base(url, method)
			=> downloadHandler = new DownloadHandlerBuffer();

		public Request(string url, string method, DownloadHandler download, UploadHandler upload, CertificateHandler certificate) : base(url, method, download, upload)
			=> certificateHandler = certificate;
		
		public new void SetRequestHeader(string name, string value) {
			Headers[name] = value;
			base.SetRequestHeader(name, value);
		}

		public void RemoveRequestHeader(string name) {
			base.SetRequestHeader(name, null);
			Headers.Remove(name);
		}

		public Dictionary<string, string> GetRequestHeaders()
			=> Headers;

		/// <summary>
		/// Server certificate exported as PEM, or <c>null</c> when the request is not HTTPS.
		/// </summary>
		public ResponseCertificate GetCertificate() 
			=> certificateHandler as ResponseCertificate;
	}
}