using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Notification;
using MSSPush_Base.Models;
using MSSPush_Base.Utilities;
using MSSPush_WP8;
using MSSPush_WP8.Models;
using push_wp8_sample.Model;

namespace PushWP8Sample
{
    public partial class MainPage : PhoneApplicationPage
    {
        #region Constants

        // Set to your "Variant UUID", as provided by the Pivotal Mobile Services Suite console
        private const string VariantUuid = "bd9060e3-77cc-4e68-ab1a-572cb0f8a5f2";

        // Set to your "Variant Secret" as provided by the Pivotal Mobile Services Suite console
        private const string VariantSecret = "83c775f8-cbca-40b3-b1d9-b805dadd43c8";

        // Set to your instance of the Pivotal Mobile Services Suite server providing your push services.
        private const string BaseServerUrl = "http://cfms-push-service-dev.main.vchs.cfms-apps.com";

        // Set to your own defined alias for this device.  May be null.
        private const string DeviceAlias = "TACOS";

        // Set to your own defined tags. May be null.
        private static readonly List<string> Tags = new List<string> { "SampleTag1", "SampleTag2"};

        // This is PCFMS environment associated with mobile app
        private const string EnvironmentUuid = "a6b0ffd6-f944-46b9-89f9-132c5550ba92";
        private const string EnvironmentKey = "647d9c48-5ce5-4196-807c-e8fec679d38d";

        #endregion

        #region Properties

        private HttpNotificationChannel _channel;
        private MSSParameters _parameters;
        public MSSParameters Parameters
        {
            get
            {
                return _parameters ?? (_parameters = new MSSParameters(VariantUuid, VariantSecret, BaseServerUrl, DeviceAlias, null /*Tags*/));
            }
        }

       
        // Using a DependencyProperty as the backing store for Logs.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty LogsProperty =
            DependencyProperty.Register("Logs", typeof(ObservableCollection<string>), typeof(MainPage), new PropertyMetadata(null));

        #endregion

        // Constructor
        public MainPage()
        {
            InitializeComponent();
        }

        #region WP8 Push Client SDK Methods

        private async void StartPushRegistration()
        {
            //Example of how to register for Push with the Pivotal Mobile Services Suite Push Server
            try
            {
                await MSSPush.SharedInstance.RegisterForPushAsync(Parameters, null, OnRegistrationCompleted);
            }
            catch (Exception e)
            {
                Log("Registration verification failed.");
                Log(e.ToString());
            }
        }

        private async void StartPushUnregistration()
        {
            //Example of how to unregister for Push with the Pivotal Mobile Services Suite Push Server
            try
            {
                await MSSPush.SharedInstance.UnregisterForPushAsync(Parameters, OnUnregistrationCompleted);
            }
            catch (Exception e)
            {
                Log("Unregistration verification failed.");
                Log(e.ToString());
            }
        }

        private void Log(string message)
        {
            Dispatcher.BeginInvoke(() =>
            {
                Debug.WriteLine(message);
                OutputTextBox.Text += "\n" + message;
                // Scroll to bottom
                OutputScrollViewer.UpdateLayout();
                OutputScrollViewer.ScrollToVerticalOffset(OutputScrollViewer.ScrollableHeight);
            });

        }

        #endregion

        #region Completion Actions

        private void OnRegistrationCompleted(HttpCompletionArgs args)
        {
            if (args.Succeeded)
            {
                Log("Successfully registered for Push.");

                //e.g. HttpNotificationChannel can be accessed from the args for additional use
                _channel = args.RawNotificationChannel;
                _channel.BindToShellToast();
                _channel.ShellToastNotificationReceived += ChannelOnShellToastNotificationReceived;
            }
            else
            {
                Log("Failed to register for Push.");
                Log(args.ErrorMessage);
            }
        }

        private void ChannelOnShellToastNotificationReceived(object sender, NotificationEventArgs e)
        {
            if (e != null && e.Collection != null && e.Collection.ContainsKey("wp:Text1"))
            {
                Log("Notification received: '" + e.Collection["wp:Text1"] + "'.");
            }
            else
            {
                Log("Notification received with no message.");
            }
        }

