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
using System.Windows.Media.Imaging;
using System.IO.IsolatedStorage;
using System.IO;
using Microsoft.Xna.Framework.Media;
using Microsoft.Phone.Tasks;
using Microsoft.Phone.Shell;
using System.Threading;
using System.ComponentModel;
using System.Windows.Threading;

namespace MyPicasa
{
    public partial class ImagesPage : PhoneApplicationPage
    {

        App app = App.Current as App;
        GestureListener gestureListener;
        BitmapImage bitmapImage;
        //Timer Slideshow
        public DispatcherTimer timer = new DispatcherTimer();

        int img;
        string selectedAlbumTitle;



        // Handle loading animation in this page
        public bool ShowProgress
        {
            get { return (bool)GetValue(ShowProgressProperty); }
            set { SetValue(ShowProgressProperty, value); }
        }

        public static readonly DependencyProperty ShowProgressProperty =
            DependencyProperty.Register("ShowProgress", typeof(bool), typeof(ImagesPage), new PropertyMetadata(false));

        ApplicationBarIconButton btnPlay = new ApplicationBarIconButton();
        ApplicationBarIconButton btnDownload = new ApplicationBarIconButton();

        public ImagesPage()
        {
            InitializeComponent();

            //Timer slideshow

            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(2);
            timer.Tick += new EventHandler(timer_Tick);
            // Initialize GestureListener
            gestureListener = GestureService.GetGestureListener(ContentPanel);
            // Handle Dragging (to show next or previous image from Album)
            gestureListener.DragCompleted += new EventHandler<DragCompletedGestureEventArgs>(gestureListener_DragCompleted);

            //Initialize application bar
            ApplicationBar = new ApplicationBar();

            ApplicationBar.Opacity = 1.0;
            ApplicationBar.IsVisible = true;
            ApplicationBar.IsMenuEnabled = true;


            //Download Button creation

            btnDownload.Click += new EventHandler(Download_Click);
            btnDownload.IconUri = new Uri("Icons/appbar.download.rest.png", UriKind.Relative);
            btnDownload.Text = AppResources.downloadbtn;
            ApplicationBar.Buttons.Add(btnDownload);

            //Play/stop slideshow button
            btnPlay.Click += new EventHandler(Play_Click);
            btnPlay.IconUri = new Uri("Icons/play.png", UriKind.Relative);
            btnPlay.Text = AppResources.playbtn;
            ApplicationBar.Buttons.Add(btnPlay);


        }

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            string slideshow = NavigationContext.QueryString["slideshow"];
            if (slideshow == "true")
            {

                play();
            }
            base.OnNavigatedTo(e);

            // Find selected image index from parameters
            IDictionary<string, string> parameters = this.NavigationContext.QueryString;
            if (parameters.ContainsKey("SelectedIndex"))
            {
                app.selectedImageIndex = Int32.Parse(parameters["SelectedIndex"]);
            }
            else
            {
                app.selectedImageIndex = 0;
            }
            // Find selected album name
            if (parameters.ContainsKey("SelectedAlbum"))
            {
                selectedAlbumTitle = parameters["SelectedAlbum"];
            }
            else
            {
                selectedAlbumTitle = AppResources.noalbum;
            }

            // Load image from Google
            LoadImage();
        }



        // Gesture - Drag is complete
        void gestureListener_DragCompleted(object sender, DragCompletedGestureEventArgs e)
        {
            if (transform.ScaleX == 1 && transform.ScaleY == 1)
            {
                // Left or Right
                if (e.HorizontalChange > 0)
                {
                    // previous image (or last if first is shown)
                    app.selectedImageIndex--;
                    if (app.selectedImageIndex < 0) app.selectedImageIndex = app.albumImages.Count - 1;
                }
                else
                {
                    // next image (or first if last is shown)
                    app.selectedImageIndex++;
                    if (app.selectedImageIndex > (app.albumImages.Count - 1)) app.selectedImageIndex = 0;
                }
                // Load image from Google
                LoadImage();
            }
        }


        void timer_Tick(object sender, EventArgs e)
        {
            app.selectedImageIndex++;
            if (app.selectedImageIndex > (app.albumImages.Count - 1)) app.selectedImageIndex = 0;
            LoadImage();
        }
        void Stop_Click(object sender, EventArgs e)
        {

            stop();
        }
        void Play_Click(object sender, EventArgs e)
        {
            play();
        }

