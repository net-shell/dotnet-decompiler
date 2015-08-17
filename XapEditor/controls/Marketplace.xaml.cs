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
using System.Xml;
using System.Xml.Linq;
using System.IO;
using Microsoft.Win32;
using System.Net;

namespace XapEditor.controls
{
    public partial class Marketplace : UserControl
    {
        string language { get { return ((cbLang.SelectedItem as ComboBoxItem).Content as string); } }
        string store { get { return ((cbStore.SelectedItem as ComboBoxItem).Content as string); } }
        List<MarketplaceEntry> entries = new List<MarketplaceEntry>();
        WebClient xapDownloader = new WebClient();
        WebClient infDownloader = new WebClient();
        string xapDownloadPath = string.Empty;
        MainWindow mainw;
        bool autoOpen = false;

        public Marketplace(MainWindow mw)
        {
            mainw = mw;
            InitializeComponent();
            infDownloader.DownloadProgressChanged += new DownloadProgressChangedEventHandler(downloadProgressChanged);
            xapDownloader.DownloadProgressChanged += new DownloadProgressChangedEventHandler(downloadProgressChanged);
            infDownloader.DownloadStringCompleted += new DownloadStringCompletedEventHandler(infDownloader_DownloadStringCompleted);
            xapDownloader.DownloadFileCompleted += new System.ComponentModel.AsyncCompletedEventHandler(xapDownloader_DownloadFileCompleted);
        }

        void downloadProgressChanged(object sender, DownloadProgressChangedEventArgs e) { prog.Value = e.ProgressPercentage; }

        void infDownloader_DownloadStringCompleted(object sender, DownloadStringCompletedEventArgs e)
        {
            if (e.Error == null)
            {
                XDocument doc = XDocument.Parse(e.Result);
                XNamespace ns = "http://www.w3.org/2005/Atom";
                XNamespace nz = "http://schemas.zune.net/catalog/apps/2008/02";
                entries = new List<MarketplaceEntry>();
                XElement xe = doc.Root.Element(ns + "entry").Element(nz + "url");
                xapDownloader.DownloadFileAsync(new Uri(xe.Value, UriKind.Absolute), xapDownloadPath);
            }
        }
        void xapDownloader_DownloadFileCompleted(object sender, System.ComponentModel.AsyncCompletedEventArgs e)
        {
            tbProg.Visibility = Visibility.Collapsed;
            prog.IsIndeterminate = true;
            if (autoOpen) mainw.OpenFile(xapDownloadPath);
            else MessageBox.Show(string.Format("Download successfull!\n{0}\n{1:0.00}KB", xapDownloadPath, (new FileInfo(xapDownloadPath).Length / 1024)));
        }

        string getSearchUrl(string query) { return string.Format("http://catalog.zune.net/v3.2/{0}/apps?q={1}&clientType=WinMobile%207.0&store={2}", language, query, store); }
        Uri getDownloadUrl(string id) { return new Uri(string.Format("http://catalog.zune.net/v3.2/{0}/apps/{1}?version=latest&clientType=WinMobile%207.0", language, id), UriKind.Absolute); }

        void downloadApp(string path = "", bool open = false)
        {
            autoOpen = open;
            if (path.Length == 0)
            {
                SaveFileDialog sfd = new SaveFileDialog();
                sfd.Filter = "XAP Files|*.xap";
                if ((bool)sfd.ShowDialog()) path = sfd.FileName;
                else return;
            }
            if (dgResult.SelectedItem != null)
            {
                xapDownloadPath = path;
                tbProg.Visibility = Visibility.Visible;
                prog.IsIndeterminate = false;
                infDownloader.DownloadStringAsync(getDownloadUrl((dgResult.SelectedItem as MarketplaceEntry).Id));
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            string url = getSearchUrl(tbQuery.Text);
            System.Net.WebClient wc = new System.Net.WebClient();
            string result = wc.DownloadString(url);
            XDocument doc = XDocument.Parse(result);
            XNamespace ns = "http://www.w3.org/2005/Atom";
            XNamespace nz = "http://schemas.zune.net/catalog/apps/2008/02";
            entries = new List<MarketplaceEntry>();
            foreach (XElement xe in doc.Root.Elements(ns + "entry").ToList())
            {
                string _id = xe.Element(ns + "id").Value;
                _id = _id.Substring(_id.LastIndexOf(':') + 1);
                try
                {
                    entries.Add(new MarketplaceEntry()
                    {
                        Id = _id,
                        Title = xe.Element(ns + "title").Value,
                        Category = xe.Element(nz + "categories").Element(nz + "category").Element(nz + "title").Value,
                        Version = xe.Element(nz + "version").Value,
                        Rating = float.Parse(xe.Element(nz + "averageUserRating").Value),
                        Released = DateTime.Parse(xe.Element(nz + "releaseDate").Value),
                        Description = xe.Element(nz + "shortDescription").Value,
                        Publisher = xe.Element(nz + "publisher").Element(nz + "name").Value
                    });
                }
                catch (Exception) { }
            }
            dgResult.ItemsSource = entries;
        }

        private void dgResult_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            tbDetails.Text = string.Empty;
            btnDownload.IsEnabled = false;
            if (dgResult.SelectedItem != null)
            {
                MarketplaceEntry me = dgResult.SelectedItem as MarketplaceEntry;
                btnDownload.IsEnabled = true;
                tbDetails.Text = string.Format("Released: {0}\n{1}", me.ReleasedString, me.Description);
            }
            btnOpen.IsEnabled = btnDownload.IsEnabled;
        }

        private void dgResult_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            downloadApp();
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {   // download xap
            downloadApp();
        }

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {   // open xap
            downloadApp(System.IO.Path.GetTempFileName() + ".xap", true);
        }
    }
    public class MarketplaceEntry
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public string Category { get; set; }
        public string Version { get; set; }
        public float Rating { get; set; }
        public DateTime Released { get; set; }
        public string ReleasedString { get { return Released.ToShortDateString(); } }
        public string Description { get; set; }
        public string Publisher { get; set; }
    }
}
