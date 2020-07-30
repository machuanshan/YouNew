using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using System;
using System.Drawing;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Xamarin.Essentials;
using YouNewAll;

namespace YouNew.AndroidApp
{
    [Service(Name = Constants.LocalProxyServiceName)]
    internal class LocalProxy : Service
    {
        public const int SERVICE_RUNNING_NOTIFICATION_ID = 10000;

        private X509Certificate2 _clientCertificate;
        
        [return: GeneratedEnum]
        public override StartCommandResult OnStartCommand(Intent intent, [GeneratedEnum] StartCommandFlags flags, int startId)
        {
            if (intent.GetStringExtra(Constants.ServiceAction) == Constants.StopService)
            {
                StopForeground(true);
                StopSelf(SERVICE_RUNNING_NOTIFICATION_ID);
            }
            else
            {
                var notification = new Notification.Builder(this, Constants.NotificationChannelId)
                    .SetContentTitle(Resources.GetString(Resource.String.proxy_notification_title))
                    .SetSmallIcon(Resource.Drawable.ic_noti)
                    .SetContentIntent(BuildIntentToShowMainActivity())
                    // user cannot dismiss the notification
                    .SetOngoing(true)
                    .AddAction(BuildStopServiceAction())
                    .Build();

                // Enlist this instance of the service as a foreground service
                StartForeground(SERVICE_RUNNING_NOTIFICATION_ID, notification);
            }

            return base.OnStartCommand(intent, flags, startId);
        }

        private Notification.Action BuildStopServiceAction()
        {
            var intent = new Intent(this, GetType());
            intent.PutExtra(Constants.ServiceAction, Constants.StopService);

            // PendingIntent is for notification system to use the inner Intent object on behalf of owner application of the Intent
            // this pendingIntent object can only be used once, so we set it with OneShot flag
            var pendingIntent = PendingIntent.GetForegroundService(this, 0, intent, PendingIntentFlags.OneShot);
            
            return new Notification.Action(Resource.Drawable.ic_stop_proxy, Resources.GetString(Resource.String.stop_proxy), pendingIntent);
        }

        private PendingIntent BuildIntentToShowMainActivity()
        {
            var intent = new Intent(this, typeof(MainActivity));
            // Start new or bring the existing to current
            intent.SetFlags(ActivityFlags.NewTask);

            // this PendingIntent can be used multiple times to bring up main activity
            return PendingIntent.GetActivity(this, 0, intent, PendingIntentFlags.UpdateCurrent);
        }

        public async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                var pwd = Preferences.Get(Constants.CertPasswordKey, string.Empty);
                var pfxFile = Path.Combine(FileSystem.AppDataDirectory, "local.pfx");

                if (!File.Exists(pfxFile))
                {
                    return;
                }

                _clientCertificate = new X509Certificate2(pfxFile, pwd);
                
                var port = Preferences.Get("localPort", 5000);
                var localServer = new TcpListener(IPAddress.Any, port);
                localServer.Start();
                stoppingToken.Register(() => localServer.Stop());

                while (true)
                {
                    var client = await localServer.AcceptTcpClientAsync();
                    ProcessClientRequest(client);
                }
            }
            catch(ObjectDisposedException)
            {
                //_logger.LogInformation("Server stopped");
            }
            catch (Exception e)
            {
                //_logger.LogError(e, "Error occurred on servicing");
            }
        }

        public override IBinder OnBind(Intent intent)
        {
            throw new NotImplementedException();
        }

        private async void ProcessClientRequest(TcpClient client1)
        {
            //_logger.LogInformation($"Accept client: {client1.Client.RemoteEndPoint}");
            var client2 = new TcpClient();
            var stream2 = default(SslStream);

            try
            {
                var stream1 = client1.GetStream();
                var server = Preferences.Get(Constants.ServerSettingKey, string.Empty);
                var port = Preferences.Get("serverPort", 5001);

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
                //_logger.LogError(e, "Error on piping stream");
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
            
            if (sslPolicyErrors == SslPolicyErrors.RemoteCertificateChainErrors)
            {
                foreach (var s in chain.ChainStatus)
                {
                    //_logger.LogInformation($"Chain status: {s.Status}, {s.StatusInformation}");
                }
            }

            return true;
        }
    }
}