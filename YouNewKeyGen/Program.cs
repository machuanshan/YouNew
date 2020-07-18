using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using YouNewAll;

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

                CertificateUtils.CreateSelfSignedCertificate(parameters["-n"], parameters["-o"], parameters["-p"]);
                Console.WriteLine("Certificate is generated successfully");
            }
            catch(Exception e)
            {
                Console.WriteLine(e);
            }
        }
    }
}
