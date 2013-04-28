
//This is a page which opens when upload button is clicked from album page
//It Enables to upload images to our album

using System;
using System.IO;
using System.Windows.Media.Imaging;
using ExifLib;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Tasks;
using Microsoft.Xna.Framework.Media;
using System.Net;
using System.Windows;
using System.IO.IsolatedStorage;

namespace MyPicasa
{
    public partial class PhotoProcess : PhoneApplicationPage
    {
        App app = App.Current as App;
        private IsolatedStorageSettings appSettings;
        PhotoResult photo;
        Stream capturedImage;
        int _width;
        int _height;
        ExifLib.ExifOrientation _orientation;
        int _angle;
        private string username = "";
        private string albId;


        // Constructor
        public PhotoProcess()
        {
            InitializeComponent();
            appSettings = IsolatedStorageSettings.ApplicationSettings;
            Loaded += new System.Windows.RoutedEventHandler(MainPage_Loaded);
            OrientationChanged += new EventHandler<OrientationChangedEventArgs>(MainPage_OrientationChanged);
        }

        void MainPage_OrientationChanged(object sender, OrientationChangedEventArgs e)
        {
            //PostedUri.Text = this.Orientation.ToString();
        }

        void MainPage_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            //PostedUri.Text = this.Orientation.ToString();
        }
        //This function will be called as we choose or capture to upload
        void OnCameraCaptureCompleted(object sender, PhotoResult e)
        {
            // figure out the orientation from EXIF data
            //The next functions are dealt with picture's size , in case it will be range out of the the xaml page image area
            e.ChosenPhoto.Position = 0;
            JpegInfo info = ExifReader.ReadJpeg(e.ChosenPhoto, e.OriginalFileName);

            _width = info.Width;
            _height = info.Height;
            _orientation = info.Orientation;

           

            switch (info.Orientation)
            {
                case ExifOrientation.TopLeft:
                case ExifOrientation.Undefined:
                    _angle = 0;
                    break;
                case ExifOrientation.TopRight:
                    _angle = 90;
                    break;
                case ExifOrientation.BottomRight:
                    _angle = 180;
                    break;
                case ExifOrientation.BottomLeft:
                    _angle = 270;
                    break;
            }

            if (_angle > 0d)
            {
                capturedImage = RotateStream(e.ChosenPhoto, _angle);
            }
            else
            {
                capturedImage = e.ChosenPhoto;
                photo = e;
            }

            BitmapImage bmp = new BitmapImage();
            bmp.SetSource(capturedImage);

            ChosenPicture.Source = bmp;
        }

        private Stream RotateStream(Stream stream, int angle)
        {
            stream.Position = 0;
            if (angle % 90 != 0 || angle < 0) throw new ArgumentException();
            if (angle % 360 == 0) return stream;

            BitmapImage bitmap = new BitmapImage();
            bitmap.SetSource(stream);
            WriteableBitmap wbSource = new WriteableBitmap(bitmap);

            WriteableBitmap wbTarget = null;
            if (angle % 180 == 0)
            {
                wbTarget = new WriteableBitmap(wbSource.PixelWidth, wbSource.PixelHeight);
            }
            else
            {
                wbTarget = new WriteableBitmap(wbSource.PixelHeight, wbSource.PixelWidth);
            }

            for (int x = 0; x < wbSource.PixelWidth; x++)
            {
                for (int y = 0; y < wbSource.PixelHeight; y++)
                {
                    switch (angle % 360)
                    {
                        case 90:
                            wbTarget.Pixels[(wbSource.PixelHeight - y - 1) + x * wbTarget.PixelWidth] = wbSource.Pixels[x + y * wbSource.PixelWidth];
                            break;
                        case 180:
                            wbTarget.Pixels[(wbSource.PixelWidth - x - 1) + (wbSource.PixelHeight - y - 1) * wbSource.PixelWidth] = wbSource.Pixels[x + y * wbSource.PixelWidth];
                            break;
                        case 270:
                            wbTarget.Pixels[y + (wbSource.PixelWidth - x - 1) * wbTarget.PixelWidth] = wbSource.Pixels[x + y * wbSource.PixelWidth];
                            break;
                    }
                }
            }
            MemoryStream targetStream = new MemoryStream();
            wbTarget.SaveJpeg(targetStream, wbTarget.PixelWidth, wbTarget.PixelHeight, 0, 100);
            return targetStream;
        }
        //This will enable Cameradevice to be opened when clicked
        private void OnMenuTakeClicked(object sender, EventArgs e)
        {
            CameraCaptureTask cam = new CameraCaptureTask();
            cam.Completed += new EventHandler<PhotoResult>(OnCameraCaptureCompleted);
            cam.Show();
        }
        //This will enable chooser task to choose picture to upload
        private void OnMenuChooseClicked(object sender, EventArgs e)
        {
            PhotoChooserTask pix = new PhotoChooserTask();
            pix.Completed += new EventHandler<PhotoResult>(OnCameraCaptureCompleted);
            pix.ShowCamera = true;
            pix.Show();
        }
      
       
        //Call unction/method
        private void OnPostClicked(object sender, System.Windows.RoutedEventArgs e)
        {
            UploadImage(photo);

        }





