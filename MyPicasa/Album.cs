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

namespace MyPicasa
{
    public class Album
    {
        public string title { get; set; }
        public string published { get; set; }
        public string href { get; set; }
        public string thumbnail { get; set; }
        public DateTime dt { get; set; }
        public string location { get; set; }
        public string path { get; set; }
        public string id { get; set; }
        public string nbpic { get; set; }

        public void changedate()
        {

            DateTime dt = Convert.ToDateTime(published);
            published = dt.ToString(AppResources.date_str);

        }

    }
}
