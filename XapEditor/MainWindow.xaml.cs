using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using Microsoft.Win32;
using System.Windows.Media;

namespace XapEditor
{
    public partial class MainWindow : Window
    {
        Xap xap;
        XapEntry SelectedXapItem { get { return (dgXap.SelectedItem as XapEntry); } }
        bool IsOnStartPage { get { if (filez.SelectedItem == null) return false; return ((filez.SelectedItem as TabItem).Content.GetType().Name == "Grid"); } }
        AshFile SelectedTab { get { return ToAshFile(filez.SelectedItem); } }
        string defaultTitle;

        public static MainWindow Instance;
        public MainWindow()
        {
            Instance = this;
            defaultTitle = ".NET Rain (beta 3)";
            App.LocalDependencies = new Dictionary<string, string>();
            InitializeComponent();
            Title = defaultTitle;
            this.DataContext = this;
            filez.Items.Add(new TabItem() { Header = "Start Page", Content = new XapEditor.controls.Startpage(this), Style = (App.Current.Resources["AshTabItem"] as Style) });
            LoadRecent("RecentFiles", "menu_recent");
            LoadRecent("RecentXap", "menu_recentxap");
            new AboutWindow("continue").ShowDialog();
        } 

        public void ReloadXAPExplorer()
        {
            string si = dgXap.SelectedValuePath;
            dgXap.SelectedValuePath = null;
            dgXap.SelectedValuePath = si;
        }
        public void SetStatus(string t) { SetStatus(t, false); }
        public void SetStatus(string t, bool primary)
        {
            (FindName(string.Format("status{0}", (primary ? string.Empty : "_secondary"))) as TextBlock).Text = t;
        }
        public bool OpenXap(string fp)
        {
            xap = new Xap(fp);
            xap.Open();
            dgXap.ItemsSource = xap.List;
            dAppName.Text = xap.Meta["Title"];
            try { dImage.Source = StaticBitmap.Read(xap.GetIcon()); }
            catch (Exception) { }
            dAppProp.Text = string.Format("Version {3} ({4})\rAuthor: {0}\rPublisher: {2}", xap.Meta["Author"], xap.Meta["Genre"], xap.Meta["Publisher"], xap.Meta["Version"], xap.Meta["RuntimeType"], xap.Meta["ProductID"]);
            menu_pack.IsEnabled = true;
            leftCol.MinWidth = 5;
            leftCol.Width = new GridLength(300);
            menu_close.IsEnabled = true;
            menu_savexap.IsEnabled = true;
            SaveRecent("RecentXap", xap.Source);
            LoadRecent("RecentXap", "menu_recentxap");
            return true;
        }
        public int CheckIfOpen(string fp)
        {
            for (int i = 0; i < filez.Items.Count; i++)
            {
                AshFile ashi = ToAshFile(filez.Items[i]);
                if (ashi != null) if (ashi.SourceFile == fp) return i;
            }
            return -1;
        }
        public bool CheckIfNeedsSave(int i)
        {
            AshFile ashi = ToAshFile(filez.Items[i]);
            if (ashi != null) return ashi.NeedsSave;
            return true;
        }
        public AshFile OpenFile(string fp, AshFile.EditorType viewas = AshFile.EditorType.Default, bool addToRecent = true)
        {
            if (Path.GetExtension(fp) == ".xap")
            {
                OpenXap(fp);
                return null;
            }

            int iOpen = CheckIfOpen(fp);
            if (iOpen >= 0)
            {
                if (CheckIfNeedsSave(iOpen))
                {
                    filez.SelectedIndex = iOpen;
                    return null;
                }
                else CloseFile(filez.Items[iOpen]);
            }
            AshFile af = new AshFile(this, fp, viewas);
            TabItem ti = new TabItem() { Content = af, Style = (App.Current.Resources["AshTabItem"] as Style) };

            TextBlock tb = new TextBlock();
            tb.SetBinding(TextBlock.TextProperty, new Binding("TabTitle") { Source = ti.Content, Mode = BindingMode.OneWay });
            tb.SetBinding(TextBlock.FontWeightProperty, new Binding("TabWeight") { Source = ti.Content, Mode = BindingMode.OneWay });
            ti.Header = tb;

            filez.Items.Add(ti);
            filez.SelectedIndex = filez.Items.Count - 1;
            if (addToRecent)
            {
                SaveRecent("RecentFiles", fp);
                LoadRecent("RecentFiles", "menu_recent");
            }
            return af;
        }
        public bool CloseFile() { return CloseFile(filez.SelectedItem); }
        public bool CloseFile(object item)
        {
            if (ToAshFile(item) == null)
            {
                filez.Items.Remove(item);
                return true;
            }
            bool close = true;
            if (ToAshFile(item).NeedsSave)
            {
                switch (MessageBox.Show("Save changes to file?", "Save Changes", MessageBoxButton.YesNoCancel))
                {
                    case MessageBoxResult.Yes: ToAshFile(item).Save(); break;
                    case MessageBoxResult.Cancel: close = false; break;
                }
            }
            if (close) filez.Items.Remove(item);
            return close;
        }

