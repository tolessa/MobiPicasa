

//This page deals with all about a single album out of picasa albums
//It enables Us to view every images thumbnail in ordered list
//We can logout directly from this page and could also upload other pictures to the Album
using System;
using System.Collections.Generic;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Phone.Controls;
using System.Collections;
using Microsoft.Phone.Tasks;
using System.IO;
using System.Text;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Windows.Media.Imaging;
using Microsoft.Phone.Shell;
using System.ComponentModel;
using Microsoft.Xna.Framework.Media;

namespace MyPicasa
{
    public partial class AlbumPage : PhoneApplicationPage
    {
        byte[] photo;
        public IDictionary<string, string> parameters;
        public string album, user;
        PhotoChooserTask photoChooserTask = new PhotoChooserTask();
        App app = App.Current as App;
        public string auth;
        BackgroundWorker worker = new BackgroundWorker();
        IAsyncResult asynchronous;
        int selectedIndex;


        // Handle loading animation in this page
        public bool ShowProgress
        {
            get { return (bool)GetValue(ShowProgressProperty); }
            set { SetValue(ShowProgressProperty, value); }
        }

        public static readonly DependencyProperty ShowProgressProperty =
            DependencyProperty.Register("ShowProgress", typeof(bool), typeof(AlbumPage), new PropertyMetadata(false));


        public AlbumPage()
        {
            InitializeComponent();
            //Initialize Application Bar
            ApplicationBar = new ApplicationBar();

            ApplicationBar.Opacity = 1.0;
            ApplicationBar.IsVisible = true;
            ApplicationBar.IsMenuEnabled = true;

            

            //Camera Button
            ApplicationBarIconButton btnCamera = new ApplicationBarIconButton();
            btnCamera.IconUri = new Uri("Icons/appbar.feature.camera.rest.png", UriKind.Relative);
            btnCamera.Text = AppResources.camerabtn;
            btnCamera.Click += new EventHandler(this.app.Camera_Click);
            ApplicationBar.Buttons.Add(btnCamera);

            //Initialize button upload
            ApplicationBarIconButton btnUpload = new ApplicationBarIconButton();
            btnUpload.Click += new EventHandler(Upload_Click);
            btnUpload.IconUri = new Uri("Icons/appbar.upload.rest.png", UriKind.Relative);
            btnUpload.Text = AppResources.uploadbtn;
            ApplicationBar.Buttons.Add(btnUpload);

            

            // Play slideshow button 
            ApplicationBarIconButton btnPlay = new ApplicationBarIconButton();
            btnPlay.Click += new EventHandler(Play_Click);
            btnPlay.IconUri = new Uri("Icons/play.png", UriKind.Relative);
            btnPlay.Text = AppResources.playbtn;
            ApplicationBar.Buttons.Add(btnPlay);

            //Initialize button logout
            /* ApplicationBarIconButton btnLogout = new ApplicationBarIconButton();
             btnLogout.Click += new EventHandler(this.app.Logout_Click);
             btnLogout.IconUri = new Uri("/Icons/cancel.png", UriKind.Relative);
             btnLogout.Text = AppResources.logoutbtn;
             ApplicationBar.Buttons.Add(btnLogout); */
            //Initialize the photoChooserTask to pick up picture to upload

            ApplicationBarIconButton btnDownload = new ApplicationBarIconButton();
            btnDownload.Click += new EventHandler(Download_Click);
            btnDownload.IconUri = new Uri("Icons/appbar.download.rest.png", UriKind.Relative);
            btnDownload.Text = AppResources.downloadbtn;
            ApplicationBar.Buttons.Add(btnDownload);

            photoChooserTask.Completed += new EventHandler<PhotoResult>(photoChooserTask_Completed);

            //Initialize Background worker to reload picture list automatically after one upload
            worker.DoWork += new DoWorkEventHandler(worker_DoWork);
            worker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(worker_RunWorkerCompleted);




        }

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            // We are coming back from Images Page
            if (e.NavigationMode == System.Windows.Navigation.NavigationMode.Back)
            {
                AlbumImagesListBox.ItemsSource = app.albumImages;
                AlbumName.Text = app.albums[app.selectedAlbumIndex].title;

                AlbumImagesListBox.SelectedIndex = -1;
                return;
            }

