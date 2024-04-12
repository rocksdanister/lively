using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;

namespace Lively.UI.WinUI.UserControls
{
    // View and change image with transition effect.
    // Updating image flicker otherwise, ref: https://github.com/microsoft/microsoft-ui-xaml/issues/8750
    public sealed partial class ImageEx : UserControl
    {
        public string Source
        {
            get { return (string)GetValue(SourceProperty); }
            set
            {
                if (value != Source)
                    UpdateImage(value);

                SetValue(SourceProperty, value);
            }
        }

        public static readonly DependencyProperty SourceProperty =
            DependencyProperty.Register("Source", typeof(string), typeof(ImageEx), new PropertyMetadata(null, OnDependencyPropertyChanged));

        // x:Bind cannot be null/empty for image: https://stackoverflow.com/questions/31897154/xbind-image-with-null-string
        public string SourceA
        {
            get { return (string)GetValue(SourceAProperty); }
            private set { SetValue(SourceAProperty, string.IsNullOrEmpty(value) ? "nil" : value); }
        }

        public static readonly DependencyProperty SourceAProperty =
            DependencyProperty.Register("SourceA", typeof(string), typeof(ImageEx), new PropertyMetadata("nil"));

        public string SourceB
        {
            get { return (string)GetValue(SourceBProperty); }
            private set { SetValue(SourceBProperty, string.IsNullOrEmpty(value) ? "nil" : value); }
        }

        public static readonly DependencyProperty SourceBProperty =
            DependencyProperty.Register("SourceB", typeof(string), typeof(ImageEx), new PropertyMetadata("nil"));

        public bool IsSourceAVisible
        {
            get { return (bool)GetValue(IsSourceAVisibleProperty); }
            private set { SetValue(IsSourceAVisibleProperty, value); }
        }

        public static readonly DependencyProperty IsSourceAVisibleProperty =
            DependencyProperty.Register("IsSourceAVisible", typeof(bool), typeof(ImageEx), new PropertyMetadata(true));

        public bool IsSourceBVisible
        {
            get { return (bool)GetValue(IsSourceBVisibleProperty); }
            private set { SetValue(IsSourceBVisibleProperty, value); }
        }

        public static readonly DependencyProperty IsSourceBVisibleProperty =
            DependencyProperty.Register("IsSourceBVisible", typeof(bool), typeof(ImageEx), new PropertyMetadata(false));

        public Stretch Stretch
        {
            get { return (Stretch)GetValue(StretchProperty); }
            set { SetValue(StretchProperty, value); }
        }

        public static readonly DependencyProperty StretchProperty =
            DependencyProperty.Register("Stretch", typeof(Stretch), typeof(ImageEx), new PropertyMetadata(Stretch.Uniform, OnDependencyPropertyChanged));

        private static void OnDependencyPropertyChanged(DependencyObject s, DependencyPropertyChangedEventArgs e)
        {
            var obj = s as ImageEx;
            if (e.Property == SourceProperty)
                obj.Source = (string)e.NewValue;
            else if (e.Property == StretchProperty)
                obj.Stretch = (Stretch)e.NewValue;
        }

        private bool toggleFlag;

        public ImageEx()
        {
            this.InitializeComponent();
        }

        private void UpdateImage(string img)
        {
            if (!toggleFlag)
            {
                SourceA = img;
                IsSourceAVisible = true;
                IsSourceBVisible = false;
            }
            else
            {
                SourceB = img;
                IsSourceBVisible = true;
                IsSourceAVisible = false;
            }
            toggleFlag = !toggleFlag;
        }
    }
}
