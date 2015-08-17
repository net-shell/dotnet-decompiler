using System.Windows.Controls;
using Mono.Cecil;
using System.Text;
using System.IO;

namespace XapEditor
{
    public partial class GAC : UserControl
    {
        public GAC()
        {
            InitializeComponent();
            loadItems(2);
        }

        void loadItems(uint i)
        {
            App.LoadGAC(i);
            dg.ItemsSource = App.GAC;
        }
        private void TextBox_TextChanged(object sender, TextChangedEventArgs e) { dg.Items.Filter = delegate(object o) { if ((sender as TextBox).Text.Length == 0) return true; return (o as Mono.Cecil.AssemblyNameReference).Name.Contains((sender as TextBox).Text); }; }
        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e) { if ((sender as ComboBox).SelectedIndex >= 0 && dg != null) loadItems(uint.Parse(((sender as ComboBox).SelectedItem as ComboBoxItem).Tag.ToString())); }
        private void dg_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (dg.SelectedItem == null) return;
            string fp = FindAssemblyInNetGac(dg.SelectedItem as AssemblyNameReference);
            if (fp != null && File.Exists(fp)) MainWindow.Instance.OpenFile(fp);
            else System.Windows.MessageBox.Show("Could not locate this entry's file path");
        }

        // *************************************

        static readonly string[] gac_paths = { Fusion.GetGacPath(false), Fusion.GetGacPath(true) };
        static readonly string[] gacs = { "GAC_MSIL", "GAC_32", "GAC" };
        static readonly string[] prefixes = { string.Empty, "v4.0_" };

        public static string FindAssemblyInNetGac(AssemblyNameReference reference)
        {
            if (reference.PublicKeyToken == null) return null;
            for (int i = 0; i < 2; i++)
            {
                for (int j = 0; j < gacs.Length; j++)
                {
                    var gac = Path.Combine(gac_paths[i], gacs[j]);
                    var file = GetAssemblyFile(reference, prefixes[i], gac);
                    if (File.Exists(file)) return file;
                }
            }
            return null;
        }

        static string GetAssemblyFile(AssemblyNameReference reference, string prefix, string gac)
        {
            var gac_folder = new StringBuilder().Append(prefix).Append(reference.Version).Append("__");
            for (int i = 0; i < reference.PublicKeyToken.Length; i++)
                gac_folder.Append(reference.PublicKeyToken[i].ToString("x2"));
            return Path.Combine(Path.Combine(Path.Combine(gac, reference.Name), gac_folder.ToString()), reference.Name + ".dll");
        }
    }
}
