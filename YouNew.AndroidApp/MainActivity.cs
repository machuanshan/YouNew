using Android.App;
using Android.OS;
using Android.Support.V7.App;
using Android.Runtime;
using Android.Widget;
using Android.Content;
using System;
using System.Collections.Generic;

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

        protected override void OnCreate(Bundle savedInstanceState)
        {
            CreateNotificationChannel();
            base.OnCreate(savedInstanceState);
            Xamarin.Essentials.Platform.Init(this, savedInstanceState);
            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.activity_main);

            _startButton = FindViewById<Button>(Resource.Id.startProxy);
            _startButton.Click += OnStartProxyButtonClicked;
            _stopButton = FindViewById<Button>(Resource.Id.stopProxy);
            _stopButton.Click += OnStopProxyButtonClicked;

            var isRunning = IsLocalProxyRunning();
            SetIsServiceRunning(isRunning);
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