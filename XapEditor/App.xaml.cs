using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;

namespace XapEditor
{
    public partial class App : Application
    {
        public static List<Mono.Cecil.AssemblyNameReference> GAC;
        public static Dictionary<string, string> LocalDependencies;
        public static void LoadGAC(uint dwFlags)
        {
            GAC = new List<Mono.Cecil.AssemblyNameReference>();
            foreach (Mono.Cecil.AssemblyNameReference anr in _loadGAC(dwFlags))
            {
                try { GAC.Add(anr); }
                catch (Exception) { }
            }
        }
        static IEnumerable<Mono.Cecil.AssemblyNameReference> _loadGAC(uint dwFlags)
        {

            IApplicationContext applicationContext = null;
            IAssemblyEnum assemblyEnum = null;
            IAssemblyName assemblyName = null;
            Fusion.CreateAssemblyEnum(out assemblyEnum, null, null, dwFlags, 0);
            while (assemblyEnum.GetNextAssembly(out applicationContext, out assemblyName, 0) == 0)
            {
                uint nChars = 0;
                assemblyName.GetDisplayName(null, ref nChars, 0);
                StringBuilder name = new StringBuilder((int)nChars);
                assemblyName.GetDisplayName(name, ref nChars, 0);
                Mono.Cecil.AssemblyNameReference r = null;
                try { r = Mono.Cecil.AssemblyNameReference.Parse(name.ToString()); }
                catch (ArgumentException) { }
                catch (FormatException) { }
                catch (OverflowException) { }
                if (r != null) yield return r;
            }
        }
    }
}
