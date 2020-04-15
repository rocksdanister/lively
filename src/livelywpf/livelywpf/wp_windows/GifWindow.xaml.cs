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
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using XamlAnimatedGif;

namespace livelywpf
{
    /// <summary>
    /// Interaction logic for GifWindow.xaml
    /// </summary>
    public partial class GifWindow : Window
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        Animator animator;
        public GifWindow(string path)
        {
            InitializeComponent();
            this.Loaded += GifWindow_Loaded;

            gifImg.Stretch = SaveData.config.GifScaler;
            AnimationBehavior.AddErrorHandler(gifImg, ErrorEvent);
            AnimationBehavior.AddLoadedHandler(gifImg, AnimationBehavior_OnLoaded);
            AnimationBehavior.SetSourceUri(gifImg, new Uri(path));
            AnimationBehavior.SetRepeatBehavior(gifImg, System.Windows.Media.Animation.RepeatBehavior.Forever);
        }

        private void GifWindow_Loaded(object sender, RoutedEventArgs e)
        {
            //ShowInTaskbar = false :- causing issue with windows10 Taskview.
            SetupDesktop.RemoveWindowFromTaskbar(new WindowInteropHelper(this).Handle);
        }

        private void ErrorEvent(object s, AnimationErrorEventArgs e) 
        {
            Logger.Error("GIF:" + e.ToString());
            MessageBox.Show(Properties.Resources.msgGIFfailure, Properties.Resources.txtLivelyErrorMsgTitle);
        }

        private void AnimationBehavior_OnLoaded(object sender, RoutedEventArgs e)
        {
            try
            {
                animator = AnimationBehavior.GetAnimator(gifImg);
                animator.Play();
            }
            catch //prevents playback(crash otherwise), error gets logged in ErrorEvent()
            { }
        }

        public void PausePlayer()
        {
            if(animator != null)
            {
                if(animator.IsPaused == false)
                    animator.Pause();
            }
        }

        public void ResumePlayer()
        {
            if (animator != null)
            {
                if (animator.IsPaused == true)
                    animator.Play();
            }
        }
 
        private void GifImg_Loaded(object sender, RoutedEventArgs e)
        {

        }

        //prevent mouseclick focus steal(bottom-most wp rendering mode).
        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            var source = PresentationSource.FromVisual(this) as HwndSource;
            source.AddHook(WndProc);
        }
        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == WM_MOUSEACTIVATE)
            {
                handled = true;
                return new IntPtr(MA_NOACTIVATE);
            }
            else
            {
                return IntPtr.Zero;
            }
        }
        private const int WM_MOUSEACTIVATE = 0x0021;
        private const int MA_NOACTIVATE = 0x0003;
    }
}
