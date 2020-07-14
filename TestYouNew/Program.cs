using System;
using System.Net;

namespace TestYouNew
{
    class Program
    {
        static void Main(string[] args)
        {
            WebClient wc = new WebClient();
            wc.Proxy = new WebProxy("127.0.0.1", 5000);
            var content = wc.DownloadString("https://www.163.com");
            Console.WriteLine(content);
        }
    }
}
