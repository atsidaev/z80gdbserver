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
using ZXMAK2.Engine.Interfaces;

namespace z80gdbserver
{
	public class GDBJtagDevice : IBusDevice, IJtagDevice
	{
		GDBNetworkServer server;
		IDebuggable emulator;

		public void Attach(IDebuggable dbg)
		{
			emulator = dbg;
			emulator.Breakpoint += OnBreakpoint;

			server = new GDBNetworkServer(emulator);
		}

		public void Detach()
		{
			server.Dispose();
		}

		public void BusConnect()
		{
		}

		public void BusDisconnect()
		{
		}

		public void BusInit(IBusManager bmgr)
		{
		}

		private int m_busOrder = 0;
		public int BusOrder
		{
			get { return m_busOrder; }
			set { m_busOrder = value; }
		}

		public BusCategory Category
		{
			get
			{
				return BusCategory.Other;
			}
		}

		public string Description
		{
			get { return "Interface for interaction with gdb debugger "; }
		}

		public string Name
		{
			get { return "GNU Debugger Interface"; }
		}

		void OnBreakpoint(object sender, EventArgs args)
		{
			server.Breakpoint(new Breakpoint(Breakpoint.BreakpointType.Execution, emulator.CPU.regs.PC));
		}
	}
}