        void play()
        {
            timer.Start();
            btnPlay.Click += new EventHandler(Stop_Click);
            btnPlay.IconUri = new Uri("Icons/pause.png", UriKind.Relative);
            btnPlay.Text = AppResources.stopbtn;
            ApplicationBar.Mode = ApplicationBarMode.Default;
            btnDownload.IsEnabled = false;
        }
        public void stop()
        {
            timer.Stop();
            btnPlay.Click += new EventHandler(Play_Click);
            btnPlay.IconUri = new Uri("Icons/play.png", UriKind.Relative);
            btnPlay.Text = AppResources.playbtn;
            btnDownload.IsEnabled = true;

        }

        // Load Image from Google
        private void LoadImage()
        {// Show loading... animation
            ShowProgress = true;
            // Load a new image
            bitmapImage = new BitmapImage(new Uri(app.albumImages[app.selectedImageIndex].content, UriKind.RelativeOrAbsolute));
            // Handle loading (hide Loading... animation)
            bitmapImage.DownloadProgress += new EventHandler<DownloadProgressEventArgs>(bitmapImage_DownloadProgress);
            // Loaded Image is image source in XAML
            image.Source = bitmapImage;
        }

        // Image is loaded from Google
        void bitmapImage_DownloadProgress(object sender, DownloadProgressEventArgs e)
        {

            // Disable LoadingListener for this image
            bitmapImage.DownloadProgress -= new EventHandler<DownloadProgressEventArgs>(bitmapImage_DownloadProgress);
            // Show image details in UI

            img = app.selectedImageIndex;

            ImageInfoTextBlock.Text = String.Format(AppResources.currentimg,
                selectedAlbumTitle,
                (app.selectedImageIndex + 1),
                app.albumImages.Count);
            // Hide loading... animation
            ShowProgress = false;
        }

        //Download button click method to call the download function
        void Download_Click(object sender, EventArgs e)
        {
            WebClient wc = new WebClient();
            wc.OpenReadCompleted += new OpenReadCompletedEventHandler(wc_OpenReadCompleted);
            wc.OpenReadAsync(new Uri(app.albumImages[img].content), wc);

        }

        public void wc_OpenReadCompleted(object sender, OpenReadCompletedEventArgs e)
        {
            String tempJPEG = "PicasaReaderTempJPEG";

            var myStore = IsolatedStorageFile.GetUserStoreForApplication();
            if (myStore.FileExists(tempJPEG))
                myStore.DeleteFile(tempJPEG);

            IsolatedStorageFileStream myFileStream = myStore.CreateFile(tempJPEG);

            BitmapImage bitmap = new BitmapImage();
            bitmap.SetSource(e.Result);
            WriteableBitmap wb = new WriteableBitmap(bitmap);
            Extensions.SaveJpeg(wb, myFileStream, wb.PixelWidth, wb.PixelHeight, 0, 85);
            myFileStream.Close();

            myFileStream = myStore.OpenFile(tempJPEG, FileMode.Open, FileAccess.Read);

            MediaLibrary library = new MediaLibrary();
            Picture pic = library.SavePicture(DateTime.Now.ToFileTime() + ".jpg", myFileStream);
            myFileStream.Close();
            MessageBox.Show(AppResources.picdwn_str);
        }




        private void Image_DragDelta(object sender, DragDeltaGestureEventArgs e)
        {
            // if is not touch enabled or the scale is different than 1 then don’t allow moving
            if (transform.ScaleX <= 1.1)
                return;
            double centerX = transform.CenterX;
            double centerY = transform.CenterY;
            double translateX = transform.TranslateX;
            double translateY = transform.TranslateY;
            double scale = transform.ScaleX;
            double width = image.ActualWidth;
            double height = image.ActualHeight;

            // verify limits to not allow the image to get out of area

            if (centerX - scale * centerX + translateX + e.HorizontalChange < 0 &&
            centerX + scale * (width - centerX) + translateX + e.HorizontalChange > width)
            {
                transform.TranslateX += e.HorizontalChange;
            }

            if (centerY - scale * centerY + translateY + e.VerticalChange < 0 &&
            centerY + scale * (height - centerY) + translateY + e.VerticalChange > height)
            {
                transform.TranslateY += e.VerticalChange;
            }

            return;
        }

        private void image_DoubleTap(object sender, Microsoft.Phone.Controls.GestureEventArgs e)
        {
            if (transform.ScaleX == 1 && transform.ScaleY == 1 && timer.IsEnabled == false)
            {
                //zoom in
                transform.ScaleX = 1.5;
                transform.ScaleY = 1.5;
            }
            else
            {
                //put the image at its initial position
                transform.TranslateX = 0;
                transform.TranslateY = 0;
                //zoom out
                transform.ScaleX = 1;
                transform.ScaleY = 1;
            }
        }

    }

}