using Ionic.Zip;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
//using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using MessageBox = System.Windows.MessageBox;
using Path = System.IO.Path;
using System.Globalization;
using Microsoft.Win32;
using livelywpf.Lively.Helpers;

namespace livelywpf
{
    /// <summary>
    /// Interaction logic for Page_Ext.xaml
    /// </summary>
    public partial class PageZipCreate : Page
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        public class BindClass
        {
            public SaveData.LivelyInfo LivelyInfo { get; set; }
            public BitmapImage Img { get; set; }

            public BindClass(SaveData.LivelyInfo info)
            {
                if (info != null)
                {
                    LivelyInfo = info;
                    Img = LoadImage(info.Thumbnail);
                }
            }
            private BitmapImage LoadImage(string filename)
            {
                try
                {
                    using (var stream = File.OpenRead(filename))
                    {
                        var bmp = new BitmapImage();
                        bmp.BeginInit();
                        bmp.StreamSource = stream;
                        bmp.CacheOption = BitmapCacheOption.OnLoad;
                        bmp.EndInit();
                        return bmp;
                    }
                }
                catch (Exception)
                {
                    return null;
                }
            }

        }

        private SaveData.LivelyInfo tmpInfo = new SaveData.LivelyInfo();
        private ObservableCollection<BindClass> data = new ObservableCollection<BindClass>();

        public PageZipCreate()
        {
            InitializeComponent();
            data.Add(new BindClass(tmpInfo));
            PreviewPanel.ItemsSource = data;

            foreach (var item in wallpaperTypes)
            {
                comboBoxType.Items.Add(item.LocalisedType);
            }
            comboBoxType.SelectedIndex = 0;
        }

        private class FileFilter
        {
            public SetupDesktop.WallpaperType Type { get; set; }
            public string FilterText { get; set; }
            public string LocalisedType { get; set; }

            public FileFilter(SetupDesktop.WallpaperType type, string filterText)
            {
                this.Type = type;
                this.FilterText = filterText;

                if (this.Type == SetupDesktop.WallpaperType.video)
                {
                    LocalisedType = Properties.Resources.txtLivelyWallpaperTypeVideo;
                }
                else if (this.Type == SetupDesktop.WallpaperType.app)
                {
                    LocalisedType = Properties.Resources.txtLivelyWallpaperTypeApp;
                }
                else if(this.Type == SetupDesktop.WallpaperType.godot)
                {
                    LocalisedType = Properties.Resources.txtLivelyWallpaperTypeGodot;
                }
                else if (this.Type == SetupDesktop.WallpaperType.unity)
                {
                    LocalisedType = Properties.Resources.txtLivelyWallpaperTypeUnity;
                }
                else if (this.Type == SetupDesktop.WallpaperType.unity_audio)
                {
                    LocalisedType = Properties.Resources.txtLivelyWallpaperTypeUnityAudio;
                }
                else if (this.Type == SetupDesktop.WallpaperType.web)
                {
                    LocalisedType = Properties.Resources.txtLivelyWallpaperTypeWeb;
                }
                else if (this.Type == SetupDesktop.WallpaperType.web_audio)
                {
                    LocalisedType = Properties.Resources.txtLivelyWallpaperTypeWebAudio;
                }
                else if (this.Type == SetupDesktop.WallpaperType.gif)
                {
                    LocalisedType = Properties.Resources.txtLivelyWallpaperTypeGIF;
                }
                else
                {
                    LocalisedType = Type.ToString();
                }
            }
        }

