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
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

using ZXMAK2.Engine.Z80;
using ZXMAK2.Engine.Interfaces;

namespace z80gdbserver
{
	public class GNUDebugBridge : IDebugBridge
	{
		GDBNetworkServer server;
		IDebuggable emulator;

		public void Initialize(IDebuggable emulator)
		{
			this.emulator = emulator;

			server = new GDBNetworkServer(emulator);
		}
		

	}
}

