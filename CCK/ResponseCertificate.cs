using System;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using UnityEngine.Networking;

namespace Nox.CCK.Network {
    public class ResponseCertificate : CertificateHandler {
        private readonly CertificateHandler Handler;

        /// <summary>
        /// Raw DER encoded certificate received from the server.
        /// </summary>
        internal byte[] Data { get; private set; }

        private X509Certificate2 _certificate;

        /// <summary>
        /// Parsed X509 certificate.
        /// </summary>
        private X509Certificate2 Certificate
            => _certificate ??= new X509Certificate2(Data);

        public ResponseCertificate() { }

        public ResponseCertificate(CertificateHandler handler)
            => Handler = handler;

        protected override bool ValidateCertificate(byte[] data) {
            Data = data;

            // Parse immediately so all properties are available after the request.
            try {
                _certificate = new X509Certificate2(data);
            }
            catch {
                _certificate = null;
            }

            if (Handler is null)
                return true;

            var type = Handler.GetType();
            var method = type.GetMethod(nameof(ValidateCertificate));

            return method != null
                && (bool)method.Invoke(Handler, new object[] { data });
        }

        // ─────────────────────────────────────────────────────────────
        // Raw certificate
        // ─────────────────────────────────────────────────────────────

        /// <summary>
        /// SHA-256 fingerprint of the DER certificate.
        /// Example: AA:BB:CC:DD:...
        /// </summary>
        public string Fingerprint {
            get {
                if (Data == null)
                    return null;

                using var sha = SHA256.Create();
                return FormatFingerprint(sha.ComputeHash(Data));
            }
        }

        /// <summary>
        /// Certificate encoded as PEM.
        /// </summary>
        public string Pem {
            get {
                if (Data == null)
                    return null;

                var base64 = Convert.ToBase64String(Data);
                var sb = new StringBuilder();

                sb.AppendLine("-----BEGIN CERTIFICATE-----");

                for (int i = 0; i < base64.Length; i += 64)
                    sb.AppendLine(base64.Substring(i, Math.Min(64, base64.Length - i)));

                sb.AppendLine("-----END CERTIFICATE-----");

                return sb.ToString();
            }
        }

        // ─────────────────────────────────────────────────────────────
        // Certificate identity
        // ─────────────────────────────────────────────────────────────

        public string Subject
            => _certificate?.Subject;

        public string Issuer
            => _certificate?.Issuer;

        public string SerialNumber
            => _certificate?.SerialNumber;

        public int Version
            => _certificate?.Version ?? 0;

        // ─────────────────────────────────────────────────────────────
        // Validity
        // ─────────────────────────────────────────────────────────────

        public DateTime NotBefore
            => _certificate?.NotBefore ?? default;

        public DateTime NotAfter
            => _certificate?.NotAfter ?? default;

        public bool IsValidNow {
            get {
                if (_certificate == null)
                    return false;

                var now = DateTime.UtcNow;

                return now >= _certificate.NotBefore.ToUniversalTime()
                    && now <= _certificate.NotAfter.ToUniversalTime();
            }
        }

        public int DaysUntilExpiration {
            get {
                if (_certificate == null)
                    return 0;

                return (int)Math.Floor(
                    (_certificate.NotAfter.ToUniversalTime() - DateTime.UtcNow).TotalDays
                );
            }
        }

        // ─────────────────────────────────────────────────────────────
        // Cryptography
        // ─────────────────────────────────────────────────────────────

        public string SignatureAlgorithm
            => _certificate?.SignatureAlgorithm?.FriendlyName;

        public string SignatureAlgorithmOid
            => _certificate?.SignatureAlgorithm?.Value;

        public string PublicKeyAlgorithm
            => _certificate?.PublicKey?.Oid?.FriendlyName;

        public string PublicKeyAlgorithmOid
            => _certificate?.PublicKey?.Oid?.Value;

        /// <summary>
        /// Subject public key encoded as Base64.
        /// </summary>
        public string PublicKey
            => _certificate == null
                ? null
                : Convert.ToBase64String(_certificate.PublicKey.EncodedKeyValue.RawData);

        /// <summary>
        /// SHA-256 hash of the subject public key.
        /// Useful for SPKI-style public-key pinning.
        /// </summary>
        public string PublicKeyFingerprint {
            get {
                if (_certificate == null)
                    return null;
                using var sha = SHA256.Create();
                var key = _certificate.PublicKey.EncodedKeyValue.RawData;
                return FormatFingerprint(sha.ComputeHash(key));
            }
        }

        // ─────────────────────────────────────────────────────────────
        // Helpers
        // ─────────────────────────────────────────────────────────────

        private static string FormatFingerprint(byte[] hash)
            => BitConverter
                .ToString(hash)
                .Replace("-", ":");
    }
}