        readonly FileFilter[] wallpaperTypes= new FileFilter[] { 
            new FileFilter(SetupDesktop.WallpaperType.video, "All Videos Files |*.dat; *.wmv; *.3g2; *.3gp; *.3gp2; *.3gpp; *.amv; *.asf;  *.avi; *.bin; *.cue; *.divx; *.dv; *.flv; *.gxf; *.iso; *.m1v; *.m2v; *.m2t; *.m2ts; *.m4v; " +
                  " *.mkv; *.mov; *.mp2; *.mp2v; *.mp4; *.mp4v; *.mpa; *.mpe; *.mpeg; *.mpeg1; *.mpeg2; *.mpeg4; *.mpg; *.mpv2; *.mts; *.nsv; *.nuv; *.ogg; *.ogm; *.ogv; *.ogx; *.ps; *.rec; *.rm; *.rmvb; *.tod; *.ts; *.tts; *.vob; *.vro; *.webm"), 
            new FileFilter(SetupDesktop.WallpaperType.gif,"Animated GIF (*.gif) |*.gif"), new FileFilter(SetupDesktop.WallpaperType.web, "Web Page (*.html) |*.html"),
            new FileFilter(SetupDesktop.WallpaperType.web_audio, "Audio Visualiser(web) (*.html) |*.html"), new FileFilter(SetupDesktop.WallpaperType.unity,"Unity Game Executable |*.exe"), 
            new FileFilter(SetupDesktop.WallpaperType.unity_audio,"Unity Audio Visualiser |*.exe"), new FileFilter(SetupDesktop.WallpaperType.app,"Application |*.exe"),
            new FileFilter(SetupDesktop.WallpaperType.godot,"Godot Game Executable |*.exe")
        };

        List<string> folderContents = new List<string>();
        private void Button_Click_Browse(object sender, RoutedEventArgs e) //application
        {
            // folderContents.Clear();
            if (comboBoxType.SelectedIndex == -1)
                return;

            OpenFileDialog openFileDialog1 = new OpenFileDialog();
            //var result = Array.Find(wallpaperTypes,x => x.Type == comboBoxType.SelectedItem);
            var result = wallpaperTypes[comboBoxType.SelectedIndex];
            openFileDialog1.Filter = result.FilterText;
            //openFileDialog1.Title = "Select the file, folder contents will be added based on wallpaper type";
            if (openFileDialog1.ShowDialog() == true)
            {
                tmpInfo.FileName = openFileDialog1.FileName;
            }
            else
            {
                return;
            }

            if (result.Type == SetupDesktop.WallpaperType.video || result.Type == SetupDesktop.WallpaperType.gif)
            {
                folderContents.Add(tmpInfo.FileName);
            }
            else
            {
                folderContents.AddRange( Directory.GetFiles( Directory.GetParent( tmpInfo.FileName ).ToString() , "*.*", SearchOption.AllDirectories) );
            }

            CreateWallpaperAddedFiles w = new CreateWallpaperAddedFiles(folderContents)
            {
                Owner = Application.Current.MainWindow,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };
            w.ShowDialog();

            if (w.DialogResult.HasValue && w.DialogResult.Value) //ok btn
            {
                //SaveData.SaveWallpaperMetaData(tmpInfo, )
            }
            else //back btn
            {
                folderContents.Clear();
            }
        }

