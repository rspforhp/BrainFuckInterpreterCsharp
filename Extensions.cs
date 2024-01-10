namespace ConsoleApp1
{
    public static class Extensions
    {
        public static T[] RepeatAmountBeforeOther<T>(this Span<T> l, int index, Func<T,bool> till)
        {
            T el = l[index];
            List<T> te = new List<T>();
            int i = 0;
            for (;till( l[index+i]); i++)
            {
                te.Add(l[index+i]);
            }

            return te.ToArray();
        }

        public enum OT
        {
            NO,
            SETZERO,
            FINDNZ,
        }
        
        public class FindZCommand : Program.BrainFckCommand
        {
            public override string GetDebugString()
            {
                return $"find nz by {Value}";
            }

            public int Value;
            public override unsafe void Run(ref int instructionPointer, ref Span<byte> buffer,  ref byte* dataPointer)
            {
                if (*dataPointer == 0) return;
                gt:
                (dataPointer) += Value;
                if (*dataPointer != 0) goto gt;

            }

            public override Program.ComType[] Types => new Program.ComType[]{   };
     
            public override void Optimise(ref Span<Program.Com> commands, ref int index, IReadOnlyList<Program.BrainFckCommand> readOnlyCommands)
            {
            
            }

            public static bool TryMakeFindZCommand(ref List<Program.BrainFckCommand> coms, ref int index, out FindZCommand command)
            {
                if (coms[index] is Program.SectionStartCommand sec)
                {
                    if(coms[index + 1] is Program.MoveDataPointerCommand v && coms[index + 2] is Program.SectionEndCommand)
                    {
                        command = new FindZCommand(sec,v.MoveAmount );
                        index += 2;
                        return true;
                    }
                }
                command = null;
                return false;
            }

            public FindZCommand(Program.SectionStartCommand sec, int value) : base(sec)
            {
                Value = value;
            }
            public FindZCommand(ref Program.Com command, int value) : base(ref command)
            {
                Value = value;
            }
        }
        

        public class ZeroDataCommand : Program.BrainFckCommand
        {
            public override string GetDebugString()
            {
                return $"dat = 0";
            }
            public override unsafe void Run(ref int instructionPointer, ref Span<byte> buffer,  ref byte* dataPointer)
            {
                (*dataPointer) = (byte)0;
            }

            public override Program.ComType[] Types => new Program.ComType[]{   };
     
            public override void Optimise(ref Span<Program.Com> commands, ref int index, IReadOnlyList<Program.BrainFckCommand> readOnlyCommands)
            {
            
            }

            public static bool TryMakeZeroDataCommand(ref List<Program.BrainFckCommand> coms, ref int index, out ZeroDataCommand command)
            {
                if (coms[index] is Program.SectionStartCommand sec)
                {
                    if(coms[index + 1] is Program.AddToDataPointerCommand { AddAmount: -1 } && coms[index + 2] is Program.SectionEndCommand)
                    {
                        command = new ZeroDataCommand(sec);
                        index += 2;
                        return true;
                    }
                }
                command = null;
                return false;
            }

            public ZeroDataCommand(Program.SectionStartCommand sec) : base(sec)
            {
            
            }
            public ZeroDataCommand(ref Program.Com command) : base(ref command)
            {
            }
        }

        public static List<Program.BrainFckCommand> ExtraOptimize(this List<Program.BrainFckCommand> list,OT t)
        {
            var l = new List<Program.BrainFckCommand>();
            switch (t)
            {
                case OT.NO: return list;
                case OT.SETZERO:
                {
                    for (int i = 0; i < list.Count; i++)
                    {
                        l.Add(ZeroDataCommand.TryMakeZeroDataCommand(ref list, ref i, out var z) ? z : list[i]);
                    }
                    break;
                }
                case OT.FINDNZ:
                {
                    for (int i = 0; i < list.Count; i++)
                    {
                        l.Add(FindZCommand.TryMakeFindZCommand(ref list, ref i, out var z) ? z : list[i]);
                    }
                    break;
                }
                default:
                    throw new ArgumentOutOfRangeException(nameof(t), t, null);
            }

            return l;
        }

    }
}