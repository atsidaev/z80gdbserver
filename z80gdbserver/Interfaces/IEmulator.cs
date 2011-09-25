using System;

using ZXMAK2.Engine.Z80;

namespace z80gdbserver
{
	public interface IEmulator
	{
		Z80CPU GetCPU();
		void Pause();
		void Run();
		
		void SetBreakpoint(Breakpoint.BreakpointType type, ushort addr);
		void RemoveBreakpoint(Breakpoint.BreakpointType type, ushort addr);
		
		event Breakpoint.BreakPointEventHandler OnBreakpoint;
	}
}

