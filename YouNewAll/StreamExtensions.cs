using System;
using System.IO;
using System.Threading.Tasks;

namespace YouNewAll
{
    public static class StreamPipe
    {
        private static async Task Pipe(Stream source, Stream target, int bufferSize = 1024 * 16)
        {
            var buffer = new byte[bufferSize];
            
            while (true)
            {
                var len = await source.ReadAsync(buffer, 0, buffer.Length);

                if(len == 0)
                {
                    break;
                }

                await target.WriteAsync(buffer, 0, len);
            }
        }

        public static async Task DuplexPipe(Stream stream1, Stream stream2, int upBufferSize = 1024 * 16, int downBufferSize = 1024 * 1024)
        {
            await Task.WhenAny(
                    Pipe(stream1, stream2, upBufferSize),
                    Pipe(stream2, stream1, downBufferSize));
        }
    }
}
