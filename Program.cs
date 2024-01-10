using System.Collections;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.Json.Serialization;
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

    public record Temp(IntPtr index, byte oldV, byte newV);

    public static unsafe void Main(string[] args)
    {
        var watch = new Stopwatch();
        watch.Start();
        var filePath = args[0].Replace("\"", "");
        if (!filePath.EndsWith(".bf")
            || !File.Exists(filePath))
            throw new Exception($"File isn't a .bf file or doesn't exist.");

        var UniqueLoopsPresent = new HashSet<string>();

        // Pre running
        using var inputStream = File.OpenText(filePath);
        ReadOnlySpan<char> inputString = inputStream.ReadToEnd().ReplaceLineEndings("");
        Span<Com> commands = stackalloc Com[inputString.Length];
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

        //Running
        var instructionPointer = 0;
        Span<byte> buffer = stackalloc byte[30000];
        fixed (byte* spanBegginning = buffer)
        {
            var dataPointer = spanBegginning;
            while (instructionPointer < inputString.Length)
            {
                 var currentCommand = commands[instructionPointer];

                byte i;
                switch (currentCommand.Type)
                {
                    case ComType.incDat:
                        i = 1;
                        while (instructionPointer + i < inputString.Length &&
                               commands[i + instructionPointer].Type == ComType.incDat)
                            i++;

                        dataPointer += i;
                        instructionPointer += i;
                        break;
                    case ComType.decDat:
                        i = 1;
                        while (instructionPointer + i < inputString.Length &&
                               commands[i + instructionPointer].Type == ComType.decDat)
                            i++;

                        dataPointer -= i;
                        instructionPointer += i;
                        break;
                    case ComType.incAt:
                        i = 1;

                        while (instructionPointer + i < inputString.Length &&
                               commands[i + instructionPointer].Type == ComType.incAt)
                            i++;


                        (*dataPointer) += i;
                        instructionPointer += i;
                        break;
                    case ComType.decAt:
                        i = 1;
                        while (instructionPointer + i < inputString.Length &&
                               commands[i + instructionPointer].Type == ComType.decAt)
                            i++;

                        (*dataPointer) -= i;
                        instructionPointer += i;
                        break;
                    case ComType.output:
                        var a = (char)*dataPointer;
                        Task.Run(async () => Console.Write(a));
                        instructionPointer++;
                        break;
                    case ComType.input:
                        *dataPointer = (byte)Console.ReadLine()[0];
                        instructionPointer++;
                        break;
                    case ComType.jmpFow:
                    {
                      

                        if (commands[instructionPointer + 1].Type == ComType.decAt &&
                            commands[instructionPointer + 2].Type == ComType.jmpBak)
                        {
                            *dataPointer = 0;
                            instructionPointer = currentCommand.Jmp+1;
                            
                            break;
                        }


                        /*
                        var ab = commands[instructionPointer + 1].Type;
                        if (ab is ComType.incDat or ComType.decDat)
                        {
                            var ib = 0;
                            while (commands[ib + instructionPointer+1].Type ==ab) ib+= ab==ComType.incDat?1: -1;

                            if (commands[ib + instructionPointer+1].Type == ComType.jmpBak)
                            {
                                dataPointer += ib;

                                while (*dataPointer != 0) dataPointer += ib;

                                instructionPointer =  currentCommand.Jmp+1 ;
                                break;
                            }
                        }
                        */
                        if (*dataPointer == 0) instructionPointer = currentCommand.Jmp;

                        instructionPointer++;
                        break;
                    }
                    case ComType.jmpBak:
                    {
                        if (*dataPointer != 0)
                            instructionPointer = currentCommand.Jmp;
                        else
                            /*    List<Temp> values = new ();
                                foreach (var kep in Pair1[instructionPointer])
                                {
                                    var newVal = *((byte*)kep);
                                    byte oldVal = (byte)(Pair2[instructionPointer] );
                                    if(newVal!=oldVal)
                                        values.Add( new((IntPtr)(((byte*)kep.ToPointer())-spanBegginning),oldVal,newVal));
                                }*/
                            //Console.WriteLine($"{string.Join("", commands.Slice(currentCommand.Jmp, instructionPointer-currentCommand.Jmp+1).ToArray())}" + $"\n" + $"{JsonConvert.SerializeObject(values)}" + $"\n DONE!");
                            UniqueLoopsPresent.Add(
                                $"{string.Join("", commands.Slice(currentCommand.Jmp, instructionPointer - currentCommand.Jmp + 1).ToArray())}");


                        instructionPointer++;
                    }
                        break;
                    case ComType.none:
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        foreach (var s in UniqueLoopsPresent)
        {
                Console.WriteLine(s);
        }


        Console.WriteLine(watch.Elapsed);
    }
}