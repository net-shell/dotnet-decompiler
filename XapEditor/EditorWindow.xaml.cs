using System.Windows;
using Mono.Cecil;
using System;
using System.Collections.Generic;
using Mono.Cecil.Cil;

namespace XapEditor
{
    public partial class EditorWindow : Window
    {
        TypeDefinition t;
        System.Windows.Threading.DispatcherTimer dt;
        public EditorWindow(TypeDefinition typedef, object f)
        {
            t = (typedef as TypeDefinition);
            //dt = new System.Windows.Threading.DispatcherTimer() { Interval = TimeSpan.FromMilliseconds(500) };
            InitializeComponent();
            /*dt.Tick += delegate(object s, EventArgs e)
            {
                tbStatus.Text = DateTime.Now.ToLongTimeString();
                FromCSharp();
                dt.Stop();
            };
            LoadMembers();
            if (f != null) FocusMember(f);*/
        }
        /*void LoadMembers()
        {
            Title = t.FullName;
            cbOpCode.Items.Clear();
            foreach (System.Reflection.MemberInfo m in typeof(OpCodes).GetMembers()) if (m.MemberType.ToString() == "Field") cbOpCode.Items.Add(m.Name.ToLower().Replace("_", "."));
            ToCSharp();
        }
        void FocusMember(object mem)
        {
            string find = string.Empty;
            if (mem.Type == dgMember.MemberType.Property) find = string.Format("{0} {1}", getCSTypeName((mem.Tag as PropertyDefinition).PropertyType.Name), (mem.Tag as PropertyDefinition).Name);
            else if (mem.Type == dgMember.MemberType.Method) find = string.Format("{0} {1}", getCSTypeName((mem.Tag as MethodDefinition).ReturnType.ReturnType.Name), (mem.Tag as MethodDefinition).Name);
            int io = codeEditor.Text.IndexOf(find);
            codeEditor.ScrollToLine(mem.Line + 1);
            if (io >= 0)
            {
                int r = find.IndexOf(' ') + 1;
                codeEditor.Select(io + r, find.Length - r);
            }
        }
        void ToCSharp()
        {
            List<dgMember> prop = new List<dgMember>();
            List<dgMember> meth = new List<dgMember>();
            lbMembers.Items.Clear();
            cbMethods.Items.Clear();
            // Convert to C#
            string cs = string.Empty;
            foreach (AssemblyNameReference anr in t.Module.AssemblyReferences)
            {
                cs += string.Format("using {0};\r", anr.Name);
            }
            cs += string.Format("\rnamespace {0}\r", t.Namespace) + "{\r\t";
            string classAttrib = string.Empty;
            cs += string.Format("{0}class {1}\r", classAttrib, t.Name) + "\t{";
            foreach (FieldDefinition f in t.Fields)
            {
                prop.Add(new dgMember() { Name = f.Name, Tag = f, Type = dgMember.MemberType.Property, Line = cs.Split('\r').Length });
                cs += string.Format("\r\t\t{2}{0} {1};", getCSTypeName(f.FieldType.Name), f.Name, getModifiers(f));
            }
            foreach (PropertyDefinition p in t.Properties)
            {
                prop.Add(new dgMember() { Name = p.Name, Tag = p, Type = dgMember.MemberType.Property, Line = cs.Split('\r').Length });
                cs += string.Format("\r\t\t{3}{0} {1}{2}", getCSTypeName(p.PropertyType.Name), p.Name, GetPropertyBody(p.GetMethod, p.SetMethod), getModifiers(p));
            }
            foreach (MethodDefinition m in t.Constructors)
            {
                meth.Add(new dgMember() { Name = m.Name, Tag = m, Type = dgMember.MemberType.Method, Line = cs.Split('\r').Length });
                cs += string.Format("\r\t\tpublic {0}({1}){2}", t.Name, getParamsString(m.Parameters), GetMethodBody(m));
            }
            foreach (MethodDefinition m in t.Methods)
            {
                cbMethods.Items.Add(new System.Windows.Controls.ComboBoxItem() { Content = m.Name, Tag = m });
                if (!m.Attributes.HasFlag(MethodAttributes.SpecialName))
                {
                    int ln = cs.Split('\r').Length;
                    meth.Add(new dgMember() { Name = m.Name, Tag = m, Type = dgMember.MemberType.Method, Line = ln });
                    cs += string.Format("\r\t\t{4}{0} {1}({2}){3}", getCSTypeName(m.ReturnType.ReturnType.Name), m.Name, getParamsString(m.Parameters), GetMethodBody(m), getModifiers(m));
                }
            }
            cs += "\r\t}\r}";
            codeEditor.Text = cs;
            // Fill member list
            foreach (dgMember d in meth) lbMembers.Items.Add(d);
            foreach (dgMember d in prop) lbMembers.Items.Add(d);
        }
        string getModifiers(PropertyDefinition p)
        {
            return "public ";
        }
        string getModifiers(FieldDefinition f)
        {
            Dictionary<FieldAttributes, string> d = new Dictionary<FieldAttributes, string>();
            string mod = string.Empty;
            d.Add(FieldAttributes.Public, "public");
            d.Add(FieldAttributes.Private, "private");
            d.Add(FieldAttributes.Static, "static");
            foreach (KeyValuePair<FieldAttributes, string> D in d) if (f.Attributes.HasFlag(D.Key)) mod += D.Value + " ";
            return mod;
        }
        string getModifiers(MethodDefinition m)
        {
            Dictionary<MethodAttributes, string> d = new Dictionary<MethodAttributes, string>();
            string mod = string.Empty;
            d.Add(MethodAttributes.Public, "public");
            d.Add(MethodAttributes.Private, "private");
            d.Add(MethodAttributes.Static, "static");
            d.Add(MethodAttributes.Virtual, "virtual");
            d.Add(MethodAttributes.Abstract, "abstract");
            foreach (KeyValuePair<MethodAttributes, string> D in d) if (m.Attributes.HasFlag(D.Key)) mod += D.Value + " ";
            return mod;
        }
        string GetPropertyBody(MethodDefinition getter, MethodDefinition setter)
        {
            string cs = "\r\t\t{";
            string nl = "\r\t\t\t";
            if (getter != null) cs += nl + "get" + GetMethodBody(getter).Replace("\t\t", "\t\t\t");
            if (setter != null) cs += nl + "set" + GetMethodBody(setter).Replace("\t\t", "\t\t\t");
            cs += "\r\t\t}";
            return cs;
        }
        string GetMethodBody(MethodDefinition m)
        {
            if (m.HasBody)
            {
                string cs = "\r\t\t{";
                cs += il2cs(m.Body, "\r\t\t\t");
                cs += "\r\t\t}";
                return cs;
            }
            else return ";";
        }
        string il2cs(MethodBody mb, string j)
        {
            List<string> cs = new List<string> { };
            foreach (Instruction i in mb.Instructions)
            {
                cs.Add(i.OpCode.Name);
                cs.Add(string.Format("// flow[{0}] type[{1}] pop[{2}] push[{3}]", i.OpCode.FlowControl, i.OpCode.OpCodeType, i.OpCode.StackBehaviourPop, i.OpCode.StackBehaviourPush));
                cs.Add("// [" + i.OpCode.OperandType + "] " + i.Operand);
            }
            string csstr = string.Empty;
            foreach (string line in cs) csstr += string.Concat(j, line);
            return csstr;
        }
        void FromCSharp()
        {
        }
        string getParamsString(ParameterDefinitionCollection par)
        {
            string p = string.Empty;
            foreach (ParameterDefinition pd in par)
                p += string.Format("{0} {1}, ", getCSTypeName(pd.ParameterType.Name), pd.Name);
            if (p.Length > 0) p = p.Substring(0, p.Length - 2);
            return p;
        }
        public static string getCSTypeName(string tn)
        {
            switch (tn.Replace("[]", string.Empty))
            {
                case "String": tn = tn.Replace("String", "string"); break;
                case "Byte": tn = tn.Replace("Byte", "byte"); break;
                case "Int32": tn = tn.Replace("Int32", "int"); break;
                case "Boolean": tn = tn.Replace("Boolean", "bool"); break;
                case "Void": tn = tn.Replace("Void", "void"); break;
            }
            return tn;
        }
        string instructionText(Instruction inst)
        {
            if (inst.Operand is Mono.Cecil.Cil.Instruction)
            {
                Mono.Cecil.Cil.Instruction instruccion = (Instruction)inst.Operand;
                return string.Format("{0} {1}", inst.OpCode.ToString(), instruccion.Offset.ToString());
            }
            else if (inst.Operand is string) return string.Format("{0} \"{1}\"", inst.OpCode.ToString(), inst.Operand.ToString());
            else if (inst.Operand is MethodReference)
            {
                MethodReference metodo = (MethodReference)inst.Operand;
                return inst.OpCode.ToString() + " " + metodo.ToString();
            }
            else if (inst.Operand != null) return inst.OpCode.ToString() + " " + inst.Operand.ToString();
            else return inst.OpCode.ToString();
        }
        void loadInstructions()
        {
            lbInstructions.Items.Clear();
            if (cbMethods.SelectedItem != null)
            {
                MethodDefinition m = ((cbMethods.SelectedItem as System.Windows.Controls.ComboBoxItem).Tag as MethodDefinition);
                if (m.HasBody) foreach (Instruction i in m.Body.Instructions) lbInstructions.Items.Add(new dgInst() { Index = lbInstructions.Items.Count, Inst = i, Text = instructionText(i) });
            }
        }
        MethodDefinition delInstruction(bool del)
        {
            MethodDefinition md = ((cbMethods.SelectedItem as System.Windows.Controls.ComboBoxItem).Tag as MethodDefinition);
            if (del) md.Body.Instructions.RemoveAt(lbInstructions.SelectedIndex);
            return md;
        }
        void refreshInstructions()
        {
            int oldsi = lbInstructions.SelectedIndex;
            int tmp = cbMethods.SelectedIndex;
            LoadMembers();
            cbMethods.SelectedIndex = tmp;
            lbInstructions.SelectedIndex = oldsi;
        }
        void addInstruction(bool replace)
        {
            try
            {
                MethodDefinition md = delInstruction(replace);
                CilWorker worker = md.Body.CilWorker;
                OpCode opcode = OpCodes.Add;
                foreach (System.Reflection.MemberInfo mi in typeof(OpCodes).GetMembers())
                {
                    if (mi.MemberType.ToString() == "Field" && cbOpCode.Text == mi.Name.ToLower().Replace("_", "."))
                    {
                        System.Reflection.FieldInfo info = (System.Reflection.FieldInfo)mi;
                        opcode = (OpCode)info.GetValue(null);
                    }
                }
                Instruction sentence;
                if (inOperand.Text.Length == 0)
                    sentence = worker.Create(opcode);
                else
                {
                    int val;
                    if (int.TryParse(inOperand.Text, out val))
                        sentence = worker.Create(opcode, val);
                    else sentence = worker.Create(opcode, inOperand.Text);
                }
                md.Body.CilWorker.InsertBefore(md.Body.Instructions[lbInstructions.SelectedIndex], sentence);
                t.Module.Assembly.MainModule.Import(md.DeclaringType);
            }
            catch (Exception) { MessageBox.Show("The operand value is not valid for this opcode"); }
            refreshInstructions();
        }
        private void TextEditor_KeyUp(object sender, System.Windows.Input.KeyEventArgs e) { dt.Stop(); dt.Start(); }
        private void lbMembers_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e) { FocusMember((sender as System.Windows.Controls.ListBox).SelectedItem as dgMember); }
        private void cbMethods_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e) { loadInstructions(); }
        private void lbInstructions_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            cbOpCode.IsEnabled = false;
            inOperand.IsEnabled = false;
            if (lbInstructions.SelectedItem != null)
            {
                dgInst i = (lbInstructions.SelectedItem as dgInst);
                cbOpCode.IsEnabled = true;
                inOperand.IsEnabled = true;
                cbOpCode.Text = i.Inst.OpCode.ToString();
                cbOpCode.SelectedItem = i.Inst.OpCode.Name;
                inOperand.Text = (i.Inst.Operand == null ? string.Empty : i.Inst.Operand.ToString());
                MsilInstruction ins = MsilInstruction.GetInstruction(i.Inst.OpCode.ToString());
                tbOpCodeInfo.Inlines.Clear();
                tbOpCodeInfo.Inlines.Add(new System.Windows.Documents.Run(i.Inst.OpCode.Name) { FontWeight = FontWeights.Bold });
                tbOpCodeInfo.Inlines.Add(new System.Windows.Documents.Run(string.Format(" [0x{0},0x{1}]", BitConverter.ToString(new byte[] { i.Inst.OpCode.Op1 }), BitConverter.ToString(new byte[] { i.Inst.OpCode.Op2 }))));
                if (ins != null)
                {
                    tbOpCodeInfo.Inlines.Add(new System.Windows.Documents.LineBreak());
                    tbOpCodeInfo.Inlines.Add(new System.Windows.Documents.Run(ins.Description));
                }
            }
            else tbOpCodeInfo.Text = "No instruction selected";
        }
        private void UpdateButton_Click(object sender, RoutedEventArgs e) { if (lbInstructions.SelectedItem != null) addInstruction(true); }
        private void AddButton_Click(object sender, RoutedEventArgs e) { if (lbInstructions.SelectedItem != null) addInstruction(false); }
        private void lbInstructions_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Delete && (sender as System.Windows.Controls.ListBox).SelectedItem != null)
                if (MessageBox.Show("Are you sure you want to remove this instruction?", "Remove instruction", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                {
                    delInstruction(true);
                    refreshInstructions();
                }
        }
        private void HelpHyperlink_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(@"http://msdn.microsoft.com/en-us/library/8c9kx4t8(vs.71).aspx"));
        }*/
    }
}
