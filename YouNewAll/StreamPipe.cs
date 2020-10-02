using System;
using System.Buffers;
using System.IO;
using System.Threading.Tasks;

namespace YouNewAll
{
    public static class StreamPipe
    {
        private const int MinBufferSize = 1024;
        private const int MaxBufferSize = 1024 * 1024;

        private static async Task Pipe(Stream source, Stream target)
        {
            var bufferSize = MinBufferSize;
            var buffer = default(byte[]);

            while (true)
            {
                try
                {
                    buffer = ArrayPool<byte>.Shared.Rent(bufferSize);
                    var lenRead = await source.ReadAsync(buffer, 0, buffer.Length);

                    if (lenRead == 0)
                    {
                        break;
                    }
                    
                    await target.WriteAsync(buffer, 0, lenRead);
                    
                    if (lenRead >= bufferSize)
                    {
                        if (bufferSize < MaxBufferSize)
                        {
                            bufferSize *= 2;
                        }
                    }     
                    else if(lenRead <= bufferSize / 2)
                    {
                        if(bufferSize > MinBufferSize)
                        {
                            bufferSize /= 2;
                        }
                    }
                }
                finally
                {
                    if (buffer != null)
                    {
                        ArrayPool<byte>.Shared.Return(buffer);
                    }
                }
            }
        }

        public static async Task DuplexPipe(Stream stream1, Stream stream2)
        {
            await Task.WhenAny(
                    Pipe(stream1, stream2),
                    Pipe(stream2, stream1));
        }
    }
}
