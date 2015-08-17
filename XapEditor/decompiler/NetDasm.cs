using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.Ast;
using ICSharpCode.ILSpy.TextView;
using Microsoft.CSharp;
using Mono.Cecil;

namespace XapEditor
{
    public class NetDasm
    {
        string source;
        AssemblyDefinition assembly;
        AshFile w;
        public enum TreeItemTypes { Module, EmbeddedResource, Type, Method, Property, Field, Event }
        public NetDasm(AshFile mw)
        {
            w = mw;
        }
        public string GetSource()
        {
            return source;
        }
        public AssemblyDefinition GetAssembly()
        {
            return assembly;
        }
        public void LoadAsm(string f)
        {
            source = f;
            var ar = new DefaultAssemblyResolver();
            ar.AddSearchDirectory(Path.GetDirectoryName(f));
            string[] searchDirs = new string[] { @"C:\Program Files\Reference Assemblies", @"C:\Program Files (x86)\Reference Assemblies" };
            foreach (string sd in searchDirs) if (Directory.Exists(sd)) ar.AddSearchDirectory(sd);
            ar.ResolveFailure += new AssemblyResolveEventHandler(ResolveFailure);
            ReaderParameters rp = new ReaderParameters() { AssemblyResolver = ar };
            assembly = AssemblyDefinition.ReadAssembly(source, rp);
            LoadAssemblyTree();
        }
        public List<string> ResolvedReferences = new List<string>();
        AssemblyDefinition ResolveFailure(object sender, AssemblyNameReference reference)
        {
            string fpath = string.Empty;
            if (App.LocalDependencies.Keys.Contains(reference.FullName))
            {
                fpath = App.LocalDependencies[reference.FullName];
            }
            else
            {
                Microsoft.Win32.OpenFileDialog ofd = new Microsoft.Win32.OpenFileDialog();
                ofd.Filter = "Assembly|" + reference.Name + ".dll";
                if ((bool)ofd.ShowDialog()) fpath = ofd.FileName;
            }
            if (File.Exists(fpath))
            {
                AssemblyDefinition ad;
                try
                {
                    ad = AssemblyDefinition.ReadAssembly(fpath);
                    ResolvedReferences.Add(fpath);
                    App.LocalDependencies.Add(reference.FullName, fpath);
                    return ad;
                }
                catch (Exception)
                {
                    MessageBox.Show("Unable to read assembly!");
                    return ResolveFailure(sender, reference);
                }
            }
            return null;
        }

