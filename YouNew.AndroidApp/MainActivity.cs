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

namespace YouNew.AndroidApp
{
    [Activity(
        Label = "@string/app_name",
        Theme = "@style/AppTheme",
        MainLauncher = true)]
    public class MainActivity : AppCompatActivity
    {
        private Button _startButton;
        private Button _stopButton;
        private EditText _txtServer;
        private EditText _txtThumbprint;
        private EditText _txtCertPwd;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            CreateNotificationChannel();
            base.OnCreate(savedInstanceState);
            Xamarin.Essentials.Platform.Init(this, savedInstanceState);

            CheckCertificate();
            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.activity_main);

            _txtCertPwd = FindViewById<EditText>(Resource.Id.txtCertPwd);
            _txtCertPwd.FocusChange += TxtCertPwdFocusChange;
            _txtServer = FindViewById<EditText>(Resource.Id.txtServer);
            _txtServer.Text = Xamarin.Essentials.Preferences.Get(Constants.ServerSettingKey, string.Empty);
            _startButton = FindViewById<Button>(Resource.Id.startProxy);
            _startButton.Click += OnStartProxyButtonClicked;
            _stopButton = FindViewById<Button>(Resource.Id.stopProxy);
            _stopButton.Click += OnStopProxyButtonClicked;

            var _selectCertButton = FindViewById<Button>(Resource.Id.selectCertificate);
            _selectCertButton.Click += OnSelectCertButtonClicked;
            var isRunning = IsLocalProxyRunning();
            SetIsServiceRunning(isRunning);

            SetThumbprint();
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
        }

        private string GetCertificatePath()
        {
            return Path.Combine(FileSystem.AppDataDirectory, Constants.LocalCertificateFile);
        }

        private void TxtCertPwdFocusChange(object sender, Android.Views.View.FocusChangeEventArgs e)
        {
            if (!e.HasFocus)
            {
                Xamarin.Essentials.Preferences.Set(Constants.CertPasswordKey, _txtCertPwd.Text ?? string.Empty);
                SetThumbprint();
            }
        }

        private void SetThumbprint()
        {
            var pwd = Xamarin.Essentials.Preferences.Get(Constants.CertPasswordKey, string.Empty);
            var pfxFile = Path.Combine(FileSystem.AppDataDirectory, "local.pfx");

            var cert = File.Exists(pfxFile) ? new X509Certificate2(pfxFile, pwd) : null;

            _txtThumbprint = FindViewById<EditText>(Resource.Id.txtThumbprint);
            _txtThumbprint.Text = cert?.Thumbprint ?? string.Empty;
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

            StartActivityForResult(Intent.CreateChooser(intent, title), 1);
        }

        protected override void OnActivityResult(int requestCode, [GeneratedEnum] Result resultCode, Intent data)
        {
            base.OnActivityResult(requestCode, resultCode, data);

            if (requestCode == 1)
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