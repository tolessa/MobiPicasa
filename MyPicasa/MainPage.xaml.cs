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
using Microsoft.Phone.Shell;
using System.ComponentModel;
using System.Threading;
using System.Windows.Controls.Primitives;

namespace MyPicasa
{
    public partial class MainPage : PhoneApplicationPage
    {  
        //declare all variables needed to deal with the authentication
        
        private BackgroundWorker backroungWorker;
        private const string emailKey = "emailKey";
        private const string passwordKey = "passwordKey";
        private const string savepasswordKey = "savepasswordKey";
        private App app = App.Current as App;
        private string email = "";
        private string password = "";
        private bool savepassword = false;
        private Popup popup;

        public MainPage()
        {
            InitializeComponent();
            ShowPopup();

            app.appSettings = IsolatedStorageSettings.ApplicationSettings;

            //Avoid to come back in the app-> a user has to use logout button to exit
            BackKeyPress += OnBackKeyPressed;

        }
        private void ShowPopup()
        {
            this.popup = new Popup();
            this.popup.Child = new PopupSplash();
            this.popup.IsOpen = true;
            StartLoadingData();
        }
        private void StartLoadingData()
        {
            backroungWorker = new BackgroundWorker();
            backroungWorker.DoWork += new DoWorkEventHandler(backroungWorker_DoWork);
            backroungWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(backroungWorker_RunWorkerCompleted);
            backroungWorker.RunWorkerAsync();
        }
        void backroungWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            this.Dispatcher.BeginInvoke(() =>
            {
                this.popup.IsOpen = false;

            }
            );
        }

        void backroungWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            // Do some data loading on a background
            // We'll just sleep for a moment for the demo
            Thread.Sleep(7000);
        }
        void OnBackKeyPressed(object sender, CancelEventArgs e)
        {

            e.Cancel = true;
        }


        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {


            // load and show saved email from isolated storage if any
            if (app.appSettings.Contains(emailKey))
            {

                email = (string)app.appSettings[emailKey];
            }
            EmailTextBox.Text = email;

            // load password from isolated storage if any
            if (app.appSettings.Contains(passwordKey))
            {
                password = (string)app.appSettings[passwordKey];
            }



            if (app.appSettings.Contains(savepasswordKey))
            {
                string savepass = (string)app.appSettings[savepasswordKey];
                if (savepass == "true")
                {
                    savepassword = true;
                    PasswordTextBox.Password = password;
                }
                else
                {
                    savepassword = false;
                    PasswordTextBox.Password = "";
                }
                SavePasswordCheckBox.IsChecked = savepassword;
            }
        }

        protected override void OnNavigatedFrom(System.Windows.Navigation.NavigationEventArgs e)
        {
            // add email to isolated storage
            app.appSettings.Remove(emailKey);
            app.appSettings.Add(emailKey, email);

            // add passwordkey and password to isolated storage
            app.appSettings.Remove(savepasswordKey);
            app.appSettings.Remove(passwordKey);

            if (SavePasswordCheckBox.IsChecked == true)
            {
                app.appSettings.Add(savepasswordKey, "true");
                app.appSettings.Add(passwordKey, password);
            }
            else
            {
                app.appSettings.Add(savepasswordKey, "false");
                app.appSettings.Add(passwordKey, password);
            }
        }
        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {

            email = EmailTextBox.Text;
            password = PasswordTextBox.Password;
            savepassword = (bool)SavePasswordCheckBox.IsChecked;
            GetAuth();

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
                        this.NavigationService.Navigate(new Uri("/AlbumsPage.xaml", UriKind.Relative));

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


    }
}