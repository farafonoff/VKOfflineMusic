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
using System.Windows.Threading;
using com.google.zxing.qrcode;
using Microsoft.Devices;
using System.Windows.Navigation;
using com.google.zxing.common;
using com.google.zxing;
using System.Windows.Controls.Primitives;

namespace VKOfflineAudio
{
    public partial class Scanner : PhoneApplicationPage
    {
        Popup confirm;
        public Scanner()
        {
            InitializeComponent();

            Button bigbutton = new Button();
            bigbutton.Content = AppResources.confirmQR;
            bigbutton.Click += new RoutedEventHandler(bigbutton_Click);
            confirm = new Popup() { Child = bigbutton, IsOpen = false,HorizontalOffset=20,VerticalOffset=(int)(this.Height/2-50)};
            bigbutton.Height = 100;
            bigbutton.Width = this.Width - 40;
            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromMilliseconds(250);
            _timer.Tick += (o, arg) => ScanPreviewBuffer();
        }

        string currentDevId;

        void bigbutton_Click(object sender, RoutedEventArgs e)
        {
            if (currentDevId != null)
            {
                Settings.instance.devid = currentDevId;
                Settings.instance.save();
                NavigationService.GoBack();
            }
        }

        private readonly DispatcherTimer _timer;
        //private readonly ObservableCollection<string> _matches;

        private PhotoCameraLuminanceSource _luminance;
        private QRCodeReader _reader;
        private PhotoCamera _photoCamera;
        private bool initalized = false;

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            _photoCamera = new PhotoCamera();
            _photoCamera.Initialized += OnPhotoCameraInitialized;            
            _previewVideo.SetSource(_photoCamera);

            CameraButtons.ShutterKeyHalfPressed += (o, arg) => _photoCamera.Focus();
            if (initalized)
            {
                _timer.Start();
            }

            base.OnNavigatedTo(e);
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            _timer.Stop();
            base.OnNavigatedFrom(e);
        }

        private void OnPhotoCameraInitialized(object sender, CameraOperationCompletedEventArgs e)
        {
            int width = Convert.ToInt32(_photoCamera.PreviewResolution.Width);
            int height = Convert.ToInt32(_photoCamera.PreviewResolution.Height);
            initalized = true;
            
            _luminance = new PhotoCameraLuminanceSource(width, height);
            _reader = new QRCodeReader();

            Dispatcher.BeginInvoke(() => {
                _previewTransform.Rotation = _photoCamera.Orientation;
                _timer.Start();
            });
        }
 
        private void ScanPreviewBuffer()
        {
            try
            {
                _photoCamera.GetPreviewBufferY(_luminance.PreviewBufferY);
                var binarizer = new HybridBinarizer(_luminance);
                var binBitmap = new BinaryBitmap(binarizer);
                var result = _reader.decode(binBitmap);
                Dispatcher.BeginInvoke(() => DisplayResult(result.Text));
            }
            catch
            {
                Dispatcher.BeginInvoke(() => { confirm.IsOpen = false; });
            }            
        }

        private void DisplayResult(string text)
        {
            currentDevId = text;
            confirm.IsOpen = true;
        }
    }
}