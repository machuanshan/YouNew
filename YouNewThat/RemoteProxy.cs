using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using YouNewAll;

namespace YouNewThat
{
    internal class RemoteProxy : BackgroundService
    {
        private readonly Metrics _metrics;
        private readonly IConfiguration _configuration;
        private readonly ILogger<RemoteProxy> _logger;
        private X509Certificate2 _serverCertificate;
        
        public RemoteProxy(
            Metrics metrics,
            IConfiguration configuration, 
            ILogger<RemoteProxy> logger)
        {
            _metrics = metrics;
            _configuration = configuration;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                var pwd = _configuration.GetValue<string>("keyPassword") ?? string.Empty;
                var pfxName = "server.pfx";
                _serverCertificate = File.Exists(pfxName) ?
                    new X509Certificate2(pfxName, pwd) :
                    CertificateUtils.CreateSelfSignedCertificate(Environment.MachineName, pwd, pfxName);

                var port = _configuration.GetValue("port", 5001);
                var server = new TcpListener(IPAddress.Any, port);
                server.Start();
                _logger.LogInformation($"Server is listening on port: {port}");
                stoppingToken.Register(() => server.Stop());

                while (true)
                {
                    var client = await server.AcceptTcpClientAsync();
                    ProcessClientRequest(client);
                }
            }
            catch(ObjectDisposedException)
            {
                _logger.LogInformation("Server stopped");
            }
            catch(Exception e)
            {
                _logger.LogError(e, "Error occurred on servicing");
            }
        }

        private async void ProcessClientRequest(TcpClient client1)
        {
            _logger.LogInformation($"Accept client: {client1.Client.RemoteEndPoint}");
            var client2 = new TcpClient();
            var stream1 = default(SslStream);

            try
            {
                _metrics.ConnectionCreated();

                stream1 = new SslStream(
                innerStream: client1.GetStream(),
                leaveInnerStreamOpen: false,
                userCertificateValidationCallback: ValidateClientCertificate);

                await stream1.AuthenticateAsServerAsync(
                            serverCertificate: _serverCertificate,
                            clientCertificateRequired: true,
                            checkCertificateRevocation: false);

                var header = await HttpHeaderParser.Parse(stream1);

                if (header.IsHttps)
                {
                    SendProxyOK(stream1);                    
                }

                _logger.LogInformation($"Connecting to {header.Host}:{header.Port}");
                await client2.ConnectAsync(header.Host, header.Port);
                var stream2 = client2.GetStream();

                if(!header.IsHttps)
                {
                    await stream2.WriteAsync(header.HeaderData);
                }

                await StreamPipe.DuplexPipe(stream1, stream2);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error on piping stream");
            }
            finally
            {
                _metrics.ConnectionClosed();
                try { stream1?.Close(); } catch { }
                try { client1.Close(); } catch { }
                try { client2.Close(); } catch { }
            }
        }

        private void SendProxyOK(Stream stream)
        {
            var responseBytes = Encoding.UTF8.GetBytes("HTTP/1.1 200 Connection established\r\n\r\n");
            stream.Write(responseBytes, 0, responseBytes.Length);
            stream.Flush();
        }

        private bool ValidateClientCertificate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            if (certificate == null) return false;

            var cert2 = certificate as X509Certificate2;
            _logger.LogInformation("Validate remote certificate: " + cert2.Thumbprint);

            var allowedClients = _configuration
                .GetValue("allowedClients", string.Empty)
                .Split(';', StringSplitOptions.RemoveEmptyEntries)
                .Select(t => t.Trim())
                .ToArray();

            var allowedClient = allowedClients.Contains(cert2.Thumbprint, StringComparer.OrdinalIgnoreCase);

            if (!allowedClient)
            {
                _logger.LogError("Client certificate is not trusted, thumbprint: " + cert2.Thumbprint);
                return false;
            }

            _logger.LogInformation("Validation result: " + sslPolicyErrors);

            if (sslPolicyErrors == SslPolicyErrors.RemoteCertificateChainErrors)
            {
                foreach (var s in chain.ChainStatus)
                {
                    _logger.LogError($"Chain status: {s.Status}, {s.StatusInformation}");
                }
            }

            return true;
        }
    }
}