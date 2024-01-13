using System.Collections;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json.Serialization;
using ConsoleApp1;
using Newtonsoft.Json;

public class Program
{
    public enum ComType : byte
    {
        none,

        //Increment the data pointer by one (to point to the next cell to the right).
        incDat,

        //Decrement the data pointer by one (to point to the next cell to the left).
        decDat,

        //Increment the byte at the data pointer by one.
        incAt,

        //Decrement the byte at the data pointer by one.
        decAt,

        //Output the byte at the data pointer.
        output,

        //Accept one byte of input, storing its value in the byte at the data pointer.
        input,

        //If the byte at the data pointer is zero, then instead of moving the instruction pointer forward to the next command, jump it forward to the command after the matching ] command.
        jmpFow,

        //If the byte at the data pointer is nonzero, then instead of moving the instruction pointer forward to the next command, jump it back to the command after the matching [ command.
        jmpBak
    }

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct Com
    {
        public ComType Type;
        public int Jmp;

        public override string ToString()
        {
            var ret = Type switch
            {
                ComType.incDat => ">",
                ComType.decDat => "<",
                ComType.incAt => "+",
                ComType.decAt => "-",
                ComType.output => ".",
                ComType.input => ",",
                ComType.jmpFow => "[",
                ComType.jmpBak => "]",
                _ => ""
            };
            return ret;
        }
    }


    public abstract class BrainFckCommand 
    {
        
        public virtual unsafe void Run(ref int instructionPointer, ref Span<byte> buffer,ref byte* dataPointer){}

     
        public BrainFckCommand(BrainFckCommand other)
        {
            
        }
        public BrainFckCommand(ref Com command)
        {
            if (!Types.Contains(command.Type)) throw new Exception($"Bro u dumb");
        }

        public override string ToString()
        {
            return GetDebugString();
        }

        public abstract string GetDebugString();

        public abstract ComType[] Types { get; }
        public abstract void Optimise(ref Span<Com> commands, ref int index, IReadOnlyList<BrainFckCommand> readOnlyCommands);
    }

    public class MoveDataPointerCommand : BrainFckCommand
    {
        public override unsafe void Run(ref int instructionPointer, ref Span<byte> buffer, ref byte* dataPointer)
        {
            unchecked
            {
                dataPointer += MoveAmount;
            }
        }

        public override string GetDebugString()
        {
            return $"adr+ {MoveAmount}";
        }

        public override ComType[] Types => new ComType[]{ ComType.incDat, ComType.decDat };
        public int MoveAmount = 0;
     
        public override void Optimise(ref Span<Com> commands, ref int index, IReadOnlyList<BrainFckCommand> readOnlyCommands)
        {
            MoveAmount = 0;
            var am = 0;
            foreach (var c in commands.RepeatAmountBeforeOther(index, Till))
            {
                MoveAmount += c.Type == ComType.incDat ? 1 : -1;
                am++;

            }
            index += am-1 ;
        }
        private static bool Till(Com com)
        {
            return com.Type is ComType.incDat or ComType.decDat;
        }
        public MoveDataPointerCommand(ref Com command) : base(ref command)
        {
        }

        public MoveDataPointerCommand(SectionStartCommand sec):base(sec)
        {
            
        }
    }
    public class AddToDataPointerCommand : BrainFckCommand
    {
        public override string GetDebugString()
        {
            return $"dat+ {AddAmount}";
        }
        public override unsafe void Run(ref int instructionPointer, ref Span<byte> buffer, ref byte* dataPointer)
        {
            unchecked
            {
                (*dataPointer) +=(byte) AddAmount;
            }
        }

        public override ComType[] Types => new ComType[]{ ComType.incAt, ComType.decAt };
        public int AddAmount = 0;
     
        public override void Optimise(ref Span<Com> commands, ref int index, IReadOnlyList<BrainFckCommand> readOnlyCommands)
        {
            AddAmount = 0;
            var am = 0;
            foreach (var c in commands.RepeatAmountBeforeOther(index, Till))
            {
                AddAmount += c.Type == ComType.incAt ? 1 : -1;
                am++;
            }
            index += am -1;
        }

        private static bool Till(Com com)
        {
            return com.Type is ComType.incAt or ComType.decAt;
        }

        public AddToDataPointerCommand(ref Com command) : base(ref command)
        {
        }
        public AddToDataPointerCommand() : base(null)
        {
        }
    }

    public class SectionStartCommand : BrainFckCommand
    {
        public override string GetDebugString()
        {
            return $"sec start, ends {SectionEndIndexReal}";
        }
        public SectionStartCommand(ref Com command) : base(ref command)
        {
            SectionEndIndex = command.Jmp;
        }
        public SectionStartCommand() : base(null)
        {
        }
        public SectionEndCommand SectionEnd;

        public override unsafe void Run(ref int instructionPointer, ref Span<byte> buffer, ref byte* dataPointer)
        {
            if (*dataPointer == 0)
            {
                instructionPointer =SectionEndIndexReal;
            }
        }

        public List<BrainFckCommand> Commands;
        public int SectionEndIndexReal;

        public override ComType[] Types=> new ComType[]{ ComType.jmpFow };
        public int SectionEndIndex;

        public override void Optimise(ref Span<Com> commands, ref int index, IReadOnlyList<BrainFckCommand> readOnlyCommands)
        {
            
        }
    }
    public class SectionEndCommand : BrainFckCommand
    {
        public override string GetDebugString()
        {
            return $"sec end, starts {SectionStartIndexReal}";
        }
        public SectionEndCommand(ref Com command) : base(ref command)
        {
            SectionStartIndex = command.Jmp;
        } public SectionEndCommand() : base(null)
        {
        }