        // ############ PRIVATE

        void SaveRecent(string storage, string f)
        {
            if (Storage.Load(storage).Length == 0) Storage.Save(storage, f);
            else
            {
                List<string> lst = new List<string>(Storage.Load(storage).Split('|'));
                if (lst.Contains(f)) lst.RemoveAt(lst.IndexOf(f));
                lst.Add(f);
                if (lst.Count > 5) lst.RemoveAt(0);
                string s = string.Empty;
                lst.ForEach(i => s += i + '|');
                Storage.Save(storage, s.Substring(0, s.Length - 1));
            }
        }
        void LoadRecent(string storage, string menuitem)
        {
            string ss = Storage.Load(storage);
            MenuItem menu = (FindName(menuitem) as MenuItem);
            StackPanel mm = null;
            if (filez.Items.Count > 0)
            {
                mm = ((filez.Items[0] as TabItem).Content as XapEditor.controls.Startpage).FindName(string.Format("metro_{0}", menuitem)) as StackPanel;
                mm.Children.Clear();
            }
            menu.Items.Clear();
            menu.IsEnabled = false;
            if (ss.Length > 0)
            {
                List<string> lst = new List<string>(ss.Split('|'));
                if (lst.Count > 0)
                {
                    for (int a = lst.Count - 1; a >= 0; a--)
                    {
                        MenuItem mi = new MenuItem() { Header = Path.GetFileName(lst[a]), ToolTip = lst[a], Tag = lst[a] };
                        mi.Click += delegate(object se, RoutedEventArgs e) { OpenFile((se as MenuItem).Tag.ToString()); };
                        mi.IsEnabled = System.IO.File.Exists(lst[a]);
                        menu.Items.Add(mi);
                        bool isx = menuitem.Contains("xap");
                        XapEditor.controls.MetroTile mt = new controls.MetroTile() { Icon = (isx ? "box" : "file"), Text = mi.Header.ToString(), Tag = mi.Tag };
                        mt.BackgroundBrush = new SolidColorBrush(!isx ? Color.FromArgb(102, 100, 140, 255) : Color.FromArgb(102, 190, 255, 20));
                        mt.txt.FontSize = 18;
                        mt.Click += delegate(object s, RoutedEventArgs e) { OpenFile((s as XapEditor.controls.MetroTile).Tag.ToString()); };
                        if (mm != null) mm.Children.Add(mt);
                    }
                    MenuItem ci = new MenuItem()
                    {
                        Header = "Clear List",
                        Tag = menuitem,
                        Icon = new System.Windows.Controls.Image { Source = new System.Windows.Media.Imaging.BitmapImage(new Uri("icons/icon_del.png", UriKind.Relative)) }
                    };
                    ci.Click += delegate(object se, RoutedEventArgs ev)
                    {
                        Storage.Save(storage, string.Empty);
                        LoadRecent(storage, (se as MenuItem).Tag.ToString());
                    };
                    menu.Items.Add(new Separator());
                    menu.Items.Add(ci);
                    menu.IsEnabled = true;
                }
            }
            else
            {
                if (mm != null) mm.Children.Add(new TextBlock() { Text = "The list is empty", FontStyle = FontStyles.Italic, Margin = new Thickness(4, 0, 4, 0), Foreground = new SolidColorBrush(Color.FromArgb(102, 0, 0, 0)) });
            }
        }
        bool CloseAllFiles(bool onlyXapEntries)
        {
            bool ok = true;
            List<TabItem> remove = new List<TabItem>();
            foreach (TabItem item in filez.Items)
            {
                AshFile af = ToAshFile(item);
                bool sc = true;
                if (onlyXapEntries)
                {
                    sc = false;
                    if (xap != null)
                        foreach (XapEntry xe in xap.List)
                            if (xe.FullPath == af.SourceFile)
                            {
                                sc = true;
                                break;
                            }
                }
                if (af != null && sc) remove.Add(item);
            }
            foreach (TabItem item in remove)
                if (!CloseFile(item))
                {
                    ok = false;
                    break;
                }
            return ok;
        }

