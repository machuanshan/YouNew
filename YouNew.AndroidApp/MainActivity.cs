using Android.App;
using Android.OS;
using Android.Support.V7.App;
using Android.Runtime;
using Android.Widget;
using Android.Content;
using System;
using System.IO;
using Xamarin.Essentials;
using YouNewAll;
using System.Security.Cryptography.X509Certificates;
using Android.Support.V7.Widget;
using System.Security.Cryptography;

namespace YouNew.AndroidApp
{
    [Activity(
        Label = "@string/app_name",
        Theme = "@style/AppTheme",
        MainLauncher = true)]
    public class MainActivity : AppCompatActivity
    {
        private const int SelectCertificateRequestCode = 1;
        private Button _startButton;
        private Button _stopButton;
        private EditText _txtServer;
        private EditText _txtThumbprint;
        
        protected override void OnCreate(Bundle savedInstanceState)
        {
            CreateNotificationChannel();
            base.OnCreate(savedInstanceState);
            Xamarin.Essentials.Platform.Init(this, savedInstanceState);

            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.activity_main);

            _txtServer = FindViewById<EditText>(Resource.Id.txtServer);
            _txtServer.Text = Xamarin.Essentials.Preferences.Get(Constants.ServerSettingKey, string.Empty);
            _startButton = FindViewById<Button>(Resource.Id.startProxy);
            _startButton.Click += OnStartProxyButtonClicked;
            _stopButton = FindViewById<Button>(Resource.Id.stopProxy);
            _stopButton.Click += OnStopProxyButtonClicked;

            var _selectCertButton = FindViewById<Button>(Resource.Id.selectCertificate);
            _selectCertButton.Click += OnSelectCertButtonClicked;
            
            CheckCertificate();
            
            var isRunning = IsLocalProxyRunning();
            SetIsServiceRunning(isRunning);
        }

        private void CheckCertificate()
        {
            var pfxFile = GetCertificatePath();

            if (!File.Exists(pfxFile))
            {
                new Android.App.AlertDialog.Builder(this)
                    .SetMessage(Resource.String.need_cert_message)
                    .SetPositiveButton(Resource.String.dlg_yes, (s, e) => SelectCertificate())
                    .SetNegativeButton(Resource.String.dlg_no, (s, e) => FinishAffinity())
                    .Create()
                    .Show();
            }
            else
            {
                ShowThumbprint();
            }
        }

        private string GetCertificatePath()
        {
            return Path.Combine(FileSystem.AppDataDirectory, Constants.LocalCertificateFile);
        }

        private void ShowThumbprint()
        {
            try
            {
                var pwd = Xamarin.Essentials.Preferences.Get(Constants.CertPasswordKey, string.Empty);
                var pfxFile = GetCertificatePath();
                var cert = new X509Certificate2(pfxFile, pwd);

                _txtThumbprint = FindViewById<EditText>(Resource.Id.txtThumbprint);
                _txtThumbprint.Text = cert.Thumbprint;
            }
            catch(CryptographicException)
            {
                Toast.MakeText(this, Resource.String.invalid_pwd_message, ToastLength.Short).Show();
                ShowSetPasswordDialog();
            }
        }

        private void OnSelectCertButtonClicked(object sender, EventArgs e)
        {
            SelectCertificate();
        }

        private void SelectCertificate()
        {
            var intent = new Intent()
                .SetType("*/*")
                .SetAction(Intent.ActionGetContent);
            var title = Resources.GetString(Resource.String.select_certificate);

            StartActivityForResult(Intent.CreateChooser(intent, title), SelectCertificateRequestCode);
        }

        protected override void OnActivityResult(int requestCode, [GeneratedEnum] Result resultCode, Intent data)
        {
            base.OnActivityResult(requestCode, resultCode, data);

            if (requestCode == SelectCertificateRequestCode)
            {
                var pfxFile = GetCertificatePath();

                if (resultCode == Result.Ok)
                {
                    if (File.Exists(pfxFile))
                    {
                        File.Delete(pfxFile);
                    }

                    using var fileStream = ContentResolver.OpenInputStream(data.Data);
                    using var localStream = File.OpenWrite(pfxFile);
                    fileStream.CopyTo(localStream);

                    ShowSetPasswordDialog();
                }
                else
                {
                    if (!File.Exists(pfxFile))
                    {
                        CheckCertificate();
                    }
                }
            }
        }

