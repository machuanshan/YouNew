using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using YouNewAll;

namespace YouNewThis
{
    internal class LocalProxy : BackgroundService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<LocalProxy> _logger;
        private X509Certificate2 _clientCertificate;

        public LocalProxy(IConfiguration configuration, ILogger<LocalProxy> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                var pwd = _configuration.GetValue<string>("keyPassword");
                _clientCertificate = new X509Certificate2("local.pfx", pwd);
                _logger.LogInformation($"Client certificate thumbprint: {_clientCertificate.Thumbprint}");

                var port = _configuration.GetValue("localPort", 5000);
                var localServer = new TcpListener(IPAddress.Any, port);
                localServer.Start();
                _logger.LogInformation($"Listening at port {port}");
                stoppingToken.Register(() => localServer.Stop());

                while (true)
                {
                    var client = await localServer.AcceptTcpClientAsync();
                    ProcessClientRequest(client);
                }
            }
            catch(ObjectDisposedException)
            {
                _logger.LogInformation("Server stopped");
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error occurred on servicing");
            }
        }

        private async void ProcessClientRequest(TcpClient client1)
        {
            _logger.LogInformation($"Accept client: {client1.Client.RemoteEndPoint}");
            var client2 = new TcpClient();
            var stream2 = default(SslStream);

            try
            {
                var stream1 = client1.GetStream();
                var server = _configuration.GetValue<string>("server");
                var port = _configuration.GetValue("serverPort", 5001);

                _logger.LogInformation($"Connecting to server: {server}:{port}");
                await client2.ConnectAsync(server, port);
                stream2 = new SslStream(
                    innerStream: client2.GetStream(),
                    leaveInnerStreamOpen: false,
                    userCertificateValidationCallback: ValidateServerCertificate);

                stream2.AuthenticateAsClient(
                    targetHost: server,
                    clientCertificates: new X509CertificateCollection(new[] { _clientCertificate }),
                    checkCertificateRevocation: false);

                await StreamPipe.DuplexPipe(stream1, stream2);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error on piping stream");
            }
            finally
            {
                try { stream2?.Close(); } catch { }
                try { client1.Close(); } catch { }
                try { client2.Close(); } catch { }
            }
        }

        private bool ValidateServerCertificate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            var cert2 = certificate as X509Certificate2;
            _logger.LogInformation("Validating remote certificate: " + cert2.Thumbprint);
            _logger.LogInformation("Validation result: " + sslPolicyErrors);

            if (sslPolicyErrors == SslPolicyErrors.RemoteCertificateChainErrors)
            {
                foreach (var s in chain.ChainStatus)
                {
                    _logger.LogInformation($"Chain status: {s.Status}, {s.StatusInformation}");
                }
            }

            return true;
        }
    }
}