        AshFile ToAshFile(object tabItem)
        {
            TabItem ti = (tabItem as TabItem);
            if (ti == null) return null;
            else if (ti.Content.GetType().Name == "AshFile") return (ti.Content as AshFile);
            return null;
        }

        // ############ EVENT HANDLERS

        private void Window_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyboardDevice.Modifiers == ModifierKeys.Control)
            {
                switch (e.Key == Key.System ? e.SystemKey : e.Key)
                {
                    case Key.O: Menu_OpenFiles(null, new RoutedEventArgs()); break;
                    case Key.S: if (menu_save.IsEnabled) Menu_Save(null, new RoutedEventArgs()); break;
                    case Key.W:
                    case Key.F4: CloseFile(); break;
                    case Key.Q: Menu_Exit(null, new RoutedEventArgs()); break;
                }
            }
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e) { if (!CloseAllFiles(false)) e.Cancel = true; base.OnClosing(e); }
        private void filez_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            menu_save.IsEnabled = false;
            if (SelectedTab != null)
            {
                menu_save.IsEnabled = SelectedTab.CanSave;
                Title = string.Format("{0} - {1}", Path.GetFileName(SelectedTab.SourceFile), defaultTitle);
            }
            else
            {
                Title = defaultTitle;
                SetStatus("Ready", true);
                SetStatus(string.Empty);
            }
            menu_closefile.IsEnabled = (SelectedTab != null);

        }
        private void dgXap_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> E)
        {
            if (dgXap.SelectedItem != null)
            {
                ContextMenu cm = new ContextMenu();
                if (!(dgXap.SelectedItem as XapEntry).IsFolder)
                {
                    foreach (string s in new string[] { "Open", "Open with...", "Open as text", "Open as binary", "Swap file...", "Save as..." })
                    {
                        MenuItem mi = new MenuItem() { Header = s };
                        mi.Click += XapContextMenuClicked;
                        cm.Items.Add(mi);
                    }
                    dgXap.ContextMenu = cm;
                }
                else dgXap.ContextMenu = null;
            }
            else dgXap.ContextMenu = null;
            menu_sel.IsEnabled = (dgXap.ContextMenu != null);
        }
        private void dgXap_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e) { if (dgXap.SelectedItem != null) if (!SelectedXapItem.IsFolder) OpenFile(SelectedXapItem.FullPath); }
        private void dgXap_KeyUp(object sender, System.Windows.Input.KeyEventArgs e) { if (e.Key == System.Windows.Input.Key.Delete && dgXap.SelectedItem != null) Menu_Delete(null, new RoutedEventArgs()); }
        void XapContextMenuClicked(object s, RoutedEventArgs e)
        {
            if (dgXap.SelectedItem == null) return;
            switch ((s as MenuItem).Header.ToString())
            {
                case "Open": Menu_OpenSelection(null, new RoutedEventArgs()); break;
                case "Open with...": Menu_OpenWith(null, new RoutedEventArgs()); break;
                case "Open as text": Menu_OpenAsText(null, new RoutedEventArgs()); break;
                case "Open as binary": Menu_OpenAsBin(null, new RoutedEventArgs()); break;
                case "Swap file...": Menu_Swap(null, new RoutedEventArgs()); break;
                case "Save as...": Menu_SaveAs(null, new RoutedEventArgs()); break;
            }
        }

        #region --- CloseCommand ---
        private Utils.RelayCommand _cmdCloseCommand;
        public ICommand CloseCommand { get { if (_cmdCloseCommand == null) _cmdCloseCommand = new Utils.RelayCommand(param => this.CloseTab_Execute(param), param => this.CloseTab_CanExecute(param)); return _cmdCloseCommand; } }
        private void CloseTab_Execute(object parm) { if ((parm as TabItem) != null) CloseFile(parm); }
        private bool CloseTab_CanExecute(object parm) { if ((parm as TabItem) != null && (parm as TabItem) != filez.Items[0]) return (parm as TabItem).IsEnabled; return false; }
        #endregion

        // ##### MENU - FILE

        public void Menu_Open(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog() { Title = "Open XAP", Filter = "XAP Files|*.xap|ZIP Files|*.zip" };
            if ((bool)ofd.ShowDialog()) OpenXap(ofd.FileName);
            else return;
        }
        public void Menu_Close(object sender, RoutedEventArgs e)
        {
            xap = null;
            dgXap.ItemsSource = null;
            dAppName.Text = string.Empty;
            dImage.Source = null;
            dAppProp.Text = string.Empty;
            menu_pack.IsEnabled = false;
            leftCol.MinWidth = 0;
            leftCol.Width = new GridLength(0);
            menu_close.IsEnabled = false;
            menu_savexap.IsEnabled = true;
            CloseAllFiles(true);
        }
        public void Menu_OpenFiles(object sender, RoutedEventArgs e)
        {
            string filter = string.Empty;
            OpenFileDialog ofd = new OpenFileDialog() { Title = "Open", Filter = "All Files|*.*", Multiselect = true };
            if ((bool)ofd.ShowDialog()) foreach (string fn in ofd.FileNames) OpenFile(fn);
        }
        public void Menu_Save(object sender, RoutedEventArgs e)
        {
            SelectedTab.Save();
        }
        public void Menu_CloseFile(object sender, RoutedEventArgs e)
        {
            CloseFile();
            menu_save.IsEnabled = false;
            menu_closefile.IsEnabled = false;
        }
        public void Menu_Exit(object sender, RoutedEventArgs e)
        {
            App.Current.Shutdown();
        }

        // ##### MENU - XAP

        public void Menu_Repack(object sender, RoutedEventArgs e)
        {
            SaveFileDialog sfd = new SaveFileDialog() { Title = "Repack XAP", Filter = "XAP File|*.xap|ZIP File|*.zip", FileName = System.IO.Path.GetFileName(xap.Source) };
            if ((bool)sfd.ShowDialog()) xap.Pack(sfd.FileName);
        }
        public void Menu_AddFiles(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog() { Title = "Swap file", Multiselect = true, Filter = "Images|*.png;*.jpg;*.bmp|Assembies|*.dll|Fonts|*.font|Sounds|*.wav;*.wma|XML|*.xml|XNB|*.xnb|All files|*.*" };
            if ((bool)ofd.ShowDialog()) foreach (string fn in ofd.FileNames) System.IO.File.Copy(fn, Path.Combine(xap.Root, Path.GetFileName(fn)), true);
            xap.RefreshList();
            dgXap.ItemsSource = xap.List;
            dgXap.Items.Refresh();
        }
        public void Menu_AddFolder(object sender, RoutedEventArgs e)
        {
        }
        private void Menu_Deploy(object sender, RoutedEventArgs e)
        {
            filez.Items.Add(new TabItem() { Header = "WP7 App Deployment", Content = new XapEditor.controls.XapDeployer(xap.Source), Style = (App.Current.Resources["AshTabItem"] as Style) });
            filez.SelectedIndex = filez.Items.Count - 1;
        }

        // ##### MENU - SELECTION

        public void Menu_SaveAs(object sender, RoutedEventArgs e)
        {
            SaveFileDialog sfd = new SaveFileDialog() { Title = "Save as", FileName = System.IO.Path.GetFileName(SelectedXapItem.FullPath) };
            if ((bool)sfd.ShowDialog()) System.IO.File.Copy(SelectedXapItem.FullPath, sfd.FileName);
        }
        public void Menu_Swap(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog() { Title = "Swap file", Filter = string.Format(".{0} file|*.{0}", SelectedXapItem.Extension) };
            if ((bool)ofd.ShowDialog())
            {
                string i = dgXap.SelectedValuePath;
                dgXap.SelectedValuePath = null;
                System.IO.File.Copy(ofd.FileName, SelectedXapItem.FullPath, true);
                dgXap.SelectedValuePath = i;
            }
        }
        public void Menu_Edit(object sender, RoutedEventArgs e)
        {
            OpenFile(SelectedXapItem.FullPath);
        }
        public void Menu_OpenAsBin(object sender, RoutedEventArgs e)
        {
            OpenFile(SelectedXapItem.FullPath, AshFile.EditorType.Binary);
        }
        public void Menu_OpenAsText(object sender, RoutedEventArgs e)
        {
            OpenFile(SelectedXapItem.FullPath, AshFile.EditorType.Text);
        }
        public void Menu_OpenSelection(object sender, RoutedEventArgs e)
        {
            Process.Start(new ProcessStartInfo(SelectedXapItem.FullPath));
        }
        public void Menu_OpenWith(object sender, RoutedEventArgs e)
        {
            var args = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "shell32.dll");
            Process.Start("rundll32.exe", string.Concat(args, ",OpenAs_RunDLL ", SelectedXapItem.FullPath));
        }
        public void Menu_Delete(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Are you sure you want to delete " + Path.GetFileNameWithoutExtension((dgXap.SelectedItem as XapEntry).FullPath) + " from the XAP?", "Delete file from XAP", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                xap.List.Remove(dgXap.SelectedItem as XapEntry);
                dgXap.Items.Refresh();
            }
        }

        // ##### MENU - TOOLS

        public void Menu_GAC(object sender, RoutedEventArgs e)
        {
            filez.Items.Add(new TabItem() { Header = "GAC", Content = new GAC(), Style = (App.Current.Resources["AshTabItem"] as Style) });
            filez.SelectedIndex = filez.Items.Count - 1;
        }
        private void Menu_Marketplace(object sender, RoutedEventArgs e)
        {
            filez.Items.Add(new TabItem() { Header = "WP7 Marketplace", Content = new XapEditor.controls.Marketplace(this), Style = (App.Current.Resources["AshTabItem"] as Style) });
            filez.SelectedIndex = filez.Items.Count - 1;
        }
        private void Menu_XapDeployer(object sender, RoutedEventArgs e)
        {
            filez.Items.Add(new TabItem() { Header = "WP7 App Deployment", Content = new XapEditor.controls.XapDeployer(), Style = (App.Current.Resources["AshTabItem"] as Style) });
            filez.SelectedIndex = filez.Items.Count - 1;
        }

        // ##### MENU - HELP

        public void Menu_Online(object sender, RoutedEventArgs e)
        {
            Process.Start(new ProcessStartInfo(@"http://ash.kvodae.com/apps/rain/"));
        }
        public void Menu_Donate(object sender, RoutedEventArgs e)
        {
            Process.Start(new ProcessStartInfo(@"http://forum.xda-developers.com/donatetome.php?u=4274526"));
        }
        public void Menu_About(object sender, RoutedEventArgs e)
        {
            new AboutWindow().Show();
        }

    }
}