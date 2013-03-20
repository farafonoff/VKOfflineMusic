using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Runtime.Serialization;
using System.ComponentModel;
using System.IO.IsolatedStorage;
using Microsoft.Phone.BackgroundAudio;

namespace VKOfflineAudio
{
    [DataContractAttribute]
    public class Song : INotifyPropertyChanged
    {
        [DataMemberAttribute]
        public string url { get; set; }
        [DataMemberAttribute]
        public long aid { get; set; }
        [DataMemberAttribute]
        public int duration { get; set; }
        [DataMemberAttribute]
        public string artist { get; set; }
        [DataMemberAttribute]
        public string title { get; set; }

        public bool onDisk
        {
            get
            {
                return IsolatedStorageFile.GetUserStoreForApplication().FileExists(string.Format("{0}.mp3", aid));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public void NotifyPropertyChanged(string prop)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(prop));
            }
        }
        public Uri uri
        {
            get { return new Uri(string.Format("{0}.mp3", aid), UriKind.Relative); }
        }
        private AudioTrack _track = null;
        public Microsoft.Phone.BackgroundAudio.AudioTrack AudioTrack
        {
            get
            {
                if (_track == null)
                {
                    _track = new AudioTrack(uri,title,artist,"Vkontakte",null);
                }
                return _track;
            }
        }
    }
}
