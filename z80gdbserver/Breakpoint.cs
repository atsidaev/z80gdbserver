using System;
namespace z80gdbserver
{
	public class Breakpoint
	{
		public enum BreakpointType { Execution, Read, Write, Access };
		public delegate void BreakPointEventHandler(Breakpoint breakpoint);
		
		public BreakpointType Type { get; set; }
		public ushort Address { get; set; }
		
		static public BreakpointType GetBreakpointType(int type)
		{
			switch(type)
			{
			case 0:
			case 1:
				return BreakpointType.Execution;
			case 2:
				return BreakpointType.Write;
			case 3:
				return BreakpointType.Read;
			case 4:
				return BreakpointType.Access;
			}
			
			throw new Exception("Incorrect parameter passed");
		}
		
		public Breakpoint(BreakpointType type, ushort address)
		{
			this.Type = type;
			this.Address = address;
		}
	}
}