        public void SaveAsm(string f)
        {
            assembly.Write(f);
            w.loadasm(f);
        }
        public TreeItemTypes? GetSelectionType()
        {
            if (w.disasmTree.SelectedItem == null) return null;
            switch (w.disasmTree.SelectedItem.GetType().Name)
            {
                case "ModuleDefinition": return TreeItemTypes.Module;
                case "EmbeddedResource": return TreeItemTypes.EmbeddedResource;
                case "TypeDefinition": return TreeItemTypes.Type;
                case "MethodDefinition": return TreeItemTypes.Method;
                case "PropertyDefinition": return TreeItemTypes.Property;
                case "FieldDefinition": return TreeItemTypes.Field;
                case "EventDefinition": return TreeItemTypes.Event;
            }
            return null;
        }
        public void LoadAssemblyTree()
        {
            w.disasmTree.ItemsSource = null;
            w.disasmTree.Items.Clear();
            foreach (ModuleDefinition module in assembly.Modules)
            {
                TreeViewItem iMod = GetTreeItem("Folder.Closed", module, module.Name, module.Name, string.Format("{0} Resources\r{1} Types\rEntry Point: {2}", module.Resources.Count, module.Types.Count, (module.EntryPoint == null ? "none" : module.EntryPoint.Name)));
                if (module.IsMain) iMod.IsExpanded = true;
                // get references
                TreeViewItem iReferences = GetTreeItem("ReferenceFolder.Closed", null, "References", string.Format("References in {0}:", module.Name), string.Format("{0} Assemblies\r{1} Modules", module.AssemblyReferences.Count, module.ModuleReferences.Count));
                foreach (AssemblyNameReference s in module.AssemblyReferences)
                {
                    iReferences.Items.Add(GetTreeItem("Assembly", s, s.Name, s.Name, s.FullName));
                }
                iMod.Items.Add(iReferences);
                // get resources
                if (module.Resources.Count > 0)
                {
                    TreeViewItem iRes = GetTreeItem("Folder.Closed", null, "Resources", null, null);
                    foreach (EmbeddedResource resource in module.Resources)
                    {
                        iRes.Items.Add(GetTreeItem("ResourceResourcesFile", resource, resource.Name, resource.Name, string.Format("{0} Bytes", resource.ResourceType)));
                    }
                    iMod.Items.Add(iRes);
                }
                // get namespaces
                List<string> namespaces = new List<string>();
                foreach (TypeDefinition type in module.Types) if (!namespaces.Contains(type.Namespace)) namespaces.Add(type.Namespace);
                foreach (string nams in namespaces)
                {
                    string ns = (nams == string.Empty ? "-" : nams);
                    int tcount = 0;
                    TreeViewItem iNamespace = GetTreeItem("Namespace", nams, ns, null, null);
                    foreach (TypeDefinition type in module.Types)
                        if (type.Namespace == nams)
                        {
                            string nf = "{0} : {1}";
                            TreeViewItem iType = GetTreeItem("Class", type, type.Name, type.FullName, string.Format("{0} Methods\r{1} Fields\r{2} Properties\r{3} Events", type.Methods.Count, type.Fields.Count, type.Properties.Count, type.Events.Count));

                            // get base type
                            if (type.BaseType != null)
                            {
                                TreeViewItem iBase = GetTreeItem("SuperTypes", null, "Base Types", null, null);
                                iBase.Items.Add(GetTreeItem("Class", type.BaseType, type.BaseType.Name, null, type.BaseType.FullName));
                                iType.Items.Add(iBase);
                            }

                            // get fields
                            foreach (FieldDefinition field in type.Fields)
                                iType.Items.Add(GetTreeItem(GetIcons("Field", field.Attributes), field, string.Format(nf, field.Name, GetTypeName(field.FieldType)), field.Name, field.Attributes.ToString()));
                            // get properties
                            foreach (PropertyDefinition prop in type.Properties)
                                iType.Items.Add(GetTreeItem("Property", prop, string.Format(nf, prop.Name, GetTypeName(prop.PropertyType)), prop.Name, string.Format("{0}\r{1} {2}", prop.Attributes, (prop.GetMethod == null ? "" : "get;"), (prop.SetMethod == null ? "" : "set;"))));
                            // get events
                            foreach (EventDefinition eve in type.Events)
                                iType.Items.Add(GetTreeItem("Event", eve, string.Format(nf, eve.Name, GetTypeName(eve.EventType)), eve.Name, eve.Attributes.ToString()));
                            // get methods
                            foreach (MethodDefinition method in type.Methods)
                                if (!method.IsSpecialName)
                                {
                                    string[] icon = (method.IsConstructor ? new string[] { "Constructor" } : GetIcons("Method", method.Attributes));
                                    iType.Items.Add(GetTreeItem(icon, method, GetMethodLabel(method), method.Name, method.Attributes.ToString()));
                                }

                            iNamespace.Items.Add(iType);
                            tcount++;
                        }
                    iNamespace.ToolTip = GetTooltip(ns, string.Format("{0} Types", tcount));
                    iMod.Items.Add(iNamespace);
                }
                w.disasmTree.Items.Add(iMod);
            }
        }
        string GetMethodLabel(MethodDefinition md)
        {
            string ps = string.Empty;
            foreach (ParameterDefinition param in md.Parameters) ps += string.Format("{0} {1}, ", GetTypeName(param.ParameterType), param.Name);
            if (ps.Length > 1) ps = ps.Substring(2);
            return string.Format("{0}({1}) : {2}", md.Name, ps, GetTypeName(md.ReturnType.GetElementType()));
        }
        string GetTypeName(TypeReference tr) { return tr.Name; }
        string[] GetIcons(string b, MethodAttributes a)
        {
            if (a.HasFlag(MethodAttributes.Private)) return GetIconsStub(b, "Private");
            return GetIconsStub(b, string.Empty);
        }
        string[] GetIcons(string b, FieldAttributes a)
        {
            if (a.HasFlag(FieldAttributes.Private)) return GetIconsStub(b, "Private");
            return GetIconsStub(b, string.Empty);
        }
        string[] GetIconsStub(string baseIcon, string overIcon)
        {
            if (overIcon != string.Empty) return new string[] { baseIcon, ("Overlay" + overIcon) };
            return new string[] { baseIcon };
        }
        public TreeViewItem GetTreeItem(string ico, object tag, string header, string ttHeader, string ttBody) { return GetTreeItem(new string[] { ico }, tag, header, ttHeader, ttBody); }
        public TreeViewItem GetTreeItem(string[] ico, object tag, string header, string ttHeader, string ttBody)
        {
            TreeViewItem ti = new TreeViewItem() { Tag = tag };
            Grid hdr = new Grid();
            Grid img = new Grid() { Width = 16, Height = 16, HorizontalAlignment = HorizontalAlignment.Left };
            foreach (string icon in ico) img.Children.Add(new Image() { Source = GetImageSource(string.Format("/icons/disasm/{0}.png", icon)) });
            hdr.Children.Add(img);
            hdr.Children.Add(new TextBlock() { Text = header, Margin = new Thickness(20, 0, 20, 0) });
            ti.Header = hdr;
            if (ttHeader != null || ttBody != null) ti.ToolTip = GetTooltip(ttHeader, ttBody);
            return ti;
        }
        StackPanel GetTooltip(string header, string data)
        {
            StackPanel sp = new StackPanel();
            if (header != null) sp.Children.Add(new TextBlock() { Text = header, FontWeight = FontWeights.Bold });
            if (data != null) sp.Children.Add(new TextBlock() { Text = data });
            return sp;
        }
        ImageSource GetImageSource(string uri) { return new BitmapImage(new Uri(uri, UriKind.Relative)); }

