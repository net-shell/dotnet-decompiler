using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using ICSharpCode.SharpZipLib.Zip;

namespace XapEditor
{
    public class Xap
    {
        public string Source;
        public string Root { get { return string.Format("{0}dotnet-rain\\{1}", Path.GetTempPath(), Path.GetFileNameWithoutExtension(Source)); } }
        public List<XapEntry> List = new List<XapEntry>();
        public Dictionary<string, string> Meta = new Dictionary<string, string>();
        public Xap(string file)
        {
            Source = file;
        }

        public void Open()
        {
            if(File.Exists(Source))
            using (ZipInputStream s = new ZipInputStream(File.OpenRead(Source)))
            {
                ZipEntry theEntry;
                if (Directory.Exists(Root)) Directory.Delete(Root, true);
                while ((theEntry = s.GetNextEntry()) != null)
                {
                    string directoryName = Root;
                    string ld = Path.GetDirectoryName(theEntry.Name);
                    if(ld.Length > 0) directoryName += string.Concat("\\", ld);
                    string fileName = Path.GetFileName(theEntry.Name);
                    bool ex = Directory.Exists(directoryName);
                    if (directoryName.Length > 0 && false == ex)
                        Directory.CreateDirectory(directoryName);
                    if (fileName != String.Empty)
                    {
                        string p = string.Concat(directoryName, "\\", fileName);
                        using (FileStream streamWriter = File.Create(p))
                        {
                            //List.Add(new XapEntry(p, this));
                            int size = 2048;
                            byte[] data = new byte[2048];
                            while (true)
                            {
                                size = s.Read(data, 0, data.Length);
                                if (size > 0) streamWriter.Write(data, 0, size);
                                else break;
                            }
                        }
                    }
                }
                RefreshList();
                GetMetadata();
            }
        }
        public void RefreshList()
        {
            List = new List<XapEntry>();
            populateList(null);
        }
        void populateList(XapEntry entry)
        {
            string p = (entry == null ? Root : entry.FullPath);
            if (entry != null) entry.Children = new List<XapEntry>();
            foreach(string d in Directory.GetDirectories(p))
            {
                XapEntry e = new XapEntry(d, this, true);
                populateList(e);
                if (entry == null) List.Add(e);
                else entry.Children.Add(e);
            }
            foreach (string f in Directory.GetFiles(p))
            {
                XapEntry e = new XapEntry(f, this, false);
                if (entry == null) List.Add(e);
                else entry.Children.Add(e);
            }
        }
        public void Pack(string fp)
        {
            ICSharpCode.SharpZipLib.Zip.FastZip z = new FastZip() { CreateEmptyDirectories = true };
            z.CreateZip(fp, Root, true, "");
        }
        public string GetIcon()
        {
            if (Meta.ContainsKey("IconPath"))
                return string.Format("{0}\\{1}", Path.GetDirectoryName(List[0].FullPath), Meta["IconPath"]);
            return string.Empty;
        }
        void GetMetadata()
        {
            XmlTextReader reader = new XmlTextReader((from l in List where l.Name == "WMAppManifest.xml" select l).Single<XapEntry>().FullPath);
            while (reader.Read())
            {
                if(reader.NodeType == XmlNodeType.Element)
                switch (reader.Name)
                {
                    case "App":
                        string[] at = new string[] { "Author", "Description", "Genre", "ProductID", "Publisher", "RuntimeType", "Title", "Version" };
                        while (reader.MoveToNextAttribute()) foreach (string s in at) if (s == reader.Name) Meta.Add(s, reader.Value);
                        break;
                    case "IconPath":
                        Meta.Add("IconPath", reader.ReadElementContentAsString());
                        break;
                    case "BackgroundImageURI":
                        Meta.Add("BackgroundImageURI", reader.ReadElementContentAsString());
                        break;
                    case "DefaultTask":
                        while (reader.MoveToNextAttribute()) if ("NavigationPage" == reader.Name) Meta.Add("NavigationPage", reader.Value);
                        break;
                }
            }
            Console.ReadLine();
        }
    }
    public class XapEntry
    {
        public static string GetIconForExtension(string ext)
        {
            if (ext.StartsWith(".")) ext = ext.Substring(1);
            string i = "file";
            switch (ext.ToLower())
            {
                case "dll": i = "dll"; break;
                case "font": i = "font"; break;
                case "png":
                case "jpg":
                case "bmp":
                case "jpeg": i = "img"; break;
                case "wav":
                case "mp3":
                case "wma": i = "wav"; break;
                case "xaml": i = "xaml"; break;
                case "xml": i = "xml"; break;
                case "xnb": i = "xnb"; break;
            }
            return string.Format("/icons/icon_{0}.png", i);
        }
        string root;
        public string Name { get; set; }
        public string Extension { get; set; }
        public string FullPath { get { return root; } }
        public bool IsFolder { get; set; }
        public List<XapEntry> Children { get; set; }
        public string IconPath
        {
            get
            {
                if (IsFolder) return "/icons/icon_dir.png";
                return XapEntry.GetIconForExtension(Extension);
            }
        }
        public XapEntry(string d, Xap p, bool dir)
        {
            root = d;
            IsFolder = dir;
            if (dir)
            {
                Name = root.Substring(p.Root.Length + 1).Replace("\\", "/");
                return;
            }
            else Name = Path.GetFileName(root);
            Extension = Path.GetExtension(root).Substring(1);
        }
    }
}
