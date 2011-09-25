using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace z80gdbserver
{
	class MainClass
	{
		public static void Main (string[] args)
		{
			byte[] file = null;
			if (args.Count() > 0)
				file = File.ReadAllBytes(args[0]);
			
			IEmulator emulator = new TestEmulator(file);
			IDebugBridge gdb = new GNUDebugBridge();
			gdb.Initialize(emulator);
			if (!args.Any(a => a == "/p"))
				emulator.Run();
			
			// Infinite loop
			while(true) { }
		}
	}
}

