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
using System.IO.IsolatedStorage;
using System.Collections;
using Microsoft.Phone.Shell;
using Microsoft.Phone.Tasks;
using System.Windows.Media.Imaging;
using System.IO;
using System.ComponentModel;

namespace MyPicasa
{
    public partial class AlbumsPage : PhoneApplicationPage
    {

        //private IsolatedStorageSettings appSettings;
        private const string emailKey = "emailKey";
        private const string passwordKey = "passwordKey";

        private string username = "";
        private string email = "";
        private string password = "";
        private string dataFeed = "";
        private App app = App.Current as App;
        public PhotoChooserTask photoChooserTask;
        public string userid_sett;

        // Handle loading animation in this page
        public bool ShowProgress
        {
            get { return (bool)GetValue(ShowProgressProperty); }
            set { SetValue(ShowProgressProperty, value); }
        }

        public static readonly DependencyProperty ShowProgressProperty =
            DependencyProperty.Register("ShowProgress", typeof(bool), typeof(AlbumsPage), new PropertyMetadata(false));


        public AlbumsPage()
        {
            InitializeComponent();

            //  appSettings = IsolatedStorageSettings.ApplicationSettings;

            //Avoid to come back in the login Page-> user have to use logout button
            BackKeyPress += OnBackKeyPressed;

            //Application Bar initialization
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

            //Help Button
            ApplicationBarIconButton btnHelp = new ApplicationBarIconButton();
            btnHelp.Click += new EventHandler(this.app.Help_Click);
            btnHelp.IconUri = new Uri("/Icons/appbar.questionmark.rest.png", UriKind.Relative);
            btnHelp.Text = "Help";
            ApplicationBar.Buttons.Add(btnHelp);

            //Logout Button
            ApplicationBarIconButton btnLogout = new ApplicationBarIconButton();
            btnLogout.Click += new EventHandler(this.app.Logout_Click);
            btnLogout.IconUri = new Uri("/Icons/appbar.edit.rest.png", UriKind.Relative);
            btnLogout.Text = AppResources.logoutbtn;
            ApplicationBar.Buttons.Add(btnLogout);



        }

        void OnBackKeyPressed(object sender, CancelEventArgs e)
        {

            e.Cancel = true;
        }


        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);


            // load saved email from isolated storage
            if (app.appSettings.Contains(emailKey))
            {
                email = (string)app.appSettings[emailKey]; // for example firstname.lastname@gmail.com
                if (username == "" && email.IndexOf("@") != -1)
                {
                    username = email.Substring(0, email.IndexOf("@")); // firstname.lastname
                }
                else if (email.IndexOf("@") == -1) { username = email; }

                dataFeed = String.Format("http://picasaweb.google.com/data/feed/api/user/{0}?alt=json", username);
            }

            // load password from isolated storage
            if (app.appSettings.Contains(passwordKey))
            {
                password = (string)app.appSettings[passwordKey];
            }

