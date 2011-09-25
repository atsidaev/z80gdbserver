/*
 * Copyright 2011 Alexander Tsidaev
 * 
 * This file is part of z80gdbserver.
 * z80gdbserver is free software: you can redistribute it and/or modify it under the
 * terms of the GNU General Public License as published by the Free Software Foundation,
 * either version 3 of the License, or (at your option) any later version.
 * 
 * z80gdbserver is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY;
 * without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.
 * See the GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License along with z80gdbserver. 
 * If not, see http://www.gnu.org/licenses/.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

using ZXMAK2.Engine.Z80;

namespace z80gdbserver
{
	public class TestEmulator : IEmulator
	{
		// If no program specified then
		// memory contains hardcoded program
		// 		ORG 0
		//		ld c,0xff
		// LOOP:
		//		dec c
		//		jp nz,LOOP
		//		jp 0x0000
		byte[] sampleProgramm = new byte[] { 0x0E, 0xFF, 0x0D, 0xC2, 0x02, 0x00, 0xC3, 0x00, 0x00};	
		
		byte[] rom;
		byte[] ram = new byte[65536];
		
		public Z80CPU cpu = new ZXMAK2.Engine.Z80.Z80CPU();
		List<Breakpoint> breakpoints = new List<Breakpoint>();
		
		private bool pause = false;
		
		public byte ReadMemory(ushort addr)
		{
			var bp = breakpoints.FirstOrDefault(
				b => (b.Type == Breakpoint.BreakpointType.Read || b.Type == Breakpoint.BreakpointType.Access) && b.Address == addr);
			if (bp != null)
			{
				pause = true;
				if (OnBreakpoint != null)
					OnBreakpoint(bp);
			}
			
			if (addr < rom.Length)
				return rom[addr];
			else
				return ram[addr];
		}
		
		public void WriteMemory(ushort addr, byte value)
		{
			var bp = breakpoints.FirstOrDefault(
				b => (b.Type == Breakpoint.BreakpointType.Write || b.Type == Breakpoint.BreakpointType.Access) && b.Address == addr);
			if (bp != null)
			{
				pause = true;
				if (OnBreakpoint != null)
					OnBreakpoint(bp);
			}
			
			if (addr < rom.Length)
				return;
			else
				ram[addr] = value;
		}
		
		public TestEmulator(byte[] program)
		{
			if (program != null)
				rom = program;
			else
				rom = sampleProgramm;
			
			cpu.RDMEM = new OnRDBUS(ReadMemory);
			cpu.WRMEM = new OnWRBUS(WriteMemory);
			cpu.RDMEM_M1 = new OnRDBUS(ReadMemory);
			
			// Ignoring port activity
			cpu.RDPORT = delegate { return 0xFF; };
			cpu.WRPORT = delegate {};
			
			pause = true;
			
			Thread zxThread = new Thread(EmulatorLoop);
			zxThread.Start(null);
		}
	
		private void EmulatorLoop(object param)
		{
			while (true)
			{
				if (!pause)
				{
					cpu.ExecCycle();
					
					var bp = breakpoints.FirstOrDefault(b => b.Type == Breakpoint.BreakpointType.Execution && b.Address == cpu.regs.PC);
					if (bp != null)
					{
						pause = true;
						if (OnBreakpoint != null)
							OnBreakpoint(bp);
					}
				}
				
				// Slowing down
				Thread.Sleep(50);
			}
		}

		public Z80CPU GetCPU()
		{
			return cpu;
		}

		public void Pause()
		{
			pause = true;
		}

		public void Run()
		{
			pause = false;
		}
	
		public void SetBreakpoint(Breakpoint.BreakpointType type, ushort addr)
		{
			if (!breakpoints.Any(b => b.Address == addr && b.Type == type))
				breakpoints.Add(new Breakpoint(type, addr));
		}

		public void RemoveBreakpoint(Breakpoint.BreakpointType type, ushort addr)
		{
			breakpoints.RemoveAll(b => b.Address == addr && b.Type == type);
		}

		public event Breakpoint.BreakPointEventHandler OnBreakpoint;
	}
}

