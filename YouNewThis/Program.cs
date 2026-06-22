using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using YouNewAll;
using YouNewThat;

namespace YouNewThis
{
    class Program
    {
        static async Task Main(string[] args)
        {
            await Host.CreateDefaultBuilder(args)
                .ConfigureServices(sc =>
                {
                    sc.AddSingleton<Metrics>();
                    sc.AddHostedService<LocalProxy>();
                    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    {
                        sc.AddHostedService<SetWindowsProxy>();
                    }
                })
                .RunConsoleAsync();
        }
    }
}
