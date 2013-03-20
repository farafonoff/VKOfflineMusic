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
using System.Collections.Generic;
using System.IO.IsolatedStorage;
using System.IO;
using Microsoft.Xna.Framework.Media;
using System.Linq;
namespace VKOfflineAudio
{
    public class Downloader
    {
        List<Song> job;
        int _curr = 0;
        Song current
        {
            get { return job[_curr]; }
        }
        internal void setSongs(System.Collections.Generic.List<Song> list)
        {
            job = list;
        }

        internal void download()
        {
            if (job == null) return;
            if (job.Count == 0) return;
            _curr = 0;
            next();
        }

        void cleanup()
        {
            IsolatedStorageFile isolatedStorageFile = IsolatedStorageFile.GetUserStoreForApplication();
            string[] filez = isolatedStorageFile.GetFileNames("*.mp3");
            foreach(var f in filez)
            {
                long id;
                long.TryParse(f.Split('.')[0],out id);
                if (job.FirstOrDefault(s => s.aid == id) == null)
                {
                    isolatedStorageFile.DeleteFile(f);
                }
            }

        }


        void next()
        {
            while(_curr<job.Count&&current.onDisk)
            {
                ++_curr;
            }
            if (_curr == job.Count) return;
            WebClient webClient = new WebClient();
            webClient.DownloadProgressChanged += new DownloadProgressChangedEventHandler(webClient_DownloadProgressChanged);
            webClient.OpenReadCompleted += new OpenReadCompletedEventHandler(webClient_OpenReadCompleted);
            webClient.OpenReadAsync(new Uri(current.url));
        }

        void webClient_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            /*try
            {
                if (progressMedia.Value <= progressMedia.Maximum)
                {
                    progressMedia.Value = (double)e.ProgressPercentage;
                }

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }*/
        }

        protected bool IncreaseIsolatedStorageSpace(long quotaSizeDemand)
        {
            bool CanSizeIncrease = false;
            IsolatedStorageFile isolatedStorageFile = IsolatedStorageFile.GetUserStoreForApplication();
            //Get the Available space
            long maxAvailableSpace = isolatedStorageFile.AvailableFreeSpace;
            if (quotaSizeDemand > maxAvailableSpace)
            {
                if (!isolatedStorageFile.IncreaseQuotaTo(isolatedStorageFile.Quota + quotaSizeDemand))
                {
                    CanSizeIncrease = false;
                    return CanSizeIncrease;
                }
                CanSizeIncrease = true;
                return CanSizeIncrease;
            }
            return CanSizeIncrease;
        }

        void webClient_OpenReadCompleted(object sender, OpenReadCompletedEventArgs e)
        {
            try
            {
                if (e.Result != null)
                {

                    var isolatedStorageFile = IsolatedStorageFile.GetUserStoreForApplication();
                    bool checkQuotaIncrease = IncreaseIsolatedStorageSpace(e.Result.Length);
                    //string File = string.Format("%s - %s.mp3", current.artist, current.title);
                    string File = string.Format("{0}.mp3", current.aid.ToString());
                    var isolatedStorageFileStream = new IsolatedStorageFileStream(File, FileMode.Create, isolatedStorageFile);
                    long VideoFileLength = (long)e.Result.Length;
                    byte[] byteImage = new byte[VideoFileLength];
                    e.Result.Read(byteImage, 0, byteImage.Length);
                    isolatedStorageFileStream.Write(byteImage, 0, byteImage.Length);
                    isolatedStorageFileStream.Close();
                    current.NotifyPropertyChanged("onDisk");
                }
                if (_curr < job.Count-1)
                {
                    ++_curr;
                    if (progress!=null)
                        progress(_curr, job.Count);
                    next();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
 

        public event Action<int, int> progress;
    }
}
