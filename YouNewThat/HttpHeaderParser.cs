using System;
using System.Buffers;
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
        public byte[] HeaderData { get; set; }
    }

    public class HttpHeaderParser
    {
        public static async Task<HttpRequestInfo> Parse(Stream requestStream)
        {
            var buffer = ArrayPool<byte>.Shared.Rent(1024);

            try
            {
                var lenRead = await requestStream.ReadAsync(buffer);
                var headerData = new Memory<byte>(buffer, 0, lenRead);
                var headerString = Encoding.UTF8.GetString(headerData.Span);
                var lines = headerString.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
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
                    IsHttps = lines[0].StartsWith("connect", StringComparison.OrdinalIgnoreCase),
                    HeaderData = headerData.ToArray()
                };

                if (header.Port == 0)
                {
                    header.Port = header.IsHttps ? 443 : 80;
                }

                return header;
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }
    }
}
