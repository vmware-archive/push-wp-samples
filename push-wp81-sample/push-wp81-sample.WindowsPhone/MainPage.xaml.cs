using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Windows.Networking.PushNotifications;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238
using MSSPush_Base.Models;
using MSSPush_Base.Utilities;
using MSSPush_Universal;
using push_wp81_sample.Model;

namespace push_wp81_sample
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private const string VariantUuid = "420ac641-e9da-41fa-a9bf-d028a747bc37";
        private const string VariantSecret = "e4417af0-3f45-4735-9da5-ed6c371682c5";
        private const string BaseUrl = "http://cfms-push-service-dev.main.vchs.cfms-apps.com";
        private const string EnvironmentUuid = "3f19f4a4-67b4-45a9-aa19-e73b9fc8bc68";
        private const string EnvironmentKey = "92d293de-ebf7-4426-8546-b98c8ebb4333";
        private const string DeviceAlias = "BANANAS";

        public MainPage()
        {
            this.InitializeComponent();
            this.NavigationCacheMode = NavigationCacheMode.Required;
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
        }

        private async void RegisterButton_OnClick(object sender, RoutedEventArgs e)
        {
            Log("Registering for push...");
            MSSPush push = MSSPush.SharedInstance;
            MSSParameters parameters = GetMssParameters();
            await push.RegisterForPushAsync(parameters, completionAction: (result) =>
            {
                if (result.Succeeded)
                {
                    Log("Push registration succeeded.");
                    if (result.RawNotificationChannel != null)
                    {
                        Log("Setting up to receive push notifications.");
                        result.RawNotificationChannel.PushNotificationReceived += OnPushNotificationReceived;
                    }
                }
                else
                {
                    Log("Push registration failed: " + result.ErrorMessage + ".");
                }
            });
        }

        private void OnPushNotificationReceived(PushNotificationChannel sender, PushNotificationReceivedEventArgs e)
        {
            if (e != null && e.RawNotification != null && !String.IsNullOrEmpty(e.RawNotification.Content)) {
                Log("Notification received: '" + e.RawNotification.Content + "'.");
            }
            else
            {
                Log("Notification received with no message.");
            }
        }

        private static MSSParameters GetMssParameters()
        {
            return new MSSParameters(VariantUuid, VariantSecret, BaseUrl, DeviceAlias, new HashSet<string>{"TAG1", "TAG2", "TAG4"});
        }

        private void Log(string logString)
        {
            Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                OutputTextBox.Text += "\n" + logString;
                ScrollTextBoxToBottom(OutputTextBox);
                Debug.WriteLine(logString);
            });
        }

        private void ScrollTextBoxToBottom(TextBox textBox)
        {
            var grid = VisualTreeHelper.GetChild(textBox, 0) as Grid;
            if (grid == null)
            {
                return;
            }
            for (var i = 0; i <= VisualTreeHelper.GetChildrenCount(grid) - 1; i++)
            {
                object obj = VisualTreeHelper.GetChild(grid, i);
                if (!(obj is ScrollViewer)) continue;
                ((ScrollViewer)obj).ChangeView(0.0f, ((ScrollViewer)obj).ExtentHeight, 1.0f);
                break;
            }
        } 

        private async void UnregisterButton_OnClick(object sender, RoutedEventArgs e)
        {
            MSSPush push = MSSPush.SharedInstance;
            MSSParameters parameters = GetMssParameters();
            await push.UnregisterForPushAsync(parameters, (result) =>
            {
                if (result.Succeeded)
                {
                    Log("Push unregistration succeeded.");
                }
                else
                {
                    Log("Push unregistration failed: " + result.ErrorMessage + ".");
                }
            });
        }

        private async void TestPushButton_OnClick(object sender, RoutedEventArgs e)
        {

            var httpRequest = WebRequest.CreateHttp(String.Format("{0}/v1/push", BaseUrl));
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
                    Log("This device is not registered for push");
                    return;
                }
                var deviceUuids = new string[] {deviceUuid as String};
                var request = PushRequest.MakePushRequest("This message was pushed at " + System.DateTime.Now, deviceUuids, "raw", "ToastText01", new Dictionary<string, string>() {{"textField1", "This message is all toasty!"}});
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

            using (var reader = new StreamReader(httpResponse.GetResponseStream()))
            {
                string jsonResponse = await reader.ReadToEndAsync();
                Log("Error requesting push message: " + jsonResponse);
            }
        }

        private bool IsSuccessfulHttpStatusCode(HttpStatusCode statusCode)
        {
            return (statusCode >= HttpStatusCode.OK && statusCode < HttpStatusCode.Ambiguous);
        }

        private string BasicAuthorizationValue(string environmentUuid, string environmentKey)
        {
            var stringToEncode = String.Format("{0}:{1}", environmentUuid, environmentKey);
            var data = Encoding.UTF8.GetBytes(stringToEncode);
            var base64 = Convert.ToBase64String(data);
            return String.Format("Basic {0}", base64);
        }
    }
}