        public ICSharpCode.AvalonEdit.Document.TextDocument Decompile(object obj)
        {
            AvalonEditTextOutput aeto = new AvalonEditTextOutput();
            AstBuilder ast = new AstBuilder(new DecompilerContext(ModuleDefinition.CreateModule("ash", ModuleKind.NetModule)));
            switch (obj.GetType().Name)
            {
                case "AssemblyDefinition":
                    ast = new AstBuilder(new DecompilerContext((obj as AssemblyDefinition).MainModule) { Settings = new DecompilerSettings() });
                    try { ast.AddAssembly(obj as AssemblyDefinition); }
                    catch (AssemblyResolutionException e) { MessageBox.Show("Could not load assembly " + e.AssemblyReference.FullName); }
                    break;
                case "TypeDefinition":
                    ast = CreateAstBuilder((obj as TypeDefinition), true);
                    try { ast.AddType(obj as TypeDefinition); }
                    catch (AssemblyResolutionException e) { MessageBox.Show("Could not load assembly " + e.AssemblyReference.FullName); }
                    break;
                case "MethodDefinition":
                    MethodDefinition method = (obj as MethodDefinition);
                    ast = CreateAstBuilder(method.DeclaringType, true);
                    if (method.IsConstructor && !method.IsStatic && !method.DeclaringType.IsValueType)
                    {
                        foreach (var field in method.DeclaringType.Fields)
                            if (field.IsStatic == method.IsStatic)
                            {
                                try { ast.AddField(field); }
                                catch (AssemblyResolutionException e) { MessageBox.Show("Could not load assembly " + e.AssemblyReference.Name); }
                            }
                        foreach (var ctor in method.DeclaringType.Methods)
                            if (ctor.IsConstructor && ctor.IsStatic == method.IsStatic)
                            {
                                try { ast.AddMethod(ctor); }
                                catch (AssemblyResolutionException e) { MessageBox.Show("Could not load assembly " + e.AssemblyReference.Name); }
                            }
                    }
                    else
                    {
                        try { ast.AddMethod(obj as MethodDefinition); }
                        catch (AssemblyResolutionException e) { MessageBox.Show("Could not load assembly " + e.AssemblyReference.Name); }
                    }
                    break;
                case "PropertyDefinition":
                    ast = CreateAstBuilder((obj as PropertyDefinition).DeclaringType, true);
                    try { ast.AddProperty(obj as PropertyDefinition); }
                    catch (AssemblyResolutionException e) { MessageBox.Show("Could not load assembly " + e.AssemblyReference.Name); }
                    break;
                case "FieldDefinition":
                    ast = CreateAstBuilder((obj as FieldDefinition).DeclaringType, true);
                    try { ast.AddField(obj as FieldDefinition); }
                    catch (AssemblyResolutionException e) { MessageBox.Show("Could not load assembly " + e.AssemblyReference.Name); }
                    break;
                case "EventDefinition":
                    ast = CreateAstBuilder((obj as EventDefinition).DeclaringType, true);
                    try { ast.AddEvent(obj as EventDefinition); }
                    catch (AssemblyResolutionException e) { MessageBox.Show("Could not load assembly " + e.AssemblyReference.Name); }
                    break;
                default:
                    return new ICSharpCode.AvalonEdit.Document.TextDocument();
            }
            try { ast.GenerateCode(aeto); }
            catch (AssemblyResolutionException e) { MessageBox.Show("Could not load assembly upon code generation:\r" + e.AssemblyReference.FullName); }
            return aeto.GetDocument();
        }

