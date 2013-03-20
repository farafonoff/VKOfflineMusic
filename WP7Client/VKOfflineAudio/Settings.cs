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
using System.IO.IsolatedStorage;

namespace VKOfflineAudio
{
    public class Settings
    {
        private static Settings _inst = new Settings();
        public static Settings instance
        {
            get
            {
                return _inst;
            }
        }

        public string devid
        {
            get { return get<string>("guid", ""); }
            set { put("guid", value); }
        }

        void put(string key, object val)
        {
            IsolatedStorageSettings.ApplicationSettings[key] = val;
        }
        T get<T>(string key, T def)
        {
            if (!IsolatedStorageSettings.ApplicationSettings.Contains(key)) return def;
            object val = IsolatedStorageSettings.ApplicationSettings[key];
            if (val != null) return ((T)val); else return def;
        }


        internal void save()
        {
            IsolatedStorageSettings.ApplicationSettings.Save();
        }
    }
}
