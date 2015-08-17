using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Mono.Cecil;
using System.IO;
using System.Collections;
using System.Resources;

namespace XapEditor.decompiler
{
    class ResourceDecompiler
    {
        EmbeddedResource resource;
        public Dictionary<string, string> Entries = new Dictionary<string,string>();
        public ResourceDecompiler(EmbeddedResource er)
        {
            resource = er;
            Decompile();
        }
        void Decompile()
        {
            Stream s = resource.GetResourceStream();
            s.Position = 0;
            if (resource.Name.EndsWith(".g.resources", StringComparison.OrdinalIgnoreCase))
            {
                IEnumerable<DictionaryEntry> rs = null;
                try { rs = new ResourceSet(s).Cast<DictionaryEntry>(); }
                catch (ArgumentException) { }
                if (rs != null && rs.All(e => e.Value is Stream))
                {
                    foreach (var pair in rs)
                    {
                        Stream entryStream = (Stream)pair.Value;
                        byte[] d = new byte[entryStream.Length];
                        entryStream.Position = 0;
                        if (pair.Key.ToString().EndsWith(".baml", StringComparison.OrdinalIgnoreCase))
                        {
                            MemoryStream ms = new MemoryStream();
                            entryStream.CopyTo(ms);
                            // TODO implement extension point
                            // var decompiler = Baml.BamlResourceEntryNode.CreateBamlDecompilerInAppDomain(ref bamlDecompilerAppDomain, assembly.FileName);
                            // string xaml = null;
                            // try {
                            //		xaml = decompiler.DecompileBaml(ms, assembly.FileName, new ConnectMethodDecompiler(assembly), new AssemblyResolver(assembly));
                            //	}
                            //	catch (XamlXmlWriterException) { } // ignore XAML writer exceptions
                            //	if (xaml != null) {
                            //	File.WriteAllText(Path.Combine(options.SaveAsProjectDirectory, Path.ChangeExtension(fileName, ".xaml")), xaml);
                            //	yield return Tuple.Create("Page", Path.ChangeExtension(fileName, ".xaml"));
                            //	continue;
                            //	}
                        }
                        else
                        {
                            entryStream.Read(d, 0, (int)entryStream.Length);
                        }
                        string tmp = Path.GetTempFileName();
                        File.WriteAllBytes(tmp, d);
                        Entries.Add(pair.Key.ToString(), tmp);
                    }
                }
            }
        }
    }
}
