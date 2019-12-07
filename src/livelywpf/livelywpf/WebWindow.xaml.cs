using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

//using CefSharp;
//using CefSharp.Wpf;
//using CefSharp.WinForms;
//using System.Windows.Forms;
//using CefSharp.SchemeHandler;

namespace livelywpf
{
    /// <summary>
    /// Interaction logic for WebWindow.xaml
    /// </summary>
    public partial class WebWindow : Window
    {
        
        public static string currURL = null;
        //private IKeyboardMouseEvents mouseHook;
        //private static bool mHooked = false;
        public ChromiumWebBrowser chromeBrowser;
        public bool cefInitialized = false;

        string path;
        public WebWindow(string _path)
        {
            InitializeComponent();
            path = _path;
            //InitializeChromium(path);
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            InitializeChromium(path);
            // Create the interop host control.
            System.Windows.Forms.Integration.WindowsFormsHost host =
                new System.Windows.Forms.Integration.WindowsFormsHost();

            // Create the MaskedTextBox control.
            //MaskedTextBox mtbDate = new MaskedTextBox("00/00/0000");

            // Assign the MaskedTextBox control as the host control's child.
            host.Child = chromeBrowser;//mtbDate;

            // Add the interop host control to the Grid
            // control's collection of child controls.
            this.webGrid.Children.Add(host);

        }

        void InitializeChromium(string path)
        {

            //Program.filePath = path;
            CefSettings settings = new CefSettings();
            settings.RegisterScheme(new CefCustomScheme
            {
                SchemeName = "localfolder",
                DomainName = "cefsharp",
                SchemeHandlerFactory = new FolderSchemeHandlerFactory(
                       rootFolder: @"C:\Users\rocks\Documents\DEMO\WEB\RainEffect-master\demo",
                       hostName: "cefsharp",
                       defaultPage: "index.html" // will default to index.html
            )
            });


            //ref: https://magpcss.org/ceforum/apidocs3/projects/(default)/_cef_browser_settings_t.html#universal_access_from_file_urls
            //settings.CefCommandLineArgs.Add("allow-universal-access-from-files", "1"); //UNSAFE, Testing Only!
            //settings.CefCommandLineArgs.Add("--mute-audio", "1");

            Cef.Initialize(settings);
            //shadertoy link handling.
            if (path.Contains("shadertoy.com/view"))
            {
                if (!path.Contains("https://"))
                    path = "https://" + path;

                path = path.Replace("view/", "embed/");
                ShadertoyTmpHTML(path);
                path = AppDomain.CurrentDomain.BaseDirectory + @"\\shadertoy_url.html";
                //Program.filePath = path;
            }
            // Create a browser component
            //chromeBrowser = new ChromiumWebBrowser(@path);
            chromeBrowser = new ChromiumWebBrowser("localfolder://cefsharp/");
            //chromeBrowser = new ChromiumWebBrowser(@"http://localhost:8000/");
            // Add it to the form and fill it to the form window.
            //webGrid.Children.Add(chromeBrowser);
            //this.Controls.Add(chromeBrowser);
            chromeBrowser.Dock = DockStyle.Fill;

            //Subscribe(); //mousehook

            //chromeBrowser.IsBrowserInitializedChanged += ChromeBrowser_IsBrowserInitializedChanged;
            chromeBrowser.IsBrowserInitializedChanged += ChromeBrowser_IsBrowserInitializedChanged1;
            //chromeBrowser.LoadError += ChromeBrowser_LoadError;
            //chromeBrowser.Paint += ChromeBrowser_Paint;

            //chromeBrowser.Dispose();
        }

        public void MuteBrowser()
        {
          //  chromeBrowser.
        }

        private void ChromeBrowser_IsBrowserInitializedChanged1(object sender, EventArgs e)
        {
            cefInitialized = true;
            //throw new NotImplementedException();
        }

        /*
        private void ChromeBrowser_Paint(object sender, PaintEventArgs e)
        {
           System.Diagnostics.Debug.WriteLine("On PAINT CEF !!");
           //Bitmap newBitmap = new Bitmap(e.Width, e.Height, 4 * e.Width, System.Drawing.Imaging.PixelFormat.Format32bppRgb, e.Buffer);
           //throw new NotImplementedException();
        }
        */

        private void ChromeBrowser_LoadError(object sender, LoadErrorEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("ERROR LOAD !!");
            //chromeBrowser.Reload(true);

            //throw new NotImplementedException();
        }

        private void ShadertoyTmpHTML(string path)
        {

            string text = @"<!DOCTYPE html><html lang=""en"" dir=""ltr""> <head> <meta charset=""utf - 8""> 
                    <title>Digital Brain</title> <style media=""screen""> iframe { position: fixed; width: 100%; height: 100%; top: 0; right: 0; bottom: 0;
                    left: 0; z-index; -1; pointer-events: none; } </style> </head> <body> <iframe width=""640"" height=""360"" frameborder=""0"" 
                    src=" + path + @"?gui=false&t=10&paused=false&muted=true""></iframe> </body></html>";
            // WriteAllText creates a file, writes the specified string to the file,
            // and then closes the file.    You do NOT need to call Flush() or Close().
            System.IO.File.WriteAllText(AppDomain.CurrentDomain.BaseDirectory + @"\\shadertoy_url.html", text);

        }

        /*
        private void ChromeBrowser_IsBrowserInitializedChanged(object sender, CefSharp.IsBrowserInitializedChangedEventArgs e)
        {
            cefInitialized = e.IsBrowserInitialized;
            //throw new NotImplementedException();
        }
        */

        public void LoadURL(string path)
        {
           // if (!path.Equals("about:blank"))
             //   Subscribe(); //mousehook

            if (path.Contains("shadertoy.com/view"))
            {
                if (!path.Contains("https://"))
                    path = "https://" + path;

                path = path.Replace("view/", "embed/");
                ShadertoyTmpHTML(path);
                path = AppDomain.CurrentDomain.BaseDirectory + @"\\shadertoy_url.html";
               // Program.filePath = path;
            }

            chromeBrowser.Load(@path);
        }

        public string CurrURL()
        {
            return chromeBrowser.Address;
        }

        public void Minimize()
        {
            // this.WindowState = FormWindowState.Minimized;
            //this.Hide();
            this.WindowState = WindowState.Minimized;
            this.Hide();
        }

        public void Maximize()
        {
            this.WindowState = WindowState.Normal;
            this.Show();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            //not needed, exit event of app is hooked.
            //Cef.Shutdown();

            chromeBrowser.Dispose();
            cefInitialized = false;
        }


        /*
        private void WebForm_FormClosing(object sender, FormClosingEventArgs e)
        {

            chromeBrowser.IsBrowserInitializedChanged -= ChromeBrowser_IsBrowserInitializedChanged;
            Unsubscribe(); //mousehook
            Cef.Shutdown();

        }
        */
    }

}
