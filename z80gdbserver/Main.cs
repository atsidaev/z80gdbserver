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
			else
			{
				Console.WriteLine("Usage: z80gdbserver [--pause] [filename]");
				Console.WriteLine("Server is using 2000 TCP port");
				Console.WriteLine("filename not specified so using hardcoded example");
				Console.WriteLine("-----");
			}
			
			IEmulator emulator = new TestEmulator(file);
			IDebugBridge gdb = new GNUDebugBridge();
			gdb.Initialize(emulator);
			if (!args.Any(a => a == "--pause"))
				emulator.Run();
			
			// Infinite loop
			while(true) { }
		}
	}
}

