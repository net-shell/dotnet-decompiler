using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace XapEditor
{
    public class MsilInstruction
    {
        public MsilInstruction(string byteCode, string format, string description)
        {
            this.ByteCode = byteCode;
            this.Format = format;
            this.Description = description;
        }

        public string ByteCode;
        public string Format;
        public string Description;

        public static MsilInstruction GetInstruction(string Format)
        {
            foreach (MsilInstruction istn in Instructions)
            {
                string text = istn.Format;
                if (istn.Format.IndexOf(' ') != -1)
                    text = InstructionFormat.Match(istn.Format).Value;
                if (string.Compare(text, Format.Replace('_', '.'), true) == 0)
                    return istn;
            }
            return null;
        }

        private static Regex InstructionFormat = new Regex(
      ".*(?=\\s)",
    RegexOptions.IgnoreCase
    | RegexOptions.CultureInvariant
    | RegexOptions.IgnorePatternWhitespace
    | RegexOptions.Compiled
    );

        /// <summary>
        /// Get a list of the MSIL instructions
        /// </summary>
        public static MsilInstruction[] Instructions;

        static MsilInstruction()
        {
            Instructions = new MsilInstruction[]{
                new MsilInstruction("58","add","Add two values, returning a new value"),
new MsilInstruction("D6","add.ovf","Add signed integer values with overflow check. "),
new MsilInstruction("D7","add.ovf.un","Add unsigned integer values with overflow check."),
new MsilInstruction("5F","And","Bitwise AND of two integral values, returns an integral value"),
new MsilInstruction("FE 00","arglist","return argument list handle for the current method "),
new MsilInstruction("3B <int32>","beq target","branch to target if equal"),
new MsilInstruction("2E <int8>","beq.s target","branch to target if equal, short form"),
new MsilInstruction("3C <int32>","bge target","branch to target if greater than or equal to"),
new MsilInstruction("2F <int8>","bge.s target","branch to target if greater than or equal to, short form"),
new MsilInstruction("41 <int32>","bge.un target","branch to target if greater than or equal to (unsigned or unordered)"),
new MsilInstruction("34 <int8>","bge.un.s target","branch to target if greater than or equal to (unsigned or unordered), short form"),
new MsilInstruction("3D <int32>","bgt target","branch to target if greater than"),
new MsilInstruction("30 <int8>","bgt.s target","branch to target if greater than, short form"),
new MsilInstruction("42 <int32>","bgt.un target","branch to target if greater than (unsigned or unordered)"),
new MsilInstruction("35 <int8>","bgt.un.s target","branch to target if greater than (unsigned or unordered), short form"),
new MsilInstruction("3E <int32>","ble target","branch to target if less than or equal to"),
new MsilInstruction("31 <int8>","ble.s target","branch to target if less than or equal to, short form"),
new MsilInstruction("43 <int32>","ble.un target","branch to target if less than or equal to (unsigned or unordered)"),
new MsilInstruction("36 <int8>","ble.un.s target","branch to target if less than or equal to (unsigned or unordered), short form"),
new MsilInstruction("3F <int32>","blt target","branch to target if less than"),
new MsilInstruction("32 <int8>","blt.s target","branch to target if less than, short form"),
new MsilInstruction("44 <int32>","blt.un target","Branch to target if less than (unsigned or unordered) "),
new MsilInstruction("37 <int8>","blt.un.s target","Branch to target if less than (unsigned or unordered), short form"),
new MsilInstruction("40 <int32>","bne.un target","branch to target if unequal or unordered"),
new MsilInstruction("33 <int8>","bne.un.s target","branch to target if unequal or unordered, short form"),
new MsilInstruction("38 <int32>","br target","branch to target "),
new MsilInstruction("2B <int8>","br.s target","branch to target, short form"),
new MsilInstruction("01 ","break","inform a debugger that a breakpoint has been reached."),
new MsilInstruction("39 <int32>","brfalse target","branch to target if value is zero (false)"),
new MsilInstruction("2C <int8>","brfalse.s target","branch to target if value is zero (false), short form"),
new MsilInstruction("39 <int32>","brnull target","branch to target if value is null (alias for brfalse)"),
new MsilInstruction("2C <int8>","brnull.s target","branch to target if value is null (alias for brfalse.s), short form"),
new MsilInstruction("39 <int32>","brzero target","branch to target if value is zero (alias for brfalse)"),
new MsilInstruction("2C <int8>","brzero.s target","branch to target if value is zero (alias for brfalse.s), short form"),
new MsilInstruction("3A <int32>","brtrue target","branch to target if value is non-zero (true)"),
new MsilInstruction("2D <int8>","brtrue.s target","branch to target if value is non-zero (true), short form"),
new MsilInstruction("3A <int32>","brinst target","branch to target if value is a non-null object reference (alias for brtrue)"),
new MsilInstruction("2D <int8>","brinst.s target","branch to target if value is a non-null object reference, short form (alias for brtrue.s)"),
new MsilInstruction("28 <T>","call method","Call method described by method"),
new MsilInstruction("29 <T>","calli callsitedescr","Call method indicated on the stack with arguments described by callsitedescr."),
new MsilInstruction("FE 01","ceq","push 1 (of type int32) if value1 equals value2, else 0"),
new MsilInstruction("FE 02 ","cgt","push 1 (of type int32) if value1 > value2, else 0"),
new MsilInstruction("FE 03 ","cgt.un","push 1 (of type int32) if value1 > value2, unsigned or unordered, else 0"),
new MsilInstruction("C3 ","ckfinite","throw ArithmeticException if value is not a finite number"),
new MsilInstruction("FE 04 ","clt","push 1 (of type int32) if value1 < value2, else 0"),
new MsilInstruction("FE 05 ","clt.un","push 1 (of type int32) if value1 < value2, unsigned or unordered, else 0"),
new MsilInstruction("67","conv.i1","Convert to int8, pushing int32 on stack"),
new MsilInstruction("68","conv.i2","Convert to int16, pushing int32 on stack"),
new MsilInstruction("69","conv.i4","Convert to int32, pushing int32 on stack"),
new MsilInstruction("6A","conv.i8","Convert to int64, pushing int64 on stack"),
new MsilInstruction("6B","conv.r4","Convert to float32, pushing F on stack"),
new MsilInstruction("6C","conv.r8","Convert to float64, pushing F on stack"),
new MsilInstruction("D2","conv.u1","Convert to unsigned int8, pushing int32 on stack"),
new MsilInstruction("D1","conv.u2","Convert to unsigned int16, pushing int32 on stack"),
new MsilInstruction("6D","conv.u4","Convert to unsigned int32, pushing int32 on stack"),
new MsilInstruction("6E","conv.u8","Convert to unsigned int64, pushing int64 on stack"),
new MsilInstruction("D3","conv.i","Convert to native int, pushing native int on stack"),
new MsilInstruction("E0","conv.u","Convert to native unsigned  int, pushing native int on stack"),
new MsilInstruction("76","conv.r.un","Convert unsigned integer to floating-point, pushing F on stack"),
new MsilInstruction("B3","conv.ovf.i1","Convert to an int8 (on the stack as int32) and throw an exception on overflow "),
new MsilInstruction("B5","conv.ovf.i2","Convert to an int16 (on the stack as int32) and throw an exception on overflow "),
new MsilInstruction("B7","conv.ovf.i4","Convert to an int32 (on the stack as int32) and throw an exception on overflow "),
new MsilInstruction("B9","conv.ovf.i8","Convert to an int64 (on the stack as int64) and throw an exception on overflow "),
new MsilInstruction("B4","conv.ovf.u1","Convert to a unsigned int8 (on the stack as int32) and throw an exception on overflow "),
new MsilInstruction("B6","conv.ovf.u2","Convert to a unsigned int16 (on the stack as int32) and throw an exception on overflow "),
new MsilInstruction("B8","conv.ovf.u4","Convert to a unsigned int32 (on the stack as int32) and throw an exception on overflow "),
new MsilInstruction("BA","conv.ovf.u8","Convert to a unsigned int64 (on the stack as int64) and throw an exception on overflow "),
new MsilInstruction("D4","conv.ovf.i","Convert to an native int (on the stack as native int) and throw an exception on overflow"),
new MsilInstruction("D5","conv.ovf.u","Convert to a native unsigned  int (on the stack as native int) and throw an exception on overflow"),
new MsilInstruction("82","conv.ovf.i1.un","Convert unsigned to an int8 (on the stack as int32) and throw an exception on overflow "),
new MsilInstruction("83","conv.ovf.i2.un","Convert unsigned to an int16 (on the stack as int32) and throw an exception on overflow "),
new MsilInstruction("84","conv.ovf.i4.un","Convert unsigned to an int32 (on the stack as int32) and throw an exception on overflow "),
new MsilInstruction("85","conv.ovf.i8.un","Convert unsigned to an int64 (on the stack as int64) and throw an exception on overflow "),
new MsilInstruction("86","conv.ovf.u1.un","Convert unsigned to an unsigned int8 (on the stack as int32) and throw an exception on overflow "),
new MsilInstruction("87","conv.ovf.u2.un","Convert unsigned to an unsigned int16 (on the stack as int32) and throw an exception on overflow "),
new MsilInstruction("88","conv.ovf.u4.un","Convert unsigned to an unsigned int32 (on the stack as int32) and throw an exception on overflow "),
new MsilInstruction("89","conv.ovf.u8.un","Convert unsigned to an unsigned int64 (on the stack as int64) and throw an exception on overflow "),
new MsilInstruction("8A","conv.ovf.i.un","Convert unsigned to a native int (on the stack as native int) and throw an exception on overflow"),
new MsilInstruction("8B","conv.ovf.u.un","Convert unsigned to a native unsigned  int (on the stack as native int) and throw an exception on overflow"),
new MsilInstruction("FE 17","cpblk","Copy data from memory to memory"),
new MsilInstruction("5B","div","Divide two values to return a quotient or floating-point result"),
new MsilInstruction("5C","div.un","Divide two values, unsigned, returning a quotient"),
new MsilInstruction("25 ","dup","duplicate value on the top of the stack"),
new MsilInstruction("FE 11","Endfilter","End filter clause of SEH exception handling"),
new MsilInstruction("DC","Endfault","End fault clause of an exception block"),
new MsilInstruction("DC","Endfinally","End finally clause of an exception block"),
new MsilInstruction("FE 18","initblk","Set a block of memory to a given byte"),
new MsilInstruction("27 <T>","jmp method","Exit current method and jump to specified method"),
new MsilInstruction("FE 09 <unsigned int16>","ldarg num","Load argument numbered num onto stack."),
new MsilInstruction("0E <unsigned int8>","ldarg.s num","Load argument numbered num onto stack, short form."),
new MsilInstruction("02","ldarg.0","Load argument 0 onto stack"),
new MsilInstruction("03","ldarg.1","Load argument 1 onto stack"),
new MsilInstruction("04","ldarg.2","Load argument 2 onto stack"),
new MsilInstruction("05","ldarg.3","Load argument 3 onto stack"),
new MsilInstruction("FE 0A <unsigned int16>","ldarga argNum","fetch the address of argument argNum."),
new MsilInstruction("0F <unsigned int8> ","ldarga.s argNum","fetch the address of argument argNum, short form"),
new MsilInstruction("20 <int32>","ldc.i4 num","Push num of type int32 onto the stack as int32."),
new MsilInstruction("21 <int64>","ldc.i8 num","Push num of type int64 onto the stack as int64."),
new MsilInstruction("22 <float32>","ldc.r4 num","Push num of type float32 onto the stack as F."),
new MsilInstruction("23 <float64>","ldc.r8 num","Push num of type float64 onto the stack as F."),
new MsilInstruction("16","ldc.i4.0","Push 0 onto the stack as int32."),
new MsilInstruction("17","ldc.i4.1","Push 1 onto the stack as int32."),
new MsilInstruction("18","ldc.i4.2","Push 2 onto the stack as int32."),
new MsilInstruction("19","ldc.i4.3","Push 3 onto the stack as int32."),
new MsilInstruction("1A","ldc.i4.4","Push 4 onto the stack as int32."),
new MsilInstruction("1B","ldc.i4.5","Push 5 onto the stack as int32."),
new MsilInstruction("1C","ldc.i4.6","Push 6 onto the stack as int32."),
new MsilInstruction("1D","ldc.i4.7","Push 7 onto the stack as int32."),
new MsilInstruction("1E","ldc.i4.8","Push 8 onto the stack as int32."),
new MsilInstruction("15","ldc.i4.m1","Push -1 onto the stack as int32."),
new MsilInstruction("15","ldc.i4.M1","Push -1 of type int32 onto the stack as int32 (alias for ldc.i4.m1)."),
new MsilInstruction("1F <int8>","ldc.i4.s num","Push num onto the stack as int32, short form."),
new MsilInstruction("FE 06 <T>","ldftn method","Push a pointer to a method referenced by method on the stack"),
new MsilInstruction("46","ldind.i1 ","Indirect load value of type int8 as int32 on the stack."),
new MsilInstruction("48","ldind.i2","Indirect load value of type int16 as int32 on the stack."),
new MsilInstruction("4A","ldind.i4 ","Indirect load value of type int32 as int32 on the stack."),
new MsilInstruction("4C","ldind.i8 ","Indirect load value of type int64 as int64 on the stack."),
new MsilInstruction("47 ","ldind.u1 ","Indirect load value of type unsigned int8 as int32 on the stack."),
new MsilInstruction("49","ldind.u2","Indirect load value of type unsigned int16 as int32 on the stack."),
new MsilInstruction("4B","ldind.u4","Indirect load value of type unsigned int32 as int32 on the stack."),
new MsilInstruction("4E","ldind.r4","Indirect load value of type float32 as F on the stack."),
new MsilInstruction("4C","ldind.u8 ","Indirect load value of type unsigned int64 as int64 on the stack (alias for ldind.i8)."),
new MsilInstruction("4F","ldind.r8 ","Indirect load value of type float64 as F on the stack."),
new MsilInstruction("4D","ldind.i","Indirect load value of type native int as native int on the stack"),
new MsilInstruction("50","ldind.ref","Indirect load value of type object ref as O on the stack."),
new MsilInstruction("FE 0C<unsigned int16>","ldloc indx","Load local variable of index indx onto stack."),
new MsilInstruction("11 <unsigned int8>","ldloc.s indx","Load local variable of index indx onto stack, short form."),
new MsilInstruction("06","ldloc.0 ","Load local variable 0 onto stack."),
new MsilInstruction("07","ldloc.1 ","Load local variable 1 onto stack."),
new MsilInstruction("08","ldloc.2 ","Load local variable 2 onto stack."),
new MsilInstruction("09","ldloc.3 ","Load local variable 3 onto stack."),
new MsilInstruction("FE 0D <unsigned int16>","ldloca index","Load address of local variable with index indx"),
new MsilInstruction("12 <unsigned int8>","ldloca.s index","Load address of local variable with index indx, short form"),
new MsilInstruction("14","ldnull","Push null reference on the stack"),
new MsilInstruction("DD <int32>","leave target","Exit a protected region of code."),
new MsilInstruction("DE <int8>","leave.s target","Exit a protected region of code, short form"),
new MsilInstruction("FE 0F","localloc ","Allocate space from the local memory pool."),
new MsilInstruction("5A","mul","Multiply values"),
new MsilInstruction("D8","mul.ovf","Multiply signed integer values. Signed result must fit in same size"),
new MsilInstruction("D9","mul.ovf.un","Multiply unsigned integer values. Unsigned result must fit in same size"),
new MsilInstruction("65","neg","Negate value"),
new MsilInstruction("00","nop","Do nothing"),
new MsilInstruction("66","not","Bitwise complement"),
new MsilInstruction("60","or","Bitwise OR of two integer values, returns an integer."),
new MsilInstruction("26","pop","pop a value from the stack"),
new MsilInstruction("5D","rem","Remainder of dividing value1 by value2"),
new MsilInstruction("5E","rem.un","Remainder of unsigned dividing value1 by value2"),
new MsilInstruction("2A","Ret","Return from method, possibly returning a value"),
new MsilInstruction("62","Shl","Shift an integer to the left (shifting in zeros)"),
new MsilInstruction("63","Shr","Shift an integer right, (shift in sign), return an integer"),
new MsilInstruction("64","shr.un","Shift an integer right, (shift in zero), return an integer"),
new MsilInstruction("FE 0B <unsigned int16>","starg num","Store a value to the argument numbered num"),
new MsilInstruction("10 <unsigned int8>","starg.s num","Store a value to the argument numbered num, short form"),
new MsilInstruction("52","stind.i1 ","Store value of type int8 into memory at address"),
new MsilInstruction("53","stind.i2 ","Store value of type int16 into memory at address"),
new MsilInstruction("54","stind.i4 ","Store value of type int32 into memory at address"),
new MsilInstruction("55","stind.i8 ","Store value of type int64 into memory at address"),
new MsilInstruction("56","stind.r4","Store value of type float32 into memory at address"),
new MsilInstruction("57","stind.r8 ","Store value of type float64 into memory at address"),
new MsilInstruction("DF","stind.i ","Store value of type native int into memory at address"),
new MsilInstruction("51","stind.ref","Store value of type object ref (type O) into memory at address"),
new MsilInstruction("FE 0E <unsigned int16>","stloc indx","Pop value from stack into local variable indx."),
new MsilInstruction("13 <unsigned int8>","stloc.s indx","Pop value from stack into local variable indx, short form."),
new MsilInstruction("0A","stloc.0","Pop value from stack into local variable 0."),
new MsilInstruction("0B","stloc.1","Pop value from stack into local variable 1."),
new MsilInstruction("0C","stloc.2","Pop value from stack into local variable 2."),
new MsilInstruction("0D","stloc.3","Pop value from stack into local variable 3."),
new MsilInstruction("59","sub","Subtract value2 from value1, returning a new value"),
new MsilInstruction("DA","sub.ovf","Subtract native int from an native int. Signed result must fit in same size"),
new MsilInstruction("DB","sub.ovf.un","Subtract native unsigned int from a native unsigned int. Unsigned result must fit in same size"),
new MsilInstruction("45 <unsigned int32> <int32>… <int32>","switch ( t1, t2 … tn )","jump to one of n values"),
new MsilInstruction("61","xor","Bitwise XOR of integer values, returns an integer")
 };
        }
    }
}
