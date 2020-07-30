using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;

namespace YouNew.AndroidApp
{
    internal static class Constants
    {
        public const string NotificationChannelId = "YouNew.General";
        public const string ServiceAction = "ServiceAction";
        public const string StopService = "StopService";
    }
}