        private void ShowSetPasswordDialog()
        {
            var view = LayoutInflater.Inflate(Resource.Layout.set_password, null);            
            var dlg = new Android.App.AlertDialog.Builder(this)
                .SetView(view)
                .SetPositiveButton(Resource.String.dlg_ok, default(IDialogInterfaceOnClickListener))
                .SetNegativeButton(Resource.String.dlg_cancel, default(IDialogInterfaceOnClickListener))
                .Create();
            
            dlg.ShowEvent += (s, e) =>
            {
                dlg.GetButton((int)DialogButtonType.Positive).Click += (s, e) =>
                {
                    try
                    {
                        var txtCertPwd = dlg.FindViewById<EditText>(Resource.Id.txtCertPwd);
                        var pwd = txtCertPwd.Text ?? string.Empty;
                        var pfxFile = GetCertificatePath();
                        var cert = new X509Certificate2(pfxFile, pwd);                        
                        Xamarin.Essentials.Preferences.Set(Constants.CertPasswordKey, pwd);
                        dlg.Dismiss();

                        ShowThumbprint();
                    }
                    catch(CryptographicException)
                    {
                        Toast.MakeText(this, Resource.String.invalid_pwd_message, ToastLength.Short).Show();
                    }
                };
            };

            dlg.Show();
        }

        private void SetIsServiceRunning(bool isRunning)
        {
            if (isRunning)
            {
                _startButton.Visibility = Android.Views.ViewStates.Gone;
                _stopButton.Visibility = Android.Views.ViewStates.Visible;
            }
            else
            {
                _startButton.Visibility = Android.Views.ViewStates.Visible;
                _stopButton.Visibility = Android.Views.ViewStates.Gone;
            }
        }

        private bool IsLocalProxyRunning()
        {
            var activityManager = (ActivityManager)GetSystemService(ActivityService);
#pragma warning disable CS0618 // Type or member is obsolete
            var serviceInfoList = activityManager.GetRunningServices(50);
#pragma warning restore CS0618 // Type or member is obsolete

            foreach (var runningServiceInfo in serviceInfoList)
            {
                if (runningServiceInfo.Service.ClassName == Constants.LocalProxyServiceName)
                {
                    return true;
                }
            }

            return false;
        }

        private void OnStopProxyButtonClicked(object sender, EventArgs e)
        {
            var intent = new Intent(this, typeof(LocalProxy));
            intent.PutExtra(Constants.ServiceAction, Constants.StopService);

            if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
            {
                StartForegroundService(intent);
            }
            else
            {
                StartService(intent);
            }

            SetIsServiceRunning(false);
        }

        private void OnStartProxyButtonClicked(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_txtServer.Text))
            {
                Toast.MakeText(this, Resource.String.server_required, ToastLength.Short).Show();
                return;
            }

            if (string.IsNullOrWhiteSpace(_txtThumbprint.Text))
            {
                Toast.MakeText(this, Resource.String.certificate_required, ToastLength.Short).Show();
                return;
            }

            Xamarin.Essentials.Preferences.Set(Constants.ServerSettingKey, _txtServer.Text.Trim());

            var intent = new Intent(this, typeof(LocalProxy));

            if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
            {
                StartForegroundService(intent);
            }
            else
            {
                StartService(intent);
            }

            SetIsServiceRunning(true);
        }

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Android.Content.PM.Permission[] grantResults)
        {
            Xamarin.Essentials.Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);

            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }

        private void CreateNotificationChannel()
        {
            if (Build.VERSION.SdkInt < BuildVersionCodes.O)
            {
                // Notification channels are new in API 26 (and not a part of the
                // support library). There is no need to create a notification
                // channel on older versions of Android.
                return;
            }

            var notificationChannel = new NotificationChannel(
                Constants.NotificationChannelId,
                base.Resources.GetString(Resource.String.notification_channel_name),
                NotificationImportance.Default);
            var notificationManager = (NotificationManager)GetSystemService(NotificationService);
            notificationManager.CreateNotificationChannel(notificationChannel);
        }
    }
}