        private void UploadImage(PhotoResult e)
        {

            //ShowProgress = true;
            byte[] sbytedata = ReadToEnd(e.ChosenPhoto);
            string s = sbytedata.ToString();
            albId = app.albums[app.selectedAlbumIndex].id;
            username = (string)appSettings["myEmail"];
            username = username.Substring(0, username.IndexOf("@"));// Get the username part of the email, without @
            WebClient webClient = new WebClient();
            string auth = "GoogleLogin auth=" + app.auth;
            webClient.Headers[HttpRequestHeader.Authorization] = auth;
            webClient.Headers[HttpRequestHeader.ContentType] = "image/jpeg";
            //webClient.Headers[HttpRequestHeader.ContentLength]=(string) e.ChosenPhoto.Length;
            Uri uri = new Uri(string.Format("https://picasaweb.google.com/data/feed/api/user/{0}/albumid/{1}", username, albId));
            webClient.AllowReadStreamBuffering = true;
            webClient.AllowWriteStreamBuffering = true;
            webClient.OpenWriteCompleted += new OpenWriteCompletedEventHandler(webClient_OpenWriteCompleted);
            webClient.OpenWriteAsync(uri, "POST", sbytedata);
        }

        public static void webClient_OpenWriteCompleted(object sender, OpenWriteCompletedEventArgs e)
         {
             if (e.Error == null)
             {
                 object[] objArr = e.UserState as object[];
                 byte[] fileContent = e.UserState as byte[];

                 Stream outputStream = e.Result;
                 outputStream.Write(fileContent, 0, fileContent.Length);
                 outputStream.Flush();
                 outputStream.Close();
                 string s = e.Result.ToString();
                 MessageBox.Show("Image has been uploaded successfully !");

             }

             else
             {
                 MessageBox.Show("There has been an error while uploading image. Please try again ...!!");
             }

         }

         public static byte[] ReadToEnd(System.IO.Stream stream)
         {
             long originalPosition = stream.Position;
             stream.Position = 0;

             try
             {
                 byte[] readBuffer = new byte[4096];

                 int totalBytesRead = 0;
                 int bytesRead;

                 while ((bytesRead = stream.Read(readBuffer, totalBytesRead, readBuffer.Length - totalBytesRead)) > 0)
                 {
                     totalBytesRead += bytesRead;

                     if (totalBytesRead == readBuffer.Length)
                     {
                         int nextByte = stream.ReadByte();
                         if (nextByte != -1)
                         {
                             byte[] temp = new byte[readBuffer.Length * 2];
                             Buffer.BlockCopy(readBuffer, 0, temp, 0, readBuffer.Length);
                             Buffer.SetByte(temp, totalBytesRead, (byte)nextByte);
                             readBuffer = temp;
                             totalBytesRead++;
                         }
                     }
                 }

                 byte[] buffer = readBuffer;
                 if (readBuffer.Length != totalBytesRead)
                 {
                     buffer = new byte[totalBytesRead];
                     Buffer.BlockCopy(readBuffer, 0, buffer, 0, totalBytesRead);
                 }
                 return buffer;
             }
             finally
             {
                 stream.Position = originalPosition;
             }

         }
    }
}