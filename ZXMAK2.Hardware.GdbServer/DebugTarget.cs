using System;
using System.Collections.Generic;
using System.Text;

using z80gdbserver;
using z80gdbserver.Interfaces;

using ZXMAK2.Engine.Cpu.Processor;
using ZXMAK2.Engine.Interfaces;

namespace ZXMAK2.Hardware.GdbServer
{
	/// <summary>
	/// Bridge between ZXMAK2 and z80gdbserver
	/// </summary>
	public class DebugTarget : IDebugTarget
	{
		private readonly IDebuggable _emulator;
		private readonly GDBExternalJtagDevice _jtagDevice;

		public DebugTarget(IDebuggable emulator, GDBExternalJtagDevice jtagDevice)
		{
			_emulator = emulator;
			_jtagDevice = jtagDevice;
		}

		public Z80Cpu CPU => _emulator.CPU;
		public void DoRun() => _emulator.DoRun();
		public void DoStop() => _emulator.DoStop();
		public void AddBreakpoint(Breakpoint.BreakpointType type, ushort addr)
		{
			if (type == Breakpoint.BreakpointType.Execution)
				_emulator.AddBreakpoint(new ZXMAK2.Engine.Entities.Breakpoint(addr));
			else
				_jtagDevice.AddBreakpoint(type, addr);
		}

		public void RemoveBreakpoint(Breakpoint.BreakpointType type, ushort addr)
		{
			if (type == Breakpoint.BreakpointType.Execution)
				_emulator.RemoveBreakpoint(new ZXMAK2.Engine.Entities.Breakpoint(addr));
			else
				_jtagDevice.RemoveBreakpoint(addr);
		}
		public void ClearBreakpoints() => _emulator.ClearBreakpoints();

		// Logging
		public Action<string> LogError => (s =>Logger.Error(s));
		public Action<Exception> LogException => Logger.Error;
		public Action<string> Log => (s =>Logger.Info(s));
	}
}
