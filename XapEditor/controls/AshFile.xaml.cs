using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.IO.IsolatedStorage;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media.Imaging;

namespace XapEditor
{
    public partial class AshFile : UserControl, INotifyPropertyChanged
    {
        string sourcefile;
        bool needsSave;
        MainWindow pwner;
        NetDasm nd;
        EditorType et = EditorType.Default;

        public event FileSavedHandler FileSaved;
        public event FileLoadedHandler FileLoaded;
        public event NeedsSaveChangedHandler NeedsSaveChanged;
        public delegate void FileSavedHandler(AshFile af);
        public delegate void FileLoadedHandler(AshFile af);
        public delegate void NeedsSaveChangedHandler(bool newState);
        protected void OnFileSaved() { if (FileSaved != null) FileSaved(this); }
        protected void OnFileLoaded() { if (FileLoaded != null) FileLoaded(this); }
        protected void OnNeedsSaveChanged() { if (NeedsSaveChanged != null) NeedsSaveChanged(this.NeedsSave); }

        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged(String info)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(info));
            }
        }
        public string TabTitle { get { return string.Format("{0} {1}", Path.GetFileName(sourcefile), (NeedsSave ? "*" : string.Empty)); } }
        public FontWeight TabWeight { get { return (NeedsSave ? FontWeights.Bold : FontWeights.Normal); } }
        public bool NeedsSave
        {
            get
            {
                return this.needsSave;
            }
            set
            {
                if (value != this.needsSave)
                {
                    this.needsSave = value;
                    NotifyPropertyChanged("NeedsSave");
                    NotifyPropertyChanged("TabTitle");
                    NotifyPropertyChanged("TabWeight");
                    OnNeedsSaveChanged();
                }
            }
        }
        public bool CanSave = false;
        public string SourceFile { get { return sourcefile; } }
        public enum EditorType { None, Default, Text, Binary, Assembly, Image, Compiler, ILEditor, Resource }
        string decompilationResult;

        public AshFile(MainWindow own, string f, EditorType etype)
        {
            et = etype;
            pwner = own;
            sourcefile = f;
            InitializeComponent();
            loadFile();
            tbEdit.TextChanged += delegate(object s, EventArgs e) { NeedsSave = true; };
            NeedsSave = false;
        }

        public void Save()
        {
            if (NeedsSave)
            {
                switch (et)
                {
                    case EditorType.Text:
                        System.IO.File.WriteAllText(sourcefile, tbEdit.Text, Encoding.UTF8);
                        pwner.ReloadXAPExplorer();
                        break;
                    case EditorType.Assembly:
                        nd.SaveAsm(sourcefile);
                        break;
                }
                NeedsSave = false;
                OnFileSaved();
            }
        }

        void loadFile()
        {
            bool eSave = false;
            pwner.SetStatus(string.Empty);
            img.Source = null;
            txtEditor.Visibility = Visibility.Collapsed;
            binEditor.Visibility = Visibility.Collapsed;
            imgPane.Visibility = Visibility.Collapsed;
            disasm.Visibility = Visibility.Collapsed;
            compi.Visibility = Visibility.Collapsed;
            iledit.Visibility = Visibility.Collapsed;
            resedit.Visibility = Visibility.Collapsed;
            nd = null;
            List<string> viewAsText = new List<string>() { "xaml", "xml" };
            List<string> viewAsImage = new List<string>() { "png", "bmp", "jpg", "ico" };
            long fl = new System.IO.FileInfo(sourcefile).Length;
            pwner.SetStatus(sourcefile, true);
            string stat = string.Format("{0:0} {1}", (fl > 1024 ? (fl / 1024) : fl), (fl > 1024 ? "Kb" : "bytes"));
            if (et == EditorType.Default)
            {
                string e = Path.GetExtension(sourcefile).Substring(1);
                if (viewAsText.Contains(e)) et = EditorType.Text;
                else if (viewAsImage.Contains(e)) et = EditorType.Image;
                else if (e == "dll") et = EditorType.Assembly;
                else et = EditorType.Binary;
            }
            switch (et)
            {
                case EditorType.Text:
                    eSave = true;
                    try
                    {
                        tbEdit.Text = System.IO.File.ReadAllText(sourcefile, Encoding.UTF8);
                    }
                    catch (Exception e) { MessageBox.Show(e.Message); }
                    txtEditor.Visibility = Visibility.Visible;
                    break;
                case EditorType.Compiler:
                    eSave = true;
                    compilerCode.Text = System.IO.File.ReadAllText(sourcefile, Encoding.UTF8);
                    compi.Visibility = Visibility.Visible;
                    break;
                case EditorType.Image:
                    BitmapImage bi = StaticBitmap.Read(sourcefile);
                    img.Source = bi;
                    stat = string.Format("{0:0}x{1:0}px | {2}", bi.Width, bi.Height, stat);
                    imgPane.Visibility = Visibility.Visible;
                    break;
                case EditorType.Assembly:
                    nd = new NetDasm(this);
                    nd.LoadAsm(sourcefile);
                    disasm.Visibility = Visibility.Visible;
                    Disassemble();
                    eSave = true;
                    break;
                case EditorType.Binary:
                    Be.Windows.Forms.HexBox hb = new Be.Windows.Forms.HexBox() { ByteProvider = new Be.Windows.Forms.FileByteProvider(sourcefile), ByteCharConverter = new Be.Windows.Forms.DefaultByteCharConverter(), BytesPerLine = 16, UseFixedBytesPerLine = true, StringViewVisible = true, VScrollBarVisible = true };
                    hb.TextChanged += delegate(object sender, EventArgs ev) { NeedsSave = true; };
                    binHost.Child = hb;
                    binEditor.Visibility = Visibility.Visible;
                    break;
                case EditorType.Resource:
                    resedit.Visibility = Visibility.Visible;
                    break;
            }
            pwner.SetStatus(stat);
            CanSave = eSave;
            pwner.menu_save.IsEnabled = CanSave;
            OnFileLoaded();
        }
        public void loadasm(string fn)
        {
            nd = new NetDasm(this);
            nd.LoadAsm(fn);
        }
        void Disassemble()
        {
            string fp = Path.Combine(Path.GetTempPath(), Path.GetFileName(sourcefile));
            File.WriteAllText(fp, nd.Decompile(nd.GetAssembly()).Text, Encoding.UTF8);
            decompilationResult = fp;
            OpenDecompiledCode();
        }
        void OpenDecompiledCode(string segment = null)
        {
            AshFile af = new AshFile(pwner, decompilationResult, EditorType.Compiler);
            af.Tag = new CodeEditorParameters() { File = SourceFile, References = nd.ResolvedReferences.ToArray() };
            disasmFile.Child = af;
            af.NeedsSaveChanged += delegate(bool ns) { NeedsSave = ns; };
            this.FileSaved += delegate(AshFile f)
            {
                NetDasm.CompileFromCs(af.compilerCode.Text, SourceFile, (af.Tag as CodeEditorParameters).References);
            };
        }

        // -----------

        private void ImageStretch_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            int i = (sender as ComboBox).SelectedIndex;
            img.Stretch = (i == 0 ? System.Windows.Media.Stretch.None : (i == 1 ? System.Windows.Media.Stretch.Fill : System.Windows.Media.Stretch.Uniform));
        }

        int codeSearchOffset = 0;
        public void LocateInCode(string g)
        {
            AshFile af = (disasmFile.Child as AshFile);
            if (af == null) af = this;
            if (af == null) return;
            if (codeSearchOffset < 0) codeSearchOffset = 0;
            if (g.Length > 0 && af.compilerCode.Text.Contains(g))
            {
                codeSearchOffset = af.compilerCode.Text.IndexOf(g, (codeSearchOffset > 0 ? (codeSearchOffset + 1) : 0));
                af.compilerCode.Select(codeSearchOffset, g.Length);
                int l = -1;
                foreach (string s in af.compilerCode.Text.Split('\r'))
                {
                    l++;
                    if (s.Contains(g)) break;
                }
                if (l >= 0) af.compilerCode.ScrollToLine(l);
            }
        }

        private void disasmTree_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if ((sender as TreeView).SelectedItem != null)
            {
                // scroll to member logic
                TreeViewItem si = ((sender as TreeView).SelectedItem as TreeViewItem);
                List<string> tp = new List<string> { "TypeDefinition", "MethodDefinition", "PropertyDefinition", "FieldDefinition", "EventDefinition" };
                if (tp.Contains(si.Tag.GetType().Name))
                {
                    AshFile af = (disasmFile.Child as AshFile);
                    string g = string.Empty;
                    string[] td = nd.Decompile(si.Tag).Text.Split('\r');
                    if (si.Tag.GetType().Name == tp[0])
                    {
                        g = td[2].Trim();
                    }
                    else
                    {
                        if (!td[0].Contains("[CompilerGenerated]"))
                        {
                            g = td[0].Trim();
                            if (g.Contains(";")) g = g.Substring(0, g.IndexOf(';'));
                        }
                        else MessageBox.Show("This member is compiler generated.");
                    }
                    LocateInCode(g);
                }
                else if (si.Tag.GetType().Name == "EmbeddedResource" && si.Items.Count == 0)
                {
                    //if (NeedsSave && MessageBoxResult.Yes == MessageBox.Show("Save current changes?", "Save changes", MessageBoxButton.YesNo)) Save();
                    XapEditor.decompiler.ResourceDecompiler rdec = new decompiler.ResourceDecompiler(si.Tag as Mono.Cecil.EmbeddedResource);
                    string tmp = Path.GetTempFileName();
                    //AshFile af = new AshFile(this.pwner, tmp, EditorType.Resource);
                    foreach (KeyValuePair<string, string> kv in rdec.Entries)
                    {
                        //af.reEntries.Items.Add(new ComboBoxItem() { Content = kv.Key, Tag = kv.Value });
                        si.Items.Add(nd.GetTreeItem(XapEntry.GetIconForExtension(Path.GetExtension(kv.Key)), kv.Key, kv.Key, kv.Key, string.Format("{0} bytes", new FileInfo(kv.Value).Length)));
                        bool iF = kv.Key.Replace('\\', '/').Contains("/");
                    }
                    //disasmFile.Child = af;
                }
            }
        }

        Task<bool> compilerTask;

        private void compilerCode_TextChanged(object sender, EventArgs e)
        {
            CompileBgAsync(compilerCode.Text);
        }

        private async Task CompileBgAsync(string code)
        {
            if (compilerTask != null)
                compilerTask = null;
            compilerTask = new Task<bool>(CompileBg(code));
            compilerTask.Start();
            NeedsSave = await compilerTask;
        }
        private bool CompileBg(string code, CodeEditorParameters cedp = null)
        {
            CodeEditorParameters cep = Tag as CodeEditorParameters;
            System.CodeDom.Compiler.CompilerResults cr = NetDasm.Compile(code, cep.File, cep.References);
            compilerErrors.ItemsSource = NetDasm.GetCompilerErrors(cr);
            compilerErrorsBorder.Background = new System.Windows.Media.SolidColorBrush(compilerErrors.Items.Count > 0 ? System.Windows.Media.Colors.OrangeRed : System.Windows.Media.Colors.LightGray);
            return (compilerErrors.ItemsSource == null);
        }

        private void compilerErrors_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (compilerErrors.SelectedItem != null)
            {
                System.CodeDom.Compiler.CompilerError er = (compilerErrors.SelectedItem as System.CodeDom.Compiler.CompilerError);
                string t = compilerCode.Text.Split(new char[] { '\r' })[er.Line - 1];
                compilerCode.ScrollToLine(er.Line);
                compilerCode.CaretOffset = (compilerCode.Text.IndexOf(t) + er.Column);
                compilerCode.Focus();
            }
        }

        private void reEntries_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBoxItem cbi = ((sender as ComboBox).SelectedItem as ComboBoxItem);
            if (cbi != null)
            {
                EditorType et = EditorType.Binary;
                switch (Path.GetExtension(cbi.Content as string))
                {
                    case ".jpg":
                    case ".jpeg":
                    case ".png":
                        et = EditorType.Image;
                        break;
                    case ".xaml":
                        et = EditorType.Text;
                        break;
                }
                AshFile af = new AshFile(this.pwner, cbi.Tag as string, et);
                reContent.Child = af;
            }
            reSaveAs.IsEnabled = (cbi != null);
        }

        private void reSave_Click(object sender, RoutedEventArgs e)
        {

        }

        private void reSaveAs_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.SaveFileDialog sfd = new Microsoft.Win32.SaveFileDialog();
            ComboBoxItem cbi = (reEntries.SelectedItem as ComboBoxItem);
            sfd.FileName = Path.GetFileName(cbi.Content as string);
            if ((bool)sfd.ShowDialog())
            {
                File.Copy(cbi.Tag as string, sfd.FileName);
            }
        }

        private void dtbDsm_Click(object sender, RoutedEventArgs e)
        {

        }

        private void dtbAsm_Click(object sender, RoutedEventArgs e)
        {

        }

        private void codeSearch_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (codeSearch.Text.Length == 0)
            {
                compilerCode.Select(0, 0);
                return;
            }
            if (e.Key != System.Windows.Input.Key.Enter && e.Key != System.Windows.Input.Key.F3) codeSearchOffset = 0;
            LocateInCode(codeSearch.Text);
        }

        private void compilerCode_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (System.Windows.Input.Keyboard.Modifiers == System.Windows.Input.ModifierKeys.Control && e.Key == System.Windows.Input.Key.F)
            {
                codeSearchPane.Visibility = System.Windows.Visibility.Visible;
                compilerCode.Margin = new Thickness(0, 0, 0, 28);
                codeSearch.Focus();
            }
        }

        private void codeSearchClose_MouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            codeSearchPane.Visibility = System.Windows.Visibility.Collapsed;
            compilerCode.Margin = new Thickness(0, 0, 0, 2);
        }
    }
    public class CodeEditorParameters
    {
        public string File { get; set; }
        public string[] References { get; set; }
    }
    public class StaticBitmap
    {
        public static BitmapImage Read(string f)
        {
            BitmapImage bi = new BitmapImage();
            bi.BeginInit();

            Stream getmsS;
            MemoryStream getmsM;
            getmsS = new FileStream(f, FileMode.Open, FileAccess.Read);
            byte[] buff = new byte[new FileInfo(f).Length];
            getmsS.Read(buff, 0, buff.Length);
            getmsM = new System.IO.MemoryStream(buff);

            bi.StreamSource = getmsM;
            bi.CacheOption = BitmapCacheOption.OnLoad;
            bi.EndInit();

            getmsM.Close();
            getmsS.Close();

            return bi;
        }
    }
    public class Storage
    {
        public static void Save(string k, string v)
        {
            IsolatedStorageFile isoStore = IsolatedStorageFile.GetStore(IsolatedStorageScope.User | IsolatedStorageScope.Assembly, null, null);
            IsolatedStorageFileStream oStream = new IsolatedStorageFileStream(k + ".txt", FileMode.Create, isoStore);
            StreamWriter writer = new StreamWriter(oStream);
            writer.Write(v);
            writer.Close();
        }
        public static string Load(string k)
        {
            try
            {
                IsolatedStorageFile isoStore = IsolatedStorageFile.GetStore(IsolatedStorageScope.User | IsolatedStorageScope.Assembly, null, null);
                IsolatedStorageFileStream iStream = new IsolatedStorageFileStream(k + ".txt", FileMode.Open, isoStore);
                StreamReader reader = new StreamReader(iStream);
                string r = reader.ReadToEnd();
                reader.Close();
                return r;
            }
            catch (Exception) { return string.Empty; }
        }
    }
}