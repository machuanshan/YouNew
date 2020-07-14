using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace YouNewKeyGen
{
    class Program
    {
        static void Main(string[] args)
        {
            if(args.Length != 6)
            {
                Console.WriteLine("younewkeygen -n [sub-name] -p [password] -o [ouput-file]");
                return;
            }

            try
            {
                var parameters = new Dictionary<string, string>();
                parameters.Add(args[0], args[1]);
                parameters.Add(args[2], args[3]);
                parameters.Add(args[4], args[5]);

                CreateSelfSignedCertificate(parameters["-n"], parameters["-o"], parameters["-p"]);
                Console.WriteLine("Certificate is generated successfully");
            }
            catch(Exception e)
            {
                Console.WriteLine(e);
            }
        }

        public static void CreateSelfSignedCertificate(string subjectName, string pfxName, string password)
        {
            var sanBuilder = new SubjectAlternativeNameBuilder();
            sanBuilder.AddIpAddress(IPAddress.Loopback);

            var distinguishedName = new X500DistinguishedName($"CN={subjectName}");

            using (RSA rsa = RSA.Create(2048))
            {
                var request = new CertificateRequest(distinguishedName, rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

                request.CertificateExtensions.Add(
                    new X509KeyUsageExtension(X509KeyUsageFlags.DataEncipherment | X509KeyUsageFlags.KeyEncipherment | X509KeyUsageFlags.DigitalSignature, false));

                request.CertificateExtensions.Add(
                   new X509EnhancedKeyUsageExtension(
                       new OidCollection { new Oid("1.3.6.1.5.5.7.3.1"), new Oid("1.3.6.1.5.5.7.3.2") }, false));

                request.CertificateExtensions.Add(sanBuilder.Build());

                var certificate = request.CreateSelfSigned(new DateTimeOffset(DateTime.UtcNow.AddDays(-1)), new DateTimeOffset(DateTime.UtcNow.AddYears(20)));
                certificate.FriendlyName = subjectName;

                var data = certificate.Export(X509ContentType.Pfx, password);
                File.WriteAllBytes(pfxName, data);
            }
        }
    }
}