        public int SectionStartIndex;
        public SectionStartCommand? SectionStart;

        public override unsafe void Run(ref int instructionPointer, ref Span<byte> buffer, ref byte* dataPointer)
        {
            if (*dataPointer != 0)
            {
                instructionPointer =SectionStartIndexReal;
            }
        }       public List<BrainFckCommand> Commands;
        public int SectionStartIndexReal;


        public override ComType[] Types=> new ComType[]{ ComType.jmpBak };
        public override void Optimise(ref Span<Com> commands, ref int index, IReadOnlyList<BrainFckCommand> readOnlyCommands)
        {
            var i = index;
            SectionStart = (SectionStartCommand)((List<BrainFckCommand>)readOnlyCommands).Find(command => (command is SectionStartCommand sec && sec.SectionEndIndex == i))!;
            SectionStart.SectionEnd = this;
        }
    }

    public static StringBuilder sb = new StringBuilder();
    public class OutInputCommand : BrainFckCommand
    {
        public override string GetDebugString()
        {
            string a=In? $"in dat" :$"out data";
            return $"{a}";
        }
        public override unsafe void Run(ref int instructionPointer, ref Span<byte> buffer, ref byte* dataPointer)
        {
            if (In)
            {
                (*dataPointer) = (byte)Console.Read();
            }
            else
            {
                var b = (char)*dataPointer;
                sb.Append(b);
                if (b == '\n')
                {
                    Console.Write(sb);
                    sb.Clear();
                }
            }
        }

        public override ComType[] Types => new ComType[]{ ComType.output, ComType.input };
        public bool In;
     
        public override void Optimise(ref Span<Com> commands, ref int index, IReadOnlyList<BrainFckCommand> readOnlyCommands)
        {
            
        }
        public OutInputCommand(ref Com command) : base(ref command)
        {
            In=command.Type == ComType.input;
        }
    }

    
    public static void ParseLLComs(ReadOnlySpan<char> inputString, ref Span<Com> commands)
    {
        var lastJmpFow = new Stack<int>();
        for (var i = 0; i < inputString.Length; i++)
        {
            var c = inputString[i];
            switch (c)
            {
                case '>':
                {
                    commands[i] = new Com() { Type = ComType.incDat };
                    break;
                }
                case '<':
                {
                    commands[i] = new Com() { Type = ComType.decDat };
                    break;
                }
                case '+':
                {
                    commands[i] = new Com() { Type = ComType.incAt };
                    break;
                }
                case '-':
                {
                    commands[i] = new Com() { Type = ComType.decAt };
                    break;
                }
                case '.':
                {
                    commands[i] = new Com() { Type = ComType.output };
                    break;
                }
                case ',':
                {
                    commands[i] = new Com() { Type = ComType.input };
                    break;
                }
                case '[':
                {
                    lastJmpFow.Push(i);
                    break;
                }
                case ']':
                {
                    var a = lastJmpFow.Pop();
                    commands[a] = new Com() { Type = ComType.jmpFow, Jmp = i };
                    commands[i] = new Com() { Type = ComType.jmpBak, Jmp = a };
                    break;
                }
            }
        }

    }

    public static List<BrainFckCommand> Optimise(ref Span<Com> commands)
    {
        var l = new List<BrainFckCommand>();
        for (int i = 0; i < commands.Length; i++)
        {
            ref Com c = ref commands[i];
            BrainFckCommand command = null;
            switch (c.Type)
            {
                case ComType.incDat:
                case ComType.decDat:
                    command = new MoveDataPointerCommand(ref c);
                    break;
                case ComType.incAt:
                case ComType.decAt:
                    command = new AddToDataPointerCommand(ref c);
                    break;
                case ComType.output:
                case ComType.input:
                    command = new OutInputCommand(ref c);
                    break;
                case ComType.jmpFow:
                    command = new SectionStartCommand(ref c);
                    break;
                case ComType.jmpBak:
                    command = new SectionEndCommand(ref c);
                    break;
                default:
                case ComType.none:
                    break;
            }

            if (command != null)
            {
                command.Optimise(ref commands,ref i, l);
                l.Add(command);
            }
           
        }
        l = l.ExtraOptimize(Extensions.OT.ALLUSEFUL);
        return l;
    }
    public static unsafe void Main(string[] args)
    {
        var filePath = args[0].Replace("\"", "");
          // Pre running
        using var inputStream = File.OpenText(filePath);
        ReadOnlySpan<char> inputString = inputStream.ReadToEnd().ReplaceLineEndings("");
        Span<Com> commands = stackalloc Com[inputString.Length];
        ParseLLComs(inputString,ref commands);
        //Running
        var coms = Optimise(ref commands);
        var instructionPointer = 0;
        Span<byte> buffer = stackalloc byte[30000];
        fixed (byte* b = buffer)
        {
            var dataPointer = b;
            ref var dpRef =ref dataPointer;
            for (int i = 0; i < coms.Count; i++)
            {
                var c = coms[i];
                switch (c)
                {
                    case SectionStartCommand s1:
                        s1.Commands = coms;
                        s1.SectionEndIndexReal=coms.IndexOf(s1.SectionEnd, i);
                        break;
                    case SectionEndCommand s2:
                        s2.Commands = coms;
                        s2.SectionStartIndexReal=coms.LastIndexOf(s2.SectionStart,i );
                        break;
                }
            }
            ref var ip = ref instructionPointer;
            ref var ib = ref buffer;
            ref var id = ref dpRef;
            while (instructionPointer < coms.Count )
            {
                 coms[instructionPointer].Run(ref ip,ref ib, ref id);
                 instructionPointer++;
            }
        }
     



    }

   
}