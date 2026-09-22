// NOT a ported file - new code added by the .NET 9 port.
//
// BloogBot referenced a prebuilt Fasm.NET.dll (Binarysharp.Assemblers.Fasm), a mixed-mode C++/CLI
// wrapper around the flat assembler built for net461. Mixed-mode assemblies built against .NET
// Framework cannot load on .NET 9, so this project provides the same namespace and the same
// members that BloogBot/MemoryManager.cs uses, P/Invoking the stock 32-bit FASM.DLL instead.
// With this in place MemoryManager.cs is byte-identical to the original again.
//
// FASM.DLL (x86, from flatassembler.net) must sit next to the bot at runtime. It is loaded
// lazily on the first Assemble call, so its absence does not affect the build and does not
// affect any code path that never assembles.
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace Binarysharp.Assemblers.Fasm
{
    /// <summary>
    /// The condition codes fasm_Assemble returns, as documented in FASM.DLL's FASMDLL.TXT.
    /// </summary>
    public enum FasmConditions
    {
        Ok = 0,
        Working = 1,
        Error = 2,
        InvalidParameter = -1,
        OutOfMemory = -2,
        StackOverflow = -3,
        SourceNotFound = -4,
        UnexpectedEndOfSource = -5,
        CannotGenerateCode = -6,
        FormatLimitationsExcedded = -7,
        WriteFailed = -8,
        InvalidDefinition = -9,
    }

    /// <summary>
    /// The assembler error codes reported when the condition is <see cref="FasmConditions.Error"/>.
    /// </summary>
    public enum FasmErrors
    {
        FileNotFound = -101,
        ErrorReadingFile = -102,
        InvalidFileFormat = -103,
        InvalidMacroArguments = -104,
        IncompleteMacro = -105,
        UnexpectedCharacters = -106,
        InvalidArgument = -107,
        IllegalInstruction = -108,
        InvalidOperand = -109,
        InvalidOperandSize = -110,
        OperandSizeNotSpecified = -111,
        OperandSizesDoNotMatch = -112,
        InvalidAddressSize = -113,
        AddressSizesDoNotAgree = -114,
        DisallowedCombinationOfRegisters = -115,
        LongImmediateNotEncodable = -116,
        RelativeJumpOutOfRange = -117,
        InvalidExpression = -118,
        InvalidAddress = -119,
        InvalidValue = -120,
        ValueOutOfRange = -121,
        UndefinedSymbol = -122,
        InvalidUseOfSymbol = -123,
        NameTooLong = -124,
        InvalidName = -125,
        ReservedWordUsedAsSymbol = -126,
        SymbolAlreadyDefined = -127,
        MissingEndQuote = -128,
        MissingEndDirective = -129,
        UnexpectedInstruction = -130,
        ExtraCharactersOnLine = -131,
        SectionNotAlignedEnough = -132,
        SettingAlreadySpecified = -133,
        DataAlreadyDefined = -134,
        TooManyRepeats = -135,
        SymbolOutOfScope = -136,
        UserError = -140,
        AssertionFailed = -141,
    }

    /// <summary>
    /// Thrown when FASM fails to assemble the mnemonics it was given.
    /// </summary>
    public class FasmAssemblerException : Exception
    {
        public FasmErrors ErrorCode { get; private set; }

        public int ErrorLine { get; private set; }

        public int ErrorOffset { get; private set; }

        public string Mnemonics { get; private set; }

        public FasmAssemblerException(int errorCode, int errorLine, int errorOffset, string mnemonics)
            : base(string.Format(
                "An error occurred during FASM was assembling mnemonics. Error code: {0} ({1}); Error line: {2}; Error offset: {3}",
                errorCode, (FasmErrors)errorCode, errorLine, errorOffset))
        {
            ErrorCode = (FasmErrors)errorCode;
            ErrorLine = errorLine;
            ErrorOffset = errorOffset;
            Mnemonics = mnemonics;
        }
    }

    /// <summary>
    /// Assembles x86 mnemonics through FASM.DLL.
    /// </summary>
    public class FasmNet
    {
        [DllImport("FASM.DLL", CallingConvention = CallingConvention.StdCall)]
        static extern int fasm_GetVersion();

        [DllImport("FASM.DLL", CallingConvention = CallingConvention.StdCall)]
        static extern int fasm_Assemble(
            byte[] lpSource,
            IntPtr lpMemory,
            int cbMemorySize,
            int nPassesLimit,
            IntPtr hDisplayPipe);

        // FASMDLL.TXT: the output block has to be at least 4096 bytes. Everything this tree
        // assembles is a handful of instructions, but the buffer is grown on OutOfMemory anyway.
        const int InitialMemorySize = 0x10000;
        const int MaximumMemorySize = 0x1000000;
        const int PassesLimit = 100;

        readonly List<string> mnemonics = new List<string>();

        /// <summary>
        /// The version of the loaded FASM.DLL, as major.minor.
        /// </summary>
        public static Version GetVersion()
        {
            var version = fasm_GetVersion();
            return new Version(version & 0xFFFF, version >> 16);
        }

        /// <summary>
        /// The mnemonics collected so far.
        /// </summary>
        public string Mnemonics
        {
            get { return string.Join("\n", mnemonics); }
        }

        /// <summary>
        /// Drops every mnemonic collected so far.
        /// </summary>
        public void Clear()
        {
            mnemonics.Clear();
        }

        /// <summary>
        /// Adds one line of assembly.
        /// </summary>
        public void AddLine(string line)
        {
            mnemonics.Add(line);
        }

        /// <summary>
        /// Adds one formatted line of assembly.
        /// </summary>
        public void AddLine(string line, params object[] args)
        {
            mnemonics.Add(string.Format(line, args));
        }

        /// <summary>
        /// Assembles the collected mnemonics.
        /// </summary>
        public byte[] Assemble()
        {
            return Assemble(Mnemonics);
        }

        /// <summary>
        /// Assembles the collected mnemonics as if they were placed at <paramref name="baseAddress"/>.
        /// </summary>
        public byte[] Assemble(IntPtr baseAddress)
        {
            // The prebuilt Fasm.NET formatted this as "org {0}", i.e. the pointer in decimal, which
            // is the one literal recoverable from the old assembly. The org line goes in front of
            // the collected mnemonics without being stored, so a later Assemble() is unaffected.
            return Assemble(string.Format("org {0}", baseAddress) + "\n" + Mnemonics);
        }

        /// <summary>
        /// Assembles the given source.
        /// </summary>
        public static byte[] Assemble(string source)
        {
            // FASM.DLL wants a null-terminated ASCII source.
            var sourceBytes = Encoding.ASCII.GetBytes(source + "\0");

            var memorySize = InitialMemorySize;
            while (true)
            {
                var memory = Marshal.AllocHGlobal(memorySize);
                try
                {
                    var condition = fasm_Assemble(sourceBytes, memory, memorySize, PassesLimit, IntPtr.Zero);

                    // FASM_STATE: condition at +0, then output_length / error_code at +4 and
                    // output_data / error_line at +8.
                    if (condition == (int)FasmConditions.Ok)
                    {
                        var length = Marshal.ReadInt32(memory, 4);
                        var data = Marshal.ReadIntPtr(memory, 8);
                        var output = new byte[length];
                        Marshal.Copy(data, output, 0, length);
                        return output;
                    }

                    if (condition == (int)FasmConditions.OutOfMemory && memorySize < MaximumMemorySize)
                    {
                        memorySize *= 2;
                        continue;
                    }

                    var errorCode = Marshal.ReadInt32(memory, 4);
                    var errorLine = 0;
                    var errorOffset = 0;
                    if (condition == (int)FasmConditions.Error)
                    {
                        // The third field points at a LINE structure: char* file_path, then the
                        // line number, then the file offset.
                        var line = Marshal.ReadIntPtr(memory, 8);
                        if (line != IntPtr.Zero)
                        {
                            errorLine = Marshal.ReadInt32(line, IntPtr.Size);
                            errorOffset = Marshal.ReadInt32(line, IntPtr.Size + 4);
                        }
                    }
                    else
                    {
                        errorCode = condition;
                    }

                    throw new FasmAssemblerException(errorCode, errorLine, errorOffset, source);
                }
                finally
                {
                    Marshal.FreeHGlobal(memory);
                }
            }
        }
    }
}
