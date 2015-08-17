using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Media.Animation;

namespace XapEditor.controls
{
    public partial class MetroTile : UserControl
    {
        string _icon;
        string _text;
        SolidColorBrush _bg;
        public SolidColorBrush BackgroundBrush
        {
            get { return _bg; }
            set
            {
                _bg = value;
                bor.Background = _bg;
            }
        }
        public string Icon
        {
            get { return _icon; }
            set
            {
                _icon = value;
                img.Source = new BitmapImage(new Uri(string.Format("/icons/win8/{0}.png", Icon), UriKind.Relative));
            }
        }
        public string Text
        {
            get { return _text; }
            set
            {
                _text = value;
                txt.Text = _text;
            }
        }
        public event RoutedEventHandler Click;
        protected void OnClick() { if (Click != null) Click(this, new RoutedEventArgs()); }
        public MetroTile()
        {
            InitializeComponent();
            TimeSpan d = TimeSpan.FromMilliseconds(200);
            Storyboard sb = new Storyboard() { Duration = d };
            DoubleAnimation da = new DoubleAnimation() { Duration = d };
            Storyboard.SetTarget(da, grr);
            Storyboard.SetTargetProperty(da, new PropertyPath(Grid.OpacityProperty));
            bool cl = false;
            sb.Completed += delegate(object s, EventArgs e) { if (cl) OnClick(); };
            grr.MouseLeftButtonUp += delegate(object s, System.Windows.Input.MouseButtonEventArgs e)
            {
                TimeSpan d1 = TimeSpan.FromMilliseconds(d.TotalMilliseconds / 3);
                TimeSpan d2 = TimeSpan.FromMilliseconds((d.TotalMilliseconds / 3) * 2);
                DoubleAnimation da1 = da.Clone();
                da1.To = 1;
                da1.Duration = d1;
                DoubleAnimation da2 = da.Clone();
                da2.To = 0;
                da2.BeginTime = d1;
                da2.Duration = d2;
                sb.Children.Clear();
                sb.Children.Add(da1);
                sb.Children.Add(da2);
                sb.Begin();
                cl = true;
            };
            grr.MouseEnter += delegate(object s, System.Windows.Input.MouseEventArgs e)
            {
                da.To = 0.2f;
                sb.Children.Clear();
                sb.Children.Add(da);
                sb.Begin();
                cl = false;
            };
            grr.MouseLeave += delegate(object s, System.Windows.Input.MouseEventArgs e)
            {
                da.To = 0;
                sb.Children.Clear();
                sb.Children.Add(da);
                sb.Begin();
                cl = false;
            };
        }
    }
}
