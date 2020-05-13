using Org.BouncyCastle.Asn1;
using Org.BouncyCastle.Asn1.Ocsp;
using Org.BouncyCastle.Asn1.X509;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.Ocsp;
using Org.BouncyCastle.X509;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using X509Certificate = Org.BouncyCastle.X509.X509Certificate;
using X509Extension = Org.BouncyCastle.Asn1.X509.X509Extension;

namespace Certera.Core.Helpers
{
    public enum OcspStatus
    {
        Good = 0,
        Revoked = 1,
        Unknown = 2,
        ClientError = 3,
        ServerError = 4
    };

    public class OcspClient
    {
        public OcspStatus GetOcspStatus(X509Certificate2 certificate)
        {
            var issuer = GetIssuerCertificate(certificate);

            return GetOcspStatus(certificate, issuer);
        }

        private OcspStatus GetOcspStatus(X509Certificate2 cert, X509Certificate2 cacert)
        {
            return GetOcspStatus(ConvertToBCX509Certificate(cert), ConvertToBCX509Certificate(cacert));
        }

        private OcspStatus GetOcspStatus(X509Certificate cert, X509Certificate cacert)
        {
            var urls = GetAuthorityInformationAccessOcspUrl(cert);
            if (urls.Count == 0)
            {
                throw new Exception("No OCSP URL found in certificate.");
            }

            var url = urls[0];
            Debug.WriteLine("Sending to :  '" + url + "'...");

            byte[] packtosend = CreateOcspPackage(cert, cacert);

            byte[] response = PostRequest(url, packtosend, "Content-Type", "application/ocsp-request");

            return VerifyResponse(response);
        }

        private byte[] ToByteArray(Stream stream)
        {
            byte[] buffer = new byte[4096 * 8];
            using (var ms = new MemoryStream())
            {
                int read = 0;

                while ((read = stream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    ms.Write(buffer, 0, read);
                }

                return ms.ToArray();
            }
        }

        private byte[] PostRequest(string url, byte[] data, string contentType, string accept)
        {
            var request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = "POST";
            request.ContentType = contentType;
            request.ContentLength = data.Length;
            request.Accept = accept;
            using (var stream = request.GetRequestStream())
            {
                stream.Write(data, 0, data.Length);
            }
            var response = (HttpWebResponse)request.GetResponse();
            using (var respStream = response.GetResponseStream())
            {
                Debug.WriteLine(string.Format("HttpStatusCode : {0}", response.StatusCode.ToString()));
                byte[] resp = ToByteArray(respStream);
                return resp;
            }
        }

        private List<string> GetAuthorityInformationAccessOcspUrl(X509Certificate cert)
        {
            var ocspUrls = new List<string>();

            try
            {
                Asn1Object obj = GetExtensionValue(cert, X509Extensions.AuthorityInfoAccess.Id);

                if (obj == null)
                {
                    return null;
                }
                Asn1Sequence s = (Asn1Sequence)obj;
                IEnumerator elements = s.GetEnumerator();

                while (elements.MoveNext())
                {
                    Asn1Sequence element = (Asn1Sequence)elements.Current;
                    DerObjectIdentifier oid = (DerObjectIdentifier)element[0];

                    if (oid.Id.Equals("1.3.6.1.5.5.7.48.1")) // Is Ocsp?
                    {
                        Asn1TaggedObject taggedObject = (Asn1TaggedObject)element[1];
                        GeneralName gn = GeneralName.GetInstance(taggedObject);
                        ocspUrls.Add(DerIA5String.GetInstance(gn.Name).GetString());
                    }
                }
            }
            catch (Exception e)
            {
                throw new Exception("Error parsing AIA.", e);
            }

            return ocspUrls;
        }

        private OcspStatus VerifyResponse(byte[] response)
        {
            OcspResp r = new OcspResp(response);
            OcspStatus cStatusEnum = OcspStatus.Unknown;
            switch (r.Status)
            {
                case OcspRespStatus.Successful:
                    var or = (BasicOcspResp)r.GetResponseObject();

                    Debug.WriteLine(or.Responses.Length);

                    if (or.Responses.Length == 1)
                    {
                        SingleResp resp = or.Responses[0];

                        var certificateStatus = resp.GetCertStatus();

                        if (certificateStatus == null || certificateStatus == CertificateStatus.Good)
                        {
                            cStatusEnum = OcspStatus.Good;
                        }
                        else if (certificateStatus is RevokedStatus)
                        {
                            cStatusEnum = OcspStatus.Revoked;
                        }
                        else if (certificateStatus is UnknownStatus)
                        {
                            cStatusEnum = OcspStatus.Unknown;
                        }
                    }
                    break;
                case OcspResponseStatus.InternalError:
                case OcspResponseStatus.TryLater:
                    cStatusEnum = OcspStatus.ServerError;
                    break;
                case OcspResponseStatus.MalformedRequest:
                case OcspResponseStatus.SignatureRequired:
                case OcspResponseStatus.Unauthorized:
                    cStatusEnum = OcspStatus.ClientError;
                    break;
                default:
                    Debug.WriteLine($"Unknow status '{r.Status}'.");
                    cStatusEnum = OcspStatus.Unknown;
                    break;
            }

            return cStatusEnum;
        }

        private static byte[] CreateOcspPackage(X509Certificate cert, X509Certificate cacert)
        {
            var gen = new OcspReqGenerator();
            try
            {
                var certId = new CertificateID(CertificateID.HashSha1, cacert, cert.SerialNumber);

                gen.AddRequest(certId);
                gen.SetRequestExtensions(CreateExtension());
                OcspReq req = gen.Generate();

                return req.GetEncoded();
            }
            catch (OcspException e)
            {
                Debug.WriteLine(e.StackTrace);
            }
            catch (IOException e)
            {
                Debug.WriteLine(e.StackTrace);
            }

            return null;
        }

        private static X509Extensions CreateExtension()
        {
            byte[] nonce = new byte[16];
            Hashtable exts = new Hashtable();

            BigInteger nc = BigInteger.ValueOf(DateTime.Now.Ticks);
            X509Extension nonceext = new X509Extension(false, new DerOctetString(nc.ToByteArray()));

            exts.Add(OcspObjectIdentifiers.PkixOcspNonce, nonceext);

            return new X509Extensions(exts);
        }

        public X509Certificate2 GetIssuerCertificate(X509Certificate2 cert)
        {
            // Self Signed Certificate
            if (cert.Subject == cert.Issuer)
            {
                return cert;
            }

            var chain = new X509Chain();
            chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
            chain.Build(cert);
            X509Certificate2 issuer = null;

            if (chain.ChainElements.Count > 1)
            {
                issuer = chain.ChainElements[1].Certificate;
            }
            chain.Reset();

            return issuer;
        }

        private static Asn1Object GetExtensionValue(X509Certificate cert, string oid)
        {
            if (cert == null)
            {
                return null;
            }

            byte[] bytes = cert.GetExtensionValue(new DerObjectIdentifier(oid)).GetOctets();

            if (bytes == null)
            {
                return null;
            }

            var aIn = new Asn1InputStream(bytes);

            return aIn.ReadObject();
        }

        private static X509Certificate ConvertToBCX509Certificate(X509Certificate2 cert)
        {
            var parser = new X509CertificateParser();
            byte[] certarr = cert.Export(X509ContentType.Cert);

            return parser.ReadCertificate(certarr);
        }
    }
}

