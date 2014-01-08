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

using ZXMAK2.Engine.Z80;
using ZXMAK2.Entities;
using ZXMAK2.Interfaces;

namespace z80gdbserver
{
	public class GDBJtagDevice : BusDeviceBase, IJtagDevice
	{
		GDBNetworkServer server;
		IDebuggable emulator;
		IBusManager busManager;
		List<Breakpoint> accessBreakpoints = new List<Breakpoint>();

		public void Attach(IDebuggable dbg)
		{
			emulator = dbg;
			emulator.Breakpoint += OnBreakpoint;

			// For memory read/write breakpoints:
			busManager.SubscribeWrMem(0x0000, 0x0000, new BusWriteProc(OnMemoryWrite));
			busManager.SubscribeRdMem(0x0000, 0x0000, new BusReadProc(OnMemoryRead));
			

			server = new GDBNetworkServer(emulator, this);
		}

		public void Detach()
		{
			server.Dispose();
		}

		public override void BusConnect()
		{
		}

		public override void BusDisconnect()
		{
		}

		public override void BusInit(IBusManager bmgr)
		{
			this.busManager = bmgr;
		}

		private int m_busOrder = 0;
		public int BusOrder
		{
			get { return m_busOrder; }
			set { m_busOrder = value; }
		}

		public override BusDeviceCategory Category
		{
			get
			{
				return BusDeviceCategory.Other;
			}
		}

		public override string Description
		{
			get { return "Interface for interaction with gdb debugger "; }
		}

		public override string Name
		{
			get { return "GNU Debugger Interface"; }
		}

		void OnBreakpoint(object sender, EventArgs args)
		{
			server.Breakpoint(new Breakpoint(Breakpoint.BreakpointType.Execution, emulator.CPU.regs.PC));
		}

		void OnMemoryWrite(ushort addr, byte value)
		{
			Breakpoint breakPoint = accessBreakpoints.FirstOrDefault(bp => bp.Address == addr && (bp.Type == Breakpoint.BreakpointType.Write || bp.Type == Breakpoint.BreakpointType.Access));
			if (breakPoint != null)
				server.Breakpoint(breakPoint);
		}

		void OnMemoryRead(ushort addr, ref byte value)
		{
			Breakpoint breakPoint = accessBreakpoints.FirstOrDefault(bp => bp.Address == addr && (bp.Type == Breakpoint.BreakpointType.Read || bp.Type == Breakpoint.BreakpointType.Access));
			if (breakPoint != null)
				server.Breakpoint(breakPoint);
		}

		public void AddBreakpoint(Breakpoint.BreakpointType type, ushort address)
		{
			accessBreakpoints.Add(new Breakpoint(type, address));
		}

		public void RemoveBreakpoint(ushort address)
		{
			accessBreakpoints.RemoveAll(b => b.Address == address);
		}

		public void ClearBreakpoints()
		{
			accessBreakpoints.Clear();
		}
	}
}

