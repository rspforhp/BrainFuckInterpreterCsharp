namespace ConsoleApp1;

public static class Extensions
{
    public static T[] RepeatAmountBeforeOther<T>(this Span<T> l, int index, Func<T,bool> till)
    {
        T el = l[index];
        List<T> te = new List<T>();
        int i = 0;
            for (;index+i < l.Length&&till( l[index+i]); i++)
            {
                te.Add(l[index+i]);
            }

        return te.ToArray();
    }

    public enum OT
    {
        NO,
        SETZERO=0B1,
        //do after SETZERO
        CLEANUPINITS=0b10,
        FINDNZ=0b100,
        SWITCHLOCATION=0b1000,
        ALLUSEFUL=  SETZERO | FINDNZ | CLEANUPINITS | SWITCHLOCATION,
        //after switch location, a pretty questionable optimization
        MULTMOVE,
    }
        
    public class MoveDataPointerMultCommand : Program.BrainFckCommand
    {
        public override unsafe void Run(ref int instructionPointer, ref Span<byte> buffer, ref byte* dataPointer)
        {
            unchecked
            {
                if (*dataPointer == 0) return;
                var am = *dataPointer;
                (*dataPointer) = 0;
                for (int i = 0; i < am; i++)
                {
                    (*(dataPointer+i*MultAmount)) = 0;
                }
                dataPointer += MultAmount*am;

                    
            }
        }

        public override string GetDebugString()
        {
            return $"adr+ {MultAmount}*I";
        }

        public override Program.ComType[] Types => new Program.ComType[]{ Program.ComType.incDat, Program.ComType.decDat };
        public int MultAmount = 0;
     
        public override void Optimise(ref Span<Program.Com> commands, ref int index, IReadOnlyList<Program.BrainFckCommand> readOnlyCommands)
        {
             
        }
         
        public MoveDataPointerMultCommand(ref Program.Com command) : base(ref command)
        {
        }

        public MoveDataPointerMultCommand(Program.SectionStartCommand sec):base(sec)
        {
            
        }
    }

        
    public class ChangeLocationCommand : Program.BrainFckCommand
    {
        public override string GetDebugString()
        {
            return $"move value by {MoveValue}, and set to 0, is smart? {SetTo}";
        }

        public int MoveValue;
        public override unsafe void Run(ref int instructionPointer, ref Span<byte> buffer,  ref byte* dataPointer)
        {
            var oldVal = (*dataPointer);
            if (oldVal == 0)
            {
                if(SetTo) dataPointer += MoveValue;
                return;
            }
            (*dataPointer) = 0;
            if (SetTo)
            {
                dataPointer += MoveValue;
                *(dataPointer) += oldVal;
            }
            else
            {
                *(dataPointer + MoveValue) += oldVal;
            }
        }

        public override Program.ComType[] Types => new Program.ComType[]{   };
     
        public override void Optimise(ref Span<Program.Com> commands, ref int index, IReadOnlyList<Program.BrainFckCommand> readOnlyCommands)
        {
            
        }

        public static bool TryMakeChangeLocationCommand(ref List<Program.BrainFckCommand> coms, ref int index, out ChangeLocationCommand command)
        {
            if (coms[index] is Program.SectionStartCommand sec)
            {
                if(coms[index + 1] is Program.AddToDataPointerCommand { AddAmount:-1} && coms[index + 2] is Program.MoveDataPointerCommand mov&& coms[index + 3] is Program.AddToDataPointerCommand {AddAmount:1}&& coms[index + 4] is Program.MoveDataPointerCommand mov2&& coms[index + 5] is Program.SectionEndCommand)
                {
                    var rev = -mov2.MoveAmount;
                    if (mov.MoveAmount != rev)
                    {
                        command = null;
                        return false;
                    }

                        
                    bool setTo = false;
                    if (coms[index + 6] is Program.MoveDataPointerCommand pointerCommand &&
                        pointerCommand.MoveAmount == mov.MoveAmount)
                    {
                        setTo = true; 
                        index++;
                    }

                    command = new ChangeLocationCommand(sec,mov.MoveAmount,setTo);
                    index += 5;
                    return true;
                }
            }
            command = null;
            return false;
        }

        public bool SetTo;
        public ChangeLocationCommand(Program.SectionStartCommand sec, int moveValue,bool setTo) : base(sec)
        {
            MoveValue = moveValue;
            SetTo = setTo;
        }
        public ChangeLocationCommand() : base(null)
        {
        }
        public ChangeLocationCommand(ref Program.Com command) : base(ref command)
        {
        }
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
        
    public class SetDataCommand : Program.BrainFckCommand
    {
        public override string GetDebugString()
        {
            return $"dat = {Value}";
        }

        public int Value;
        public override unsafe void Run(ref int instructionPointer, ref Span<byte> buffer,  ref byte* dataPointer)
        {
            (*dataPointer) = (byte)Value;
        }

        public override Program.ComType[] Types => new Program.ComType[]{   };
     
        public override void Optimise(ref Span<Program.Com> commands, ref int index, IReadOnlyList<Program.BrainFckCommand> readOnlyCommands)
        {
            
        }


        public SetDataCommand(ZeroDataCommand zd, int value) : base(zd)
        {
            Value = value;
        }
        public SetDataCommand(ref Program.Com command, int value) : base(ref command)
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
            case OT.ALLUSEFUL:
            {
                for (int i = 0; i < list.Count; i++)
                {
                    if (ZeroDataCommand.TryMakeZeroDataCommand(ref list, ref i, out var z1))
                    {
                        l.Add(z1);
                        continue;
                    }
                    if (FindZCommand.TryMakeFindZCommand(ref list, ref i, out var z2))
                    {
                        l.Add(z2);
                        continue;
                    }
                    if (list[i] is ZeroDataCommand zr && list[i + 1] is Program.AddToDataPointerCommand add)
                    {
                        var z3=new SetDataCommand(zr,add.AddAmount);
                        i += 1;
                        l.Add(z3);
                        continue;
                    }

                    if (ChangeLocationCommand.TryMakeChangeLocationCommand(ref list, ref i, out var z4))
                    {
                        l.Add(z4);
                        continue;
                    }
                    l.Add(  list[i]);
                }

                break;
            }
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
            case OT.CLEANUPINITS:
            {
                for (int i = 0; i < list.Count; i++)
                {
                    var v = list[i];
                    if (v is ZeroDataCommand zr && list[i + 1] is Program.AddToDataPointerCommand add)
                    {
                        l.Add(new SetDataCommand(zr,add.AddAmount));
                        i += 1;
                    }
                    else l.Add(v );
                }
                break;
            }
            case OT.SWITCHLOCATION:
            {
                for (int i = 0; i < list.Count; i++)
                {
                    l.Add(ChangeLocationCommand.TryMakeChangeLocationCommand(ref list, ref i, out var z) ? z : list[i]);
                }
                break;
                break;
            }
            case OT.MULTMOVE:
            {
                for (int i = 0; i < list.Count; i++)
                {
                    var v = list[i];
                    if (v is Program.SectionStartCommand zr && list[i + 1] is Program.AddToDataPointerCommand  && list[i + 2] is ChangeLocationCommand { SetTo:true} chg&& list[i + 3] is Program.SectionEndCommand )
                    {
                        l.Add(new MoveDataPointerMultCommand(zr){MultAmount = chg.MoveValue});
                        i += 3;
                    }
                    else l.Add(v );
                }
                break;
            }
            default:
                throw new ArgumentOutOfRangeException(nameof(t), t, null);
        }

        return l;
    }

}