        private async void Button_Create_Click(object sender, RoutedEventArgs e)
        {
            tmpInfo.Title =  textboxTitle.Text;
            tmpInfo.Author = textboxAuthor.Text;
            tmpInfo.Desc = textboxDesc.Text;
            tmpInfo.Contact = textboxWebsite.Text;
            tmpInfo.License = textboxLicense.Text;
            //tmpInfo.Type = (SetupDesktop.WallpaperType)comboBoxType.SelectedItem;
            tmpInfo.Type = wallpaperTypes[comboBoxType.SelectedIndex].Type;
            tmpInfo.Arguments = textboxArgs.Text;

            if (folderContents.Count == 0 || String.IsNullOrWhiteSpace(tmpInfo.FileName))
            {
                MessageBox.Show(Properties.Resources.txtMsgSelectWallpaperFile);
                return;
            }

            if ( String.IsNullOrEmpty(tmpInfo.Title) || String.IsNullOrEmpty(tmpInfo.Desc) || String.IsNullOrEmpty(tmpInfo.Author) 
                    )//|| String.IsNullOrEmpty(tmpInfo.contact) )
            {
                MessageBox.Show(Properties.Resources.txtMsgFillAllFields);
                return;
            }
            
            /*
            //Optional
            if( !File.Exists(tmpInfo.Thumbnail) || !File.Exists(tmpInfo.Preview) )
            {
                MessageBox.Show(Properties.Resources.txtSelectPreviewThumb);
                return;
            }
            */

            SaveFileDialog saveFileDialog1 = new SaveFileDialog
            {
                Title = "Select location to save the file",
                Filter = "Lively/zip file|*.zip",
                OverwritePrompt = true
            };

            if (saveFileDialog1.ShowDialog() == true)
            {
                if (!String.IsNullOrEmpty(saveFileDialog1.FileName))
                {
                    //to write to Livelyinfo.json file only, tmp object.
                    SaveData.LivelyInfo tmp = new SaveData.LivelyInfo(tmpInfo);
                    tmp.FileName = Path.GetFileName(tmp.FileName);
                    tmp.Preview = Path.GetFileName(tmp.Preview);
                    tmp.Thumbnail = Path.GetFileName(tmp.Thumbnail);

                    SaveData.SaveWallpaperMetaData(tmp, Path.Combine(App.PathData, "tmpdata"));

                    /*
                    //if previous livelyinfo.json file(s) exists in wallpaper directory, remove all of them.
                    folderContents.RemoveAll(x => Path.GetFileName(x).Equals(Path.GetFileName(folderContents[folderContents.Count - 1]),
                                        StringComparison.InvariantCultureIgnoreCase));
                                        */
                    folderContents.Add( Path.Combine(App.PathData, "tmpdata","LivelyInfo.json"));

                    //btnCreateWallpaer.IsEnabled = false;
                    await CreateZipFile(saveFileDialog1.FileName, folderContents);

                    string folderPath = System.IO.Path.GetDirectoryName(saveFileDialog1.FileName);
                    if (Directory.Exists(folderPath))
                    {
                        ProcessStartInfo startInfo = new ProcessStartInfo
                        {
                            Arguments = folderPath,
                            FileName = "explorer.exe"
                        };
                        Process.Start(startInfo);
                    }

                    //clearing temp files if any.
                    FileOperations.EmptyDirectory(Path.Combine(App.PathData, "SaveData", "wptmp"));
                    //this.NavigationService.GoBack(); //won't work, since prev is window, not page.
                    var wnd = Window.GetWindow(this);
                    wnd.Close();
                }
            }
            else
            {
                return;
            }

        }

        private async Task CreateZipFile(string savePath, List<string> documentPaths)
        {          
            //savefiledialog will promt user if replacement.
            if(File.Exists(savePath))
            {
                try
                {
                    File.Delete(savePath);
                }
                catch(Exception e)
                {
                    Logger.Info(e.ToString());
                    MessageBox.Show("Failed to delete existing file on disk, skipping!",Properties.Resources.txtLivelyErrorMsgTitle);
                    return;
                }

            }

            string baseDirectory = Directory.GetParent(tmpInfo.FileName).ToString();
            try
            {
                using (ZipFile zip = new ZipFile(savePath))
                {
                    zip.CompressionLevel = Ionic.Zlib.CompressionLevel.BestCompression;
                    zip.ZipErrorAction = ZipErrorAction.Throw;

                    //relative path calculations.
                    if (documentPaths.Count > 0)
                    {   
                        //livelyinfo.json file in root.
                        zip.AddFile(documentPaths[documentPaths.Count - 1], "");
                        //root directory of zip.
                        if (File.Exists(tmpInfo.Thumbnail))
                            zip.AddFile(tmpInfo.Thumbnail, "");
                        if(File.Exists(tmpInfo.Preview))
                            zip.AddFile(tmpInfo.Preview, "");

                        for (int i = 0; i < (documentPaths.Count - 1); i++)
                        {
                            try
                            {
                                //adding files in root directory of zip, maintaining folder structure.
                                zip.AddFile(documentPaths[i], Path.GetDirectoryName(documentPaths[i]).Replace(baseDirectory, string.Empty));
                            }
                            catch
                            {
                                //ignore Liveinfo.json, preview, thumbnail file if already exists in the wallpaper path(from previous wp zip maybe).
                                //Note: will skip other repeating files too in the path, should not be an issue in this particular case.
                                Logger.Info("zip: ignoring some files due to repeated filename.");
                            }
                        }
                    }
       
                    zip.SaveProgress += Zip_SaveProgress;
                    
                    await Task.Run(() => zip.Save());
                }               
            }
            catch(Ionic.Zip.ZipException e1)
            {
                MessageBox.Show("File creation failure:" + e1.ToString());
                Logger.Error(e1.ToString());
            }
            catch(Exception e2)
            {
                MessageBox.Show("File creation failure:" + e2.ToString());
                Logger.Error(e2.ToString());
            }
        }

