using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

using ZXMAK2.Engine.Z80;

namespace z80gdbserver
{
	public class GNUDebugBridge : IDebugBridge
	{
		GDBNetworkServer server;
		
		public void Initialize (IEmulator emulator)
		{
			emulator.OnBreakpoint += OnBreakpoint;
			
			server = new GDBNetworkServer(emulator);
		}
		
		void OnBreakpoint(Breakpoint breakpoint)
		{
			server.Breakpoint(breakpoint);
		}
	}
}