            // we are coming back from AlbumPage
            if (e.NavigationMode == System.Windows.Navigation.NavigationMode.Back)
            {
                AlbumsListBox.ItemsSource = app.albums;
                AlbumsListBox.SelectedIndex = -1;
            }
            else
            {
                // get authentication from Google
                GetAuth();
            }
        }

        private void GetAuth()
        {
            string service = "lh2"; // Picasa
            string accountType = "GOOGLE";

            WebClient webClient = new WebClient();
            Uri uri = new Uri(string.Format("https://www.google.com/accounts/ClientLogin?Email={0}&Passwd={1}&service={2}&accountType={3}", email, password, service, accountType));
            webClient.DownloadStringCompleted += new DownloadStringCompletedEventHandler(AuthDownloaded);
            webClient.DownloadStringAsync(uri);
        }

        private void AuthDownloaded(object sender, DownloadStringCompletedEventArgs e)
        {
            try
            {
                if (e.Result != null && e.Error == null)
                {
                    app.auth = "";
                    int index = e.Result.IndexOf("Auth=");
                    if (index != -1)
                    {
                        app.auth = e.Result.Substring(index + 5);
                    }
                    if (app.auth != "")
                    {
                        // get albums from Google
                        GetAlbums();
                        return;
                    }
                }
                MessageBox.Show(AppResources.error_auth);
            }
            catch (WebException)
            {
                MessageBox.Show(AppResources.error_auth);
            }
        }

        private void GetAlbums()
        {
            // Show loading... animation
            ShowProgress = true;
            WebClient webClient = new WebClient();
            webClient.Headers[HttpRequestHeader.Authorization] = "GoogleLogin auth=" + app.auth;
            Uri uri = new Uri(dataFeed, UriKind.Absolute);
            webClient.DownloadStringCompleted += new DownloadStringCompletedEventHandler(AlbumsDownloaded);
            webClient.DownloadStringAsync(uri);
        }
        public void AlbumsDownloaded(object sender, DownloadStringCompletedEventArgs e)
        {
            try
            {
                if (e.Result == null || e.Error != null)
                {
                    MessageBox.Show(AppResources.error_albums);
                    return;
                }
                else
                {

                    // Deserialize JSON string to dynamic object
                    IDictionary<string, object> json = (IDictionary<string, object>)SimpleJson.DeserializeObject(e.Result);
                    // Feed object
                    IDictionary<string, object> feed = (IDictionary<string, object>)json["feed"];
                    // Author List
                    IList author = (IList)feed["author"];
                    // First author (and only)
                    IDictionary<string, object> firstAuthor = (IDictionary<string, object>)author[0];

                    //Stock UserID in appsettings
                    IDictionary<string, object> userid = (IDictionary<string, object>)feed["gphoto$user"];


                    app.appSettings.Remove("userid_sett");
                    app.appSettings.Add("userid_sett", (string)userid["$t"]);
                    // Album entries
                    IList entries = (IList)feed["entry"];

                    // Delete previous albums from albums list
                    app.albums.Clear();
                    // Find album details
                    for (int i = 0; i < entries.Count; i++)
                    {
                        // Create a new Album
                        Album album = new Album();
                        // Album entry object
                        IDictionary<string, object> entry = (IDictionary<string, object>)entries[i];
                        // Published object
                        IDictionary<string, object> published = (IDictionary<string, object>)entry["published"];
                        // Get published date
                        album.published = (string)published["$t"];
                        //convert date
                        album.changedate();
                        //Number of pictures
                        IDictionary<string, object> nbpic = (IDictionary<string, object>)entry["gphoto$numphotos"];
                        album.nbpic = (string)nbpic["$t"].ToString() + AppResources.picture_str;

                        // Title object
                        IDictionary<string, object> title = (IDictionary<string, object>)entry["title"];
                        // Album title
                        album.title = (string)title["$t"];
                        // Link List
                        IList link = (IList)entry["link"];
                        // First link is album data link object
                        IDictionary<string, object> path = (IDictionary<string, object>)link[0];
                        // Get album data addres
                        album.path = (string)path["href"];

                        // Media group object
                        IDictionary<string, object> mediagroup = (IDictionary<string, object>)entry["media$group"];
                        // Media thumbnail object list
                        IList mediathumbnailList = (IList)mediagroup["media$thumbnail"];
                        // First thumbnail object (smallest)
                        var mediathumbnail = (IDictionary<string, object>)mediathumbnailList[0];
                        // Get thumbnail url
                        album.thumbnail = (string)mediathumbnail["url"];
                        //  Location Object
                        IDictionary<string, object> location = (IDictionary<string, object>)entry["gphoto$location"];
                        // Location Name

                        album.location = (string)location["$t"];

                        // Add album to albums
                        app.albums.Add(album);
                    }

                    // Hide loading... animation
                    ShowProgress = false;
                    // Add albums to AlbumListBox
                    AlbumsListBox.ItemsSource = app.albums;
                }
            }
            catch (WebException)
            {
                MessageBox.Show(AppResources.error_albums);
            }
            catch (KeyNotFoundException)
            {
                MessageBox.Show(AppResources.error_load);
            }
        }
        private void AlbumsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // If real selection is happened, go to a AlbumPage
            if (AlbumsListBox.SelectedIndex == -1) return;
            app.selectedAlbumIndex = AlbumsListBox.SelectedIndex;
            this.NavigationService.Navigate(new Uri("/AlbumPage.xaml?SelectedIndex=" + AlbumsListBox.SelectedIndex, UriKind.Relative));
        }



    }
}