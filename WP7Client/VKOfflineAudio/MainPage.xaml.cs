using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Notification;
using System.Text;
using RestSharp;
using System.ComponentModel;
using System.IO.IsolatedStorage;
using System.Runtime.Serialization;
using PlayerAgent;
using System.Windows.Navigation;
using Microsoft.Phone.BackgroundAudio;
using System.Net.NetworkInformation;
using System.Diagnostics;

namespace VKOfflineAudio
{
    public partial class MainPage : PhoneApplicationPage
    {
        RestClient client = new RestClient("http://udmtaxi.ru/vkofflinemusic/");
        bool isOnline;
        // Constructor
        public MainPage()
        {
            InitializeComponent();
            Loaded += new RoutedEventHandler(MainPage_Loaded);
            NetworkChange.NetworkAddressChanged += new NetworkAddressChangedEventHandler(NetworkChange_NetworkAddressChanged);
            updateNetworkState();
            
        }

        void NetworkChange_NetworkAddressChanged(object sender, EventArgs e)
        {
            updateNetworkState();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            updateNetworkState();
            base.OnNavigatedTo(e);
        }

        void updateNetworkState()
        {
            if (NetworkInterface.GetIsNetworkAvailable())
            {
                iconnState.Text = Microsoft.Phone.Net.NetworkInformation.NetworkInterface.NetworkInterfaceType.ToString();
                //iconnState.Text = NetworkInterface.networkInterfacetype
            }
            else
            {
                iconnState.Text = "Offline";
            }
            isOnline = onlineMode.IsChecked.GetValueOrDefault(false);
            if (isOnline)
            {
                if (pushChannel == null)
                {
                    initPush(true);
                }
                getAudioz(Settings.instance.devid);
            }
        }

        List<Song> Playlist
        {
            get
            {
                return AudioPlayer.playlist;
            }
            set
            {
                AudioPlayer.playlist = value;
                PlayListBox.ItemsSource = value;
            }
        }

        void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
            if (Settings.instance.devid == "")
            {
                NavigationService.Navigate(new Uri("/Scanner.xaml", UriKind.Relative));
            }
            try
            {
                Playlist = (List<Song>)IsolatedStorageSettings.ApplicationSettings["playlist"];
                //BackgroundAudioPlayer.Instance.Track = Playlist[(int)IsolatedStorageSettings.ApplicationSettings["index"]].AudioTrack;
            }
            catch (Exception ex)
            {

            }
            //initPush(false);
            //getAudioz(Settings.instance.devid);
        }
        HttpNotificationChannel pushChannel = null;
        void initPush(bool force)
        {
            // The name of our push channel.
            string channelName = "VKOfflineAudio";

            // Try to find the push channel.
            pushChannel = HttpNotificationChannel.Find(channelName);

            // If the channel was not found, then create a new connection to the push service.
            if (pushChannel == null)
            {
                if (force)
                {
                    pushChannel = new HttpNotificationChannel(channelName);

                    // Register for all the events before attempting to open the channel.
                    pushChannel.ChannelUriUpdated += new EventHandler<NotificationChannelUriEventArgs>(PushChannel_ChannelUriUpdated);
                    pushChannel.ErrorOccurred += new EventHandler<NotificationChannelErrorEventArgs>(PushChannel_ErrorOccurred);

                    // Register for this notification only if you need to receive the notifications while your application is running.
                    pushChannel.ShellToastNotificationReceived += new EventHandler<NotificationEventArgs>(PushChannel_ShellToastNotificationReceived);

                    pushChannel.Open();

                    // Bind this new channel for toast events.
                    pushChannel.BindToShellToast();
                }

            }
            else
            {
                // The channel was already open, so just register for all the events.
                pushChannel.ChannelUriUpdated += new EventHandler<NotificationChannelUriEventArgs>(PushChannel_ChannelUriUpdated);
                pushChannel.ErrorOccurred += new EventHandler<NotificationChannelErrorEventArgs>(PushChannel_ErrorOccurred);

                // Register for this notification only if you need to receive the notifications while your application is running.
                pushChannel.ShellToastNotificationReceived += new EventHandler<NotificationEventArgs>(PushChannel_ShellToastNotificationReceived);

                // Display the URI for testing purposes. Normally, the URI would be passed back to your web service at this point.
                processPushUri(pushChannel.ChannelUri.ToString());

            }
        }
        void PushChannel_ChannelUriUpdated(object sender, NotificationChannelUriEventArgs e)
        {
            processPushUri(e.ChannelUri.ToString());
        }

