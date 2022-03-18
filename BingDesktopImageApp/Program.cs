using Newtonsoft.Json;
using System;
using System.Drawing;
using System.IO;
using System.Net;
using System.Reflection;
using System.Windows.Forms;

namespace BingDesktopImageApp
{
    public class Program
    {
        private static string BingUrl = "http://www.bing.com/HPImageArchive.aspx?idx=0&n=1&format=js";

        private static System.Timers.Timer Timer { get; set; }

        private static System.Threading.ManualResetEvent _quitEvent = new System.Threading.ManualResetEvent(false);
        private static int ErrorCount { get; set; }

        private static NotifyIcon TrayIcon { get; set; }

        static void Main(string[] args)
        {
            TrayIcon = new NotifyIcon();
            AddTrayIcon();
            ErrorCount = 0;
            Console.CancelKeyPress += (sender, eArgs) =>
            {
                _quitEvent.Set();
                eArgs.Cancel = true;
            };
            StartTimer();
            UpdateBackgroundImage(); // Try immediately
            _quitEvent.WaitOne();
            
        }

        private static void StopTimer()
        {
            Timer.Stop();
            Timer.Dispose();
            //Environment.Exit(0); // Don't close app. Keep tray icon around
        }

        private static void StartTimer()
        {
            Timer = new System.Timers.Timer(1000 * 60 * 5); // 5 minutes
            Timer.Elapsed += Timer_Elapsed;
            Timer.Start();
        }

        static void Timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            UpdateBackgroundImage();
            //StopTimer();
        }

        private static object lck = new Object();

        private static void IncrementTimer()
        {
            var interval = Math.Min(Timer.Interval * 2, 3600000); // Double the timer interval (to a max of 1 hour);
            Timer.Interval = interval;
        }

        private static void UpdateBackgroundImage()
        {
            lock (lck)
            {
                try
                {
                    var imageDir = System.AppDomain.CurrentDomain.BaseDirectory + "images\\";
                    if (!Directory.Exists(imageDir))
                    {
                        Directory.CreateDirectory(imageDir);
                    }
                    using (var stream = MakeRequest(BingUrl))
                    using (var reader = new StreamReader(stream))
                    {
                        var data = reader.ReadToEnd();
                        dynamic json = JsonConvert.DeserializeObject(data);
                        var images = json.images;
                        
                        for (var i = 0; i < images.Count; i++)
                        {
                            var image = images[0];

                            var imageDetail = image.copyright.ToString();
                            //var copyrightIndex = imageDetail.IndexOf("(©");
                            //if (copyrightIndex > -1)
                            //{
                            //    imageDetail = imageDetail.Substring(0, copyrightIndex - 1).Trim();
                            //}
                            var maxIndex = Math.Min(63, imageDetail.Length);
                            imageDetail = imageDetail.Substring(0, Math.Min(63, imageDetail.Length));

                            if (TrayIcon.Text != imageDetail)
                            {
                                TrayIcon.Text = imageDetail;
                            }

                            var urlbase = image.url.ToString() as string;
                            //var imageName = urlbase.Split("/".ToCharArray()).Last();
                            var imageName = image.copyright.ToString() as string;
                            var index = imageName.IndexOf("(");
                            imageName = imageName.Substring(0, index-1) + ".jpg";
                            var path = imageDir + imageName;


                            var dateString = image.startdate.ToString();
                            var date = DateTime.ParseExact(dateString, "yyyyMMdd", System.Globalization.CultureInfo.InvariantCulture);
                            
                            if (File.Exists(path))
                            {
                                // Set background anyway - maybe it wasn't set before
                                DesktopUtility.SetDesktopBackground(path, BackgroundStyle.Stretched);

                                if (date >= DateTime.Today)
                                {
                                    StopTimer();
                                    return;
                                }

                                IncrementTimer();
                                return;
                            }

                            var url = ("http://www.bing.com" + image.url) as string;
                            using (var imageStream = MakeRequest(url))
                            using (var imageReader = new BinaryReader(imageStream))
                            {
                                var imageData = imageReader.ReadBytes(1 * 1024 * 1024 * 10);

                                File.WriteAllBytes(path, imageData);
                                DesktopUtility.SetDesktopBackground(path, BackgroundStyle.Stretched);

                                ShowNotification(imageDetail);
                            }

                            if (date >= DateTime.Today)
                            {
                                StopTimer();
                                return;
                            }
                            
                            //StopTimer(); // The timer only has to run again the next day.
                        }
                    }
                }
                catch (Exception e)
                {
                    //Something went wrong. maybe it was no internet connection etc. 
                    //Maybe it will work next time
                    ErrorCount++;
                    if (ErrorCount > 5)
                    {
                        // More than 5 errors something is not working, rather stop the process
                        StopTimer();
                    }
                }
            }
        }

        private static void onExitClick(object sender, EventArgs e)
        {
            Environment.Exit(0);
        }

        private static void AddTrayIcon()
        {
            var iconStream = Assembly.GetExecutingAssembly().GetManifestResourceStream("BingDesktopImageApp.bing.ico");
            TrayIcon.Icon = new Icon(iconStream);

            var menu = new ContextMenu();
            menu.MenuItems.Add(new MenuItem("E&xit", onExitClick));

            TrayIcon.Visible = true;
            
            TrayIcon.ContextMenu = menu;
        }

        private static void ShowNotification(string message)
        {
            TrayIcon.Text = message;
            TrayIcon.BalloonTipIcon = ToolTipIcon.Info;
            TrayIcon.BalloonTipTitle = "Bing Desktop Image";
            TrayIcon.BalloonTipText = message;
            TrayIcon.ShowBalloonTip(5000);
        }

        private static Stream MakeRequest(string url)
        {
            var request = WebRequest.Create(url);
            var response = request.GetResponse();
            return response.GetResponseStream();
        }
    }
}