        private bool initializeProgressbar = false;
        private void Zip_SaveProgress(object sender, SaveProgressEventArgs e)
        {
            if (e.EntriesTotal != 0)
            {
                if (!initializeProgressbar)
                {
                    this.Dispatcher.Invoke(() =>
                    {
                        zipProgressBar.Minimum = 0;
                        zipProgressBar.Maximum = e.EntriesTotal;
                    });

                    initializeProgressbar = true;
                }

                this.Dispatcher.Invoke(() =>
                {
                    zipProgressBar.Value = e.EntriesSaved;
                });
           
            }
        }

        private void ThumbnailBtn_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog1 = new OpenFileDialog
            {
                Filter = "Image (*.jpg) |*.jpg; *.jpeg",
                Title = "Select 200x200 Image File"
            };
            if (openFileDialog1.ShowDialog() == true)
            {
                using (var imageStream = File.OpenRead(openFileDialog1.FileName))
                {
                    var decoder = BitmapDecoder.Create(imageStream, BitmapCreateOptions.IgnoreColorProfile,
                        BitmapCacheOption.Default);
                    var height = decoder.Frames[0].PixelHeight;
                    var width = decoder.Frames[0].PixelWidth;

                    if(height != 200 & width != 200)
                    {
                        //MessageBox.Show("Select ONLY 200x200(width x height) image thumbnail file");
                        //return;
                        string saveFileName = Path.Combine(App.PathData, "tmpdata","wpdata", Path.GetRandomFileName() + ".jpg");
                        ImageOperations.ResizeImage(openFileDialog1.FileName, saveFileName, new System.Drawing.Size(200, 200));

                        tmpInfo.Thumbnail = saveFileName;
                        data.Clear();
                        data.Add(new BindClass(tmpInfo));
                    }
                    else
                    {
                        tmpInfo.Thumbnail = openFileDialog1.FileName;
                        data.Clear();
                        data.Add(new BindClass(tmpInfo));
                    }
                }
            }
            else
            {
                return;
            }
        }

        private void PreviewClipBtn_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog1 = new OpenFileDialog
            {
                Filter = "Animated GIF (*.gif) |*.gif",
                Title = "Select 192x108 GIF File"
            };
            if (openFileDialog1.ShowDialog() == true)
            {
                //var size = MainWindow.GetVideoSize(openFileDialog1.FileName);

                using (var imageStream = File.OpenRead(openFileDialog1.FileName))
                {
                    var decoder = BitmapDecoder.Create(imageStream, BitmapCreateOptions.IgnoreColorProfile,
                        BitmapCacheOption.Default);
                    var height = decoder.Frames[0].PixelHeight;
                    var width = decoder.Frames[0].PixelWidth;

                    if (width != 192 && height != 108)
                    {
                        //MessageBox.Show("Select ONLY 192x108(width x height) GIF preview file");
                        //return;
                        string saveFileName = Path.Combine(App.PathData, "tmpdata","wpdata", Path.GetRandomFileName() + ".gif");
                        ImageOperations.ResizeGif(openFileDialog1.FileName, saveFileName, new System.Drawing.Size(192, 108));

                        tmpInfo.Preview = saveFileName;
                        data.Clear();
                        data.Add(new BindClass(tmpInfo));
                    }
                    else
                    {
                        tmpInfo.Preview = openFileDialog1.FileName;
                        data.Clear();
                        data.Add(new BindClass(tmpInfo));
                    }
                }
            }
            else
            {
                return;
            }
        }

    }
}