            // We are coming from MainPage, start loading album images
            parameters = this.NavigationContext.QueryString;
            if (parameters.ContainsKey("SelectedIndex"))
            {
                selectedIndex = Int32.Parse(parameters["SelectedIndex"]);

                AlbumName.Text = app.albums[selectedIndex].title;

                GetImages(selectedIndex);
            }
            if (app.albums[app.selectedAlbumIndex].location != "")
            {

                AlbumLocation.Text = AppResources.location_str + app.albums[app.selectedAlbumIndex].location;
            }

        }

        public void GetImages(int selectedIndex)
        {
            // Show loading... animation
            ShowProgress = true;

            WebClient webClient = new WebClient();
            auth = "GoogleLogin auth=" + app.auth;
            webClient.Headers[HttpRequestHeader.Authorization] = auth;
            Uri uri = new Uri(app.albums[selectedIndex].path, UriKind.Absolute);
            webClient.DownloadStringCompleted += new DownloadStringCompletedEventHandler(ImagesDownloaded);
            webClient.DownloadStringAsync(uri);

        }

        public void ImagesDownloaded(object sender, DownloadStringCompletedEventArgs e)
        {
            try
            {
                if (e.Result == null || e.Error != null)
                {
                    MessageBox.Show(AppResources.error_load);
                    return;
                }
                else
                {
                    // Deserialize JSON string to dynamic object
                    IDictionary<string, object> json = (IDictionary<string, object>)SimpleJson.DeserializeObject(e.Result);
                    // Feed object
                    IDictionary<string, object> feed = (IDictionary<string, object>)json["feed"];
                    // Number of photos object
                    IDictionary<string, object> numberOfPhotos = (IDictionary<string, object>)feed["gphoto$numphotos"];



                    // Entries List
                    var entries = (IList)feed["entry"];

                    //Store the current album ID for Upload
                    IDictionary<string, object> albumid = (IDictionary<string, object>)feed["gphoto$id"];
                    album = albumid["$t"].ToString();

                    // clear previous images from albumImages
                    app.albumImages.Clear();
                    // Find image details from entries
                    for (int i = 0; i < entries.Count; i++)
                    {
                        // Create a new albumImage
                        AlbumImage albumImage = new AlbumImage();
                        // Image entry object
                        IDictionary<string, object> entry = (IDictionary<string, object>)entries[i];
                        // Image title object
                        IDictionary<string, object> title = (IDictionary<string, object>)entry["title"];
                        // Get album title
                        albumImage.title = (string)title["$t"];
                        // Album content object
                        IDictionary<string, object> content = (IDictionary<string, object>)entry["content"];
                        // Get image src url
                        albumImage.content = (string)content["src"];
                        // Image width object
                        IDictionary<string, object> width = (IDictionary<string, object>)entry["gphoto$width"];
                        // Get image width
                        albumImage.width = (string)width["$t"];
                        // Image height object
                        IDictionary<string, object> height = (IDictionary<string, object>)entry["gphoto$height"];
                        // Get image height
                        albumImage.height = (string)height["$t"];
                        // Image size object
                        IDictionary<string, object> size = (IDictionary<string, object>)entry["gphoto$size"];
                        // Get image size 
                        albumImage.size = (string)size["$t"];
                        // Image media group List
                        IDictionary<string, object> mediaGroup = (IDictionary<string, object>)entry["media$group"];
                        IList mediaThumbnailList = (IList)mediaGroup["media$thumbnail"];
                        // First thumbnail object
                        IDictionary<string, object> mediathumbnail = (IDictionary<string, object>)mediaThumbnailList[0];
                        // Get thumbnail url
                        albumImage.thumbnail = (string)mediathumbnail["url"];



                        // Add albumImage to albumImages Collection
                        app.albumImages.Add(albumImage);
                    }
                    // Hide loading... animation
                    ShowProgress = false;
                    // Add albumImages to AlbumImagesListBox
                    AlbumImagesListBox.ItemsSource = app.albumImages;
                }
            }
            catch (WebException)
            {
                MessageBox.Show(AppResources.error_load);
            }
            catch (KeyNotFoundException)
            {
                MessageBox.Show(AppResources.error_load);
            }
        }

