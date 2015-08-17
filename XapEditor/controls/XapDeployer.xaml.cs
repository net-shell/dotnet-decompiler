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
using Microsoft.SmartDevice.Connectivity;
using Microsoft.Win32;
using System.IO;

namespace XapEditor.controls
{
    public partial class XapDeployer : UserControl
    {
        DatastoreManager dsmgrObj = new DatastoreManager(1033);
        Platform WP7SDK;
        Device WP7Device;
        string xapIcon = "";
        string xapGuid = "";

        public XapDeployer(string pathToXap="")
        {
            InitializeComponent();
            foreach(Platform p in dsmgrObj.GetPlatforms()) cbPlatforms.Items.Add(p.Name);
            if(cbPlatforms.Items.Count > 0) cbPlatforms.SelectedIndex = 0;
            if (File.Exists(pathToXap))
            {
                tbXapPath.Text = pathToXap;
                loadXapFile();
            }
        }

        void loadXapFile()
        {
            btnDeploy.IsEnabled = false;
            if (File.Exists(tbXapPath.Text))
            {
                Xap x = new Xap(tbXapPath.Text);
                x.Open();
                xapIcon = (from xx in x.List where xx.Name == x.Meta["IconPath"] select xx).Single<XapEntry>().FullPath;
                xapGuid = x.Meta["ProductID"];
                tbInfo.Inlines.Clear();
                foreach (KeyValuePair<string, string> kv in x.Meta)
                {
                    Run r = new Run(string.Format("{0}: ", kv.Key));
                    r.FontWeight = FontWeights.Bold;
                    tbInfo.Inlines.Add(r);
                    tbInfo.Inlines.Add(new LineBreak());
                    tbInfo.Inlines.Add(new Run(kv.Value));
                    tbInfo.Inlines.Add(new LineBreak());
                    tbInfo.Inlines.Add(new LineBreak());
                }
                btnDeploy.IsEnabled = true;
            }
            else MessageBox.Show("Invalid file path");
        }

        public void Deploy(string pathXap, string pathIco)
        {
            try
            {
                WP7Device.Connect();
                Guid appID = new Guid(xapGuid);
                RemoteApplication app;
                if (WP7Device.IsApplicationInstalled(appID))
                {
                    app = WP7Device.GetApplication(appID);
                    app.Uninstall();
                }
                app = WP7Device.InstallApplication(appID, appID, "NormalApp", pathIco, pathXap);
                app.Launch();
                MessageBox.Show("The application has been successfully deployed.");
            }
            catch (Exception e) { MessageBox.Show(e.Message); }
        }

        private void cbPlatforms_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cbPlatforms.SelectedItem != null)
            {
                WP7SDK = dsmgrObj.GetPlatforms().Single(p => p.Name == cbPlatforms.SelectedItem.ToString());
                cbDevices.Items.Clear();
                foreach (Device d in WP7SDK.GetDevices()) cbDevices.Items.Add(d.Name);
                if (cbDevices.Items.Count > 0) cbDevices.SelectedIndex = 0;
            }
        }
        private void cbDevices_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cbDevices.SelectedItem != null)
                WP7Device = WP7SDK.GetDevices().Single(d => d.Name == cbDevices.SelectedItem.ToString());
        }
        private void Browse_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "XAP Files|*.xap;*.zip";
            if ((bool)ofd.ShowDialog())
            {
                tbXapPath.Text = ofd.FileName;
                loadXapFile();
            }
        }
        private void Deploy_Click(object sender, RoutedEventArgs e) { Deploy(tbXapPath.Text, xapIcon); }
    }
}
