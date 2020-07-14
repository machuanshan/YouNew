using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Threading.Tasks;

namespace YouNewThat
{
    class Program
    {
        static async Task Main(string[] args)
        {
            await Host.CreateDefaultBuilder(args)
                .ConfigureServices(sc => sc.AddHostedService<RemoteProxy>())
                .RunConsoleAsync();
        }
    }
}