        private void OnUnregistrationCompleted(HttpCompletionArgs args)
        {
            if (args.Succeeded)
            {
                Log("Successfully unregistered for Push.");
            }
            else
            {
                Log("Failed to unregister for Push.");
                Log(args.ErrorMessage);
            }
        }

        #endregion

        #region Button Handlers

        private void RegisterButton_OnClick(object sender, RoutedEventArgs e)
        {
            Log("Registering...");
            StartPushRegistration();
        }

        private void UnregisterButton_OnClick(object sender, RoutedEventArgs e)
        {
            Log("Unregistering...");
            StartPushUnregistration();
        }

        private async void TestPushButton_OnClick(object sender, RoutedEventArgs e)
        {
            var httpRequest = WebRequest.CreateHttp(String.Format("{0}/v1/push", BaseServerUrl));
            httpRequest.Method = "POST";
            httpRequest.Accept = "application/json";
            httpRequest.Headers[HttpRequestHeader.Authorization] = BasicAuthorizationValue(EnvironmentUuid, EnvironmentKey);

            httpRequest.ContentType = "application/json; charset=UTF-8";
            using (var stream = await Task.Factory.FromAsync<Stream>(httpRequest.BeginGetRequestStream, httpRequest.EndGetRequestStream, null))
            {
                var settings = new Settings();
                object deviceUuid;
                if (!settings.TryGetValue("PushDeviceUuid", out deviceUuid))
                {
                    Log("This device is not registered for push.");
                    return;
                }
                var deviceUuids = new string[] { deviceUuid as String };
                var request = PushRequest.MakePushRequest("This message was pushed at " + System.DateTime.Now, deviceUuids, "raw", "ToastText01", new Dictionary<string, string>() { { "textField1", "This message is all toasty!" } });
                var jsonSerializer = new JsonSerializer();
                var jsonString = jsonSerializer.SerializeToJson(request);
                var bytes = Encoding.UTF8.GetBytes(jsonString);
                stream.Write(bytes, 0, bytes.Length);
            }

            WebResponse webResponse;
            try
            {
                webResponse = await Task.Factory.FromAsync<WebResponse>(httpRequest.BeginGetResponse, httpRequest.EndGetResponse, null);
            }
            catch (WebException ex)
            {
                webResponse = ex.Response;
            }

            var httpResponse = webResponse as HttpWebResponse;
            if (httpResponse == null)
            {
                Log("Error requesting push message: Unexpected/invalid response type. Unable to parse JSON.");
                return;
            }

            if (IsSuccessfulHttpStatusCode(httpResponse.StatusCode))
            {
                Log("Server accepted message for delivery.");
                return;
            }

            string jsonResponse = null;
            using (var reader = new StreamReader(httpResponse.GetResponseStream()))
            {
                jsonResponse = await reader.ReadToEndAsync();
                Log("Error requesting push message: " + jsonResponse);
            }
        }

        private string BasicAuthorizationValue(string environmentUuid, string environmentKey)
        {
            var stringToEncode = String.Format("{0}:{1}", environmentUuid, environmentKey);
            var data = Encoding.UTF8.GetBytes(stringToEncode);
            var base64 = Convert.ToBase64String(data);
            return String.Format("Basic {0}", base64);
        }
        
        private bool IsSuccessfulHttpStatusCode(HttpStatusCode statusCode)
        {
            return (statusCode >= HttpStatusCode.OK && statusCode < HttpStatusCode.Ambiguous);
        }

        #endregion

        private void OnPageRoot_OrientationChanged(object sender, OrientationChangedEventArgs e)
        {
            if (e.Orientation == PageOrientation.LandscapeLeft || e.Orientation == PageOrientation.LandscapeRight)
            {
                Grid.SetRow(buttonList, 0);
                Grid.SetColumn(buttonList, 0);
                Grid.SetRow(OutputScrollViewer, 0);
                Grid.SetColumn(OutputScrollViewer, 1);
            }
            else if(e.Orientation ==  PageOrientation.PortraitUp)
            {
                Grid.SetRow(buttonList, 0);
                Grid.SetColumn(buttonList, 0);
                Grid.SetRow(OutputScrollViewer, 1);
                Grid.SetColumn(OutputScrollViewer, 0);
            }
            else
            {
                Debug.WriteLine("confused about orientation");
            }
        }
    }
}