        void processPushUri(string uri)
        {
            client.PostAsync(new RestRequest("/regid/{devid}").AddUrlSegment("devid",Settings.instance.devid).AddParameter("text/plain",uri,ParameterType.RequestBody),(response,handle)=>{
                if (response.Content == "OK") ;
            });
        }


        void PushChannel_ErrorOccurred(object sender, NotificationChannelErrorEventArgs e)
        {
            // Error handling logic for your particular application would be here.
            Dispatcher.BeginInvoke(() =>
                MessageBox.Show(String.Format("A push notification {0} error occurred.  {1} ({2}) {3}",
                    e.ErrorType, e.Message, e.ErrorCode, e.ErrorAdditionalData))
                    );
        }

        void PushChannel_ShellToastNotificationReceived(object sender, NotificationEventArgs e)
        {
            StringBuilder message = new StringBuilder();
            string relativeUri = string.Empty;
            message.Append("Playlist updated");
            //message.AppendFormat("Received Toast {0}:\n", DateTime.Now.ToShortTimeString());

            /*// Parse out the information that was part of the message.
            foreach (string key in e.Collection.Keys)
            {
                message.AppendFormat("{0}: {1}\n", key, e.Collection[key]);

                if (string.Compare(
                    key,
                    "wp:Param",
                    System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.CompareOptions.IgnoreCase) == 0)
                {
                    relativeUri = e.Collection[key];
                }
            }*/

            // Display a dialog of all the fields in the toast.
            if (isOnline)
            {
                Dispatcher.BeginInvoke(() => { getAudioz(Settings.instance.devid); /*MessageBox.Show(message.ToString());*/ });
            }

        }
        Downloader dl = null;

        void getAudioz(string devid)
        {
            client.ExecuteAsyncGet<List<Song>>(new RestRequest("/getauds/{id}").AddUrlSegment("id", devid), (response, handle) =>
            {
                try
                {
                    if (response.ErrorException == null)
                    {
                        if (response.StatusCode == HttpStatusCode.OK)
                        {
                            var r = response.Data;
                            dl = new Downloader();
                            dl.setSongs(r);
                            dl.download();
                            System.IO.IsolatedStorage.IsolatedStorageSettings.ApplicationSettings["playlist"] = r;
                            System.IO.IsolatedStorage.IsolatedStorageSettings.ApplicationSettings.Save();
                            Playlist = r;
                        }
                    }
                }
                catch (Exception ex)
                { }
            },"GET");
        }

        private void PlayListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (PlayListBox.SelectedIndex >= 0)
            {
                BackgroundAudioPlayer.Instance.Track = ((Song)PlayListBox.SelectedItem).AudioTrack;
                AudioPlayer.currentPlPosition = PlayListBox.SelectedIndex;
                IsolatedStorageSettings.ApplicationSettings["index"] = PlayListBox.SelectedIndex;
            }
            //
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            BackgroundAudioPlayer.Instance.SkipPrevious();
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            BackgroundAudioPlayer.Instance.SkipNext();
        }

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            if (BackgroundAudioPlayer.Instance.PlayerState == PlayState.Playing)
            {
                BackgroundAudioPlayer.Instance.Pause();
            }
            else
            {
                BackgroundAudioPlayer.Instance.Play();
            }
        }

        private void ApplicationBarIconButton_Click(object sender, EventArgs e)
        {
            //initPush(true);
            getAudioz(Settings.instance.devid);
        }

        private void ApplicationBarIconButton_Click_1(object sender, EventArgs e)
        {
            NavigationService.Navigate(new Uri("/Scanner.xaml", UriKind.Relative));
        }
        private void onlineMode_Checked(object sender, RoutedEventArgs e)
        {
            
        }

        private void onlineMode_Click(object sender, RoutedEventArgs e)
        {
            updateNetworkState();
        }
    }
}