        private void AlbumImagesListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // If real selection is happened, go to a ImagesPage
            if (AlbumImagesListBox.SelectedIndex == -1) return;
            this.NavigationService.Navigate(new Uri("/ImagesPage.xaml?SelectedIndex=" + AlbumImagesListBox.SelectedIndex + "&slideshow=false&SelectedAlbum=" + AlbumName.Text, UriKind.Relative));
        }

        public byte[] ConvertToBytes(BitmapImage bitmapImage)
        {
            byte[] data = null;
            using (MemoryStream stream = new MemoryStream())
            {
                WriteableBitmap wBitmap = new WriteableBitmap(bitmapImage);
                wBitmap.SaveJpeg(stream, wBitmap.PixelWidth, wBitmap.PixelHeight, 0, 100);
                stream.Seek(0, SeekOrigin.Begin);
                data = stream.GetBuffer();
            }

            return data;
        }

        private void photoChooserTask_Completed(object sender, PhotoResult e)
        {

            //create a new Bitmap image
            BitmapImage image = new BitmapImage();
            // Store the chosen picture in this Bitmap image
            image.SetSource(e.ChosenPhoto);
            //Convert this bitmap in a byte array
            photo = ConvertToBytes(image);

            // put in the var 'user', the user id we stored in the Isolated Storage settings 'appsettings'
            //in the AlbumsPages.xaml.cs
            user = app.appSettings["userid_sett"].ToString();

            // prepare the url with the userID and album id to pass to the request
            var url = string.Format("http://picasaweb.google.com/data/feed/api/user/{0}/albumid/{1}", user, album);

            //Definition of the request 
            var request = WebRequest.Create(new Uri(url)) as HttpWebRequest;
            request.Method = "POST";
            request.ContentType = "image/jpeg";
            request.Headers[HttpRequestHeader.Authorization] = auth;
            request.Headers["Slug"] = "test";


            request.BeginGetRequestStream(new AsyncCallback(GetRequestStreamCallback), request);


        }

        public void Upload_Click(object sender, EventArgs e)
        {
            //Display the camera button in the PhotoChooser Task
            photoChooserTask.ShowCamera = true;
            //Display the PhotoChooser Task
            photoChooserTask.Show();
        }

        private void GetRequestStreamCallback(IAsyncResult asynchronousResult)
        {
            HttpWebRequest request = (HttpWebRequest)asynchronousResult.AsyncState;

            // End the operation
            Stream postStream = request.EndGetRequestStream(asynchronousResult);

            // Write to the request stream.
            postStream.Write(photo, 0, photo.Length);
            postStream.Close();

            // Start the asynchronous operation to get the response
            request.BeginGetResponse(new AsyncCallback(GetResponseCallback), request);
        }

        private void GetResponseCallback(IAsyncResult asynchronousResult)
        {

            asynchronous = asynchronousResult;
            worker.RunWorkerAsync();
        }

        void worker_DoWork(object sender, DoWorkEventArgs e)
        {
            HttpWebRequest request = (HttpWebRequest)asynchronous.AsyncState;


            // End the operation
            HttpWebResponse response = (HttpWebResponse)request.EndGetResponse(asynchronous);
            Stream streamResponse = response.GetResponseStream();
            StreamReader streamRead = new StreamReader(streamResponse);

            string responseString = streamRead.ReadToEnd();

            // Close the stream object
            streamResponse.Close();
            streamRead.Close();

            // Release the HttpWebResponse
            response.Close();

        }

        void worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                MessageBox.Show(e.Error.Message);
            }
            else if (e.Cancelled)
            {
                // Next, handle the case where the user canceled  
                // the operation. 
                // Note that due to a race condition in  
                // the DoWork event handler, the Cancelled 
                // flag may not have been set, even though 
                // CancelAsync was called.
                MessageBox.Show(AppResources.cancel);
            }
            else
            {
                // Finally, handle the case where the operation  
                // succeeded.

                this.Dispatcher.BeginInvoke(delegate()
                {
                    GetImages(selectedIndex);
                });


            }


        }

        void Play_Click(object sender, EventArgs e)
        {
            this.NavigationService.Navigate(new Uri("/ImagesPage.xaml?SelectedIndex=0&SelectedAlbum=" + AlbumName.Text + "&slideshow=true", UriKind.Relative));

        }

        void Download_Click(object sender, EventArgs e)
        {
            for (int i = 0; i < app.albumImages.Count; i++)
            {
                WebClient wc = new WebClient();
                wc.OpenReadCompleted += new OpenReadCompletedEventHandler(wc_OpenReadCompleted);
                wc.OpenReadAsync(new Uri(app.albumImages[i].content), wc);
            }
            MessageBox.Show(AppResources.albdwn_str);
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

        }
    }
}
        