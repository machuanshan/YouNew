using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace YouNewThat
{
    public class HttpRequestInfo
    {
        public bool IsHttps { get; set; }
        public string Host { get; set; }
        public int Port { get; set; }
    }

    public class HttpHeaderParser
    {
        public static async Task<HttpRequestInfo> Parse(Stream requestStream)
        {
            var lines = new List<string>();
            using var streamReader = new StreamReader(requestStream, Encoding.UTF8, false, 1024, true);

            while (true)
            {
                var l = await streamReader.ReadLineAsync();

                if (string.IsNullOrEmpty(l))
                {
                    break;
                }
                else
                {
                    lines.Add(l);
                }
            }


            var hostRow = lines.FirstOrDefault(l => l.StartsWith("host", StringComparison.OrdinalIgnoreCase));

            if (string.IsNullOrEmpty(hostRow))
            {
                throw new InvalidDataException("Invalid http request");
            }
            
            var addrParts = hostRow.Split(':').Skip(1).Select(s => s?.Trim()).ToArray();

            var header = new HttpRequestInfo
            {
                Host = addrParts[0],
                Port = addrParts.Length > 1 ? int.Parse(addrParts[1]) : 0,
                IsHttps = lines[0].StartsWith("connect", StringComparison.OrdinalIgnoreCase)
            };

            if (header.Port == 0)
            {
                header.Port = header.IsHttps ? 443 : 80;
            }

            return header;
        }
    }
}
