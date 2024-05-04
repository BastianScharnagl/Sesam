using System;

using Android.App;
using Android.Content.PM;
using Android.Runtime;
using Android.OS;
using Android.Service.QuickSettings;
using System.Text;
using Android.Widget;
using Android.Content;
using System.Net.Mqtt;
using Java.Lang;
using AndroidX.Core.App;
using System.Threading.Tasks;

namespace Sesam.Droid
{
    [Activity(Label = "Sesam", Icon = "@mipmap/icon", Theme = "@style/MainTheme", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize)]
    public class MainActivity : global::Xamarin.Forms.Platform.Android.FormsAppCompatActivity
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            Xamarin.Essentials.Platform.Init(this, savedInstanceState);
            global::Xamarin.Forms.Forms.Init(this, savedInstanceState);
            LoadApplication(new App());
        }

        protected override void OnResume()
        {
            base.OnResume();

            //LoadApplication(new App());
        }
        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Android.Content.PM.Permission[] grantResults)
        {
            Xamarin.Essentials.Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);

            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }
    }

    public static class MqttSettings
    {
        public static string Host = YOUR_HOST;
        public static int Port = YOUR_PORT;
        public static string ClientId = YOUR_CLIENTID;
        public static string User = YOUR_USER;
        public static string Password = YOUR_PASSWORD;
    }

    [Service]
    public class MqttIntentService : Service
    {
        public override IBinder OnBind(Intent intent)
        {
            return null;
        }

        public const int ServiceRunningNotifID = 9000;

        public override StartCommandResult OnStartCommand(Intent intent, StartCommandFlags flags, int startId)
        {
            NotificationHelper helper = new NotificationHelper();
            Notification notif = helper.ReturnNotif();
            StartForeground(ServiceRunningNotifID, notif);

            DoLongRunningOperationThings();

            return StartCommandResult.NotSticky;
        }
        
        protected async void DoLongRunningOperationThings()
        {
            try
            {
                var configuration = new MqttConfiguration();
                configuration.Port = MqttSettings.Port;
                var _mqttClient = await MqttClient.CreateAsync(MqttSettings.Host, configuration);
                var _sessionState = await _mqttClient.ConnectAsync(new MqttClientCredentials(MqttSettings.ClientId, MqttSettings.User, MqttSettings.Password));
                _mqttClient.PublishAsync(new MqttApplicationMessage("/home/door", Encoding.UTF8.GetBytes($"open")), MqttQualityOfService.AtLeastOnce).Wait();
                await _mqttClient.DisconnectAsync();
                Toast.MakeText(this, "Door opened", ToastLength.Short).Show();
            }
            catch (System.Exception e)
            {
                Toast.MakeText(this, e.Message, ToastLength.Long).Show();
            }
        }

    }

    [Service(Name = "com.refractored.sesam.SesamTile",
             Permission = Android.Manifest.Permission.BindQuickSettingsTile,
             Label = "Sesam",
             Icon = "@mipmap/key_foreground")]
    [IntentFilter(new[] { ActionQsTile })]
    [MetaData("android.service.quicksettings.ACTIVE_TILE", Value = "true")]
    public class SesamTile : TileService
    {
        //First time tile is added to quick settings
        public override void OnTileAdded()
        {
            base.OnTileAdded();
            var tile = QsTile;
            tile.State = TileState.Inactive;
            tile.UpdateTile();
        }

        //Called each time tile is visible
        public override void OnStartListening()
        {
            base.OnStartListening();
        }

        //Called when tile is no longer visible
        public override void OnStopListening()
        {
            base.OnStopListening();
            var tile = QsTile;
            tile.State = TileState.Inactive;
            tile.UpdateTile();
        }

        //Called when tile is removed by the user
        public override void OnTileRemoved()
        {
            base.OnTileRemoved();
        }
        public override void OnClick()
        {
            var tile = QsTile;
            tile.State = TileState.Active;
            tile.UpdateTile();


            AndroidServiceHelper helper = new AndroidServiceHelper();

            if (IsLocked)
            {
                UnlockAndRun(new Runnable(() =>
                {
                    helper.StartService();
                }));
            }
            else
            {
                helper.StartService();
            }
            //Task.Delay(10000).Wait();

            //helper.StopService();
            tile.State = TileState.Inactive;
            tile.UpdateTile();
            
        }
    }

    public class AndroidServiceHelper
    {
        private static Context context = global::Android.App.Application.Context;

        public void StartService()
        {
            var intent = new Intent(context, typeof(MqttIntentService));

            if (Android.OS.Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.O)
            {
                context.StartForegroundService(intent);
            }
            else
            {
                context.StartService(intent);
            }
        }

        public void StopService()
        {
            var intent = new Intent(context, typeof(MqttIntentService));
            context.StopService(intent);
        }
    }

    public class NotificationHelper
    {
        private static string foregroundChannelId = "9001";
        private static Context context = global::Android.App.Application.Context;


        public Notification ReturnNotif()
        {
            // Building intent
            var intent = new Intent(context, typeof(MainActivity));
            intent.AddFlags(ActivityFlags.SingleTop);
            intent.PutExtra("Title", "Message");

            var pendingIntent = PendingIntent.GetActivity(context, 0, intent, PendingIntentFlags.UpdateCurrent);

            var notifBuilder = new NotificationCompat.Builder(context, foregroundChannelId)
                .SetContentTitle("Your Title")
                .SetContentText("Main Text Body")
                .SetOngoing(true)
                .SetContentIntent(pendingIntent);

            // Building channel if API verion is 26 or above
            if (global::Android.OS.Build.VERSION.SdkInt >= BuildVersionCodes.O)
            {
                NotificationChannel notificationChannel = new NotificationChannel(foregroundChannelId, "Title", NotificationImportance.High);
                notificationChannel.Importance = NotificationImportance.High;
                notificationChannel.EnableLights(true);
                notificationChannel.EnableVibration(true);
                notificationChannel.SetShowBadge(true);
                notificationChannel.SetVibrationPattern(new long[] { 100, 200, 300, 400, 500, 400, 300, 200, 400 });

                var notifManager = context.GetSystemService(Context.NotificationService) as NotificationManager;
                if (notifManager != null)
                {
                    notifBuilder.SetChannelId(foregroundChannelId);
                    notifManager.CreateNotificationChannel(notificationChannel);
                }
            }

            return notifBuilder.Build();
        }
    }
}