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
using Microsoft.Phone.Tasks;
using System.Windows.Navigation;
using Microsoft.Phone.Marketplace;


namespace MyPicasa
{
    public partial class HelpPage : PhoneApplicationPage
    {
        SavePhoneNumberTask savePhoneNumberTask = new SavePhoneNumberTask();
        SaveEmailAddressTask saveEmailAddressTask = new SaveEmailAddressTask();
        

        // Constructor
        public HelpPage()
        {
            InitializeComponent();
            
            saveEmailAddressTask.Completed += saveEmailTask_Completed;

            ((App)Application.Current).RootFrame.Obscured += RootFrame_Obscured;
            ((App)Application.Current).RootFrame.Unobscured += RootFrame_Unobscured;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            
        }

        void RootFrame_Unobscured(object sender, System.EventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("Unobscured...");
        }

        void RootFrame_Obscured(object sender, ObscuredEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("Obscured...");
        }

       

        private void SupportEmailLink_Click(object sender, RoutedEventArgs e)
        {
            EmailComposeTask task = new EmailComposeTask()
            {
                To = (string)supportEmailLink.Content.ToString(),
                Subject = "MobiPicasa Application",
                Body = "Support Issue Details:"
            };
            task.Show();
        }

        private void ShareSms_Click(object sender, RoutedEventArgs e)
        {
            SmsComposeTask task = new SmsComposeTask()
            {
                Body = "I like the MobiPicasa Application, you should try it out!",
            };
            task.Show();
        }

        
        private void HomePage_Click(object sender, RoutedEventArgs e)
        {
            MarketplaceDetailTask task = new MarketplaceDetailTask();
            task.Show();
        }

        private void Search_Click(object sender, RoutedEventArgs e)
        {
            MarketplaceSearchTask task = new MarketplaceSearchTask()
            {
                SearchTerms = "MobiPicasa",
                ContentType = MarketplaceContentType.Applications
            };
            task.Show();
        }

        private void BingSearch_Click(object sender, RoutedEventArgs e)
        {
            SearchTask task = new SearchTask()
            {
                SearchQuery = "MobiPicasa bing.com"
            };
            task.Show();
        }

       

        private void SaveEmail_Click(object sender, RoutedEventArgs e)
        {
            saveEmailAddressTask.Email = (string)supportEmailLink.Content;
            saveEmailAddressTask.Show();
        }

        private void saveEmailTask_Completed(object sender, TaskEventArgs e)
        {
            if (e.TaskResult == TaskResult.OK)
            {
                saveEmailButton.Visibility = Visibility.Collapsed;
            }
        }


          }
}