        AstBuilder CreateAstBuilder(TypeDefinition currentType, bool isSingleMember)
        {
            ModuleDefinition currentModule = currentType.Module;
            DecompilerSettings settings = new DecompilerSettings();
            if (isSingleMember)
            {
                settings = settings.Clone();
                settings.UsingDeclarations = false;
            }
            return new AstBuilder(new DecompilerContext(currentModule) { CurrentType = currentType, Settings = settings });
        }

        public static TypeDefinition DeepCloneType(TypeDefinition t)
        {
            TypeDefinition td = new TypeDefinition(t.Namespace, t.Name, t.Attributes);
            return td;
        }

        // Compilation

        public static List<CompilerError> GetCompilerErrors(CompilerResults cr)
        {
            if (cr.Errors.HasErrors)
            {
                List<CompilerError> cer = new List<CompilerError>();
                foreach (CompilerError c in cr.Errors) cer.Add(c);
                return cer;
            }
            return null;
        }
        public static CompilerResults Compile(string cs, string f, string[] references)
        {
            return DynamicCompilation(cs, false, false, Path.GetTempFileName(), true, references);
        }

        public static bool CompileFromCs(string cs, string f, string[] references)
        {
            string tf = Path.GetTempFileName();
            CompilerResults cr = DynamicCompilation(cs, false, true, tf, true, references);
            if (cr.Errors.HasErrors)
            {
                System.Windows.MessageBox.Show("There were compilation errors. Please review your code.");
                return false;
            }
            File.Copy(tf, f, true);
            File.Delete(tf);
            return true;
        }
        public static CompilerResults DynamicCompilation(string srccode, bool fromFile, bool toFile, string toFilePath, bool isCSharp, string[] referencedAssemblies = null)
        {
            System.CodeDom.Compiler.CodeDomProvider prov;
            if (isCSharp == true) prov = new CSharpCodeProvider();
            else prov = new Microsoft.VisualBasic.VBCodeProvider();
            CompilerParameters cp = new CompilerParameters();
            if (!toFile)
            {
                cp.GenerateInMemory = true;
                cp.GenerateExecutable = false;
            }
            else
            {
                cp.GenerateExecutable = false;
                cp.GenerateInMemory = false;
                cp.OutputAssembly = toFilePath;
            }
            cp.CompilerOptions = "/target:library";
            cp.ReferencedAssemblies.Add("mscorlib.dll");
            cp.ReferencedAssemblies.Add("System.dll");
            cp.ReferencedAssemblies.Add("System.Core.dll");
            if (referencedAssemblies != null) cp.ReferencedAssemblies.AddRange(referencedAssemblies);
            CompilerResults res;
            if (fromFile == true) res = prov.CompileAssemblyFromFile(cp, srccode);
            else res = prov.CompileAssemblyFromSource(cp, srccode);
            return res;
        }
    }
}
