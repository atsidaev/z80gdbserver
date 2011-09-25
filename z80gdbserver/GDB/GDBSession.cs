using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

using ZXMAK2.Engine.Z80;

namespace z80gdbserver
{
	public class GDBSession
	{
		public static class StandartAnswers
		{
			public const string Empty = "";
			public const string OK = "OK";
			public const string Error = "E00";
			public const string Breakpoint = "T05";
			public const string HaltedReason = "T05thread:00;";
		}
		
		IEmulator emulator;
		
		public GDBSession(IEmulator emulator)
		{
			this.emulator = emulator;
		}
		
		#region Register stuff
		public enum RegisterSize { Byte, Word };
		
		// GDB regs order:
		// "a", "f", "bc", "de", "hl", "ix", "iy", "sp", "i", "r",
		// "ax", "fx", "bcx", "dex", "hlx", "pc"
		
		static RegisterSize[] registerSize = new RegisterSize[] {
			RegisterSize.Byte, RegisterSize.Byte,
			RegisterSize.Word, RegisterSize.Word,
			RegisterSize.Word, RegisterSize.Word,
			RegisterSize.Word, RegisterSize.Word,
			RegisterSize.Byte, RegisterSize.Byte,
			RegisterSize.Byte, RegisterSize.Byte,
			RegisterSize.Word, RegisterSize.Word,
			RegisterSize.Word, RegisterSize.Word
		};
		
		Action<REGS, ushort>[] regSetters = new Action<REGS, ushort>[] {
			(r, v) => r.A = (byte)v,
			(r, v) => r.F = (byte)v,
			(r, v) => r.BC = v,
			(r, v) => r.DE = v,
			(r, v) => r.HL = v,
			(r, v) => r.IX = v,
			(r, v) => r.IY = v,
			(r, v) => r.SP = v,
			(r, v) => r.I = (byte)v,
			(r, v) => r.R = (byte)v,
			(r, v) => r._AF = (ushort)((r._AF & 0x00FF) | ((v & 0xFF) << 8)),
			(r, v) => r._AF = (ushort)((r._AF & 0xFF) | (v & 0xFF)),
			(r, v) => r._BC = v,
			(r, v) => r._DE = v,
			(r, v) => r._HL = v,
			(r, v) => r.PC = v
		};
		
		Func<REGS, int>[] regGetters = new Func<REGS, int>[] {
			r => r.A,
			r => r.F,
			r => r.BC,
			r => r.DE,
			r => r.HL,
			r => r.IX,
			r => r.IY,
			r => r.SP,
			r => r.I,
			r => r.R,
			r => r._AF >> 8,
			r => r._AF & 0xFF,
			r => r._BC,
			r => r._DE,
			r => r._HL,
			r => r.PC
		};
		
		static public int RegistersCount { get { return registerSize.Length; } }
		public static RegisterSize GetRegisterSize(int i)
		{
			return registerSize[i];
		}

		public string GetRegisterAsHex(int reg)
		{
			int result = regGetters[reg](emulator.GetCPU().regs);
			if (registerSize[reg] == RegisterSize.Byte)
				return ((byte)(result)).ToLowEndianHexString();
			else
				return ((ushort)(result)).ToLowEndianHexString();
		}
		
		public bool SetRegister(int reg, string hexValue)
		{
			int val = 0;
			if (hexValue.Length == 4)
				val = Convert.ToUInt16(hexValue.Substring(0, 2), 16) | (Convert.ToUInt16(hexValue.Substring(2, 2), 16) << 8);
			else
				val = Convert.ToUInt16(hexValue, 16);
				
			regSetters[reg](emulator.GetCPU().regs, (ushort)val);
			
			return true;
		}
		
		#endregion
		
		static public string FormatResponse(string response)
		{
			return "+$" + response + "#" + GDBPacket.CalculateCRC(response);
		}
		
		public string ParseRequest(GDBPacket packet)
		{
			string result = StandartAnswers.Empty;
			
			switch(packet.CommandName)
			{
			case '\0': // Command is empty ("+" in 99.99% cases)
				return null;
			case 'q':
				result = GeneralQueryResponse(packet); break;
			case 'Q':
				result = GeneralQueryResponse(packet); break;
			case '?':
				result = GetTargetHaltedReason(packet); break;
			case '!': // extended connection
				break;
			case 'g': // read registers
				result = ReadRegisters(packet); break;
			case 'G': // write registers
				result = WriteRegisters(packet); break;
			case 'm': // read memory
				result = ReadMemory(packet);break;
			case 'M': // write memory
				result = WriteMemory(packet);break;
			case 'X': // write memory binary
				result = StandartAnswers.OK;
				break;
			case 'p': // get single register
				result = GetRegister(packet);break;
			case 'P': // set single register
				result = SetRegister(packet); break;
			case 'v': // some requests, mainly vCont
				result = ExecutionRequest(packet);break;
			case 's': //stepi
				emulator.GetCPU().ExecCycle();
				result = "T05";
				break;
			case 'z': // remove bp
				result = RemoveBreakpoint(packet);
				break;
			case 'Z': // insert bp
				result = SetBreakpoint(packet);
				break;
			case 'k': // Kill the target
				break;
			case 'H': // set thread
				result = StandartAnswers.OK; // we do not have threads, so ignoring this command is OK
				break;
			case 'c': // continue
				emulator.Run();
				result = null;
				break;
			case 'D': // Detach from client
				emulator.Run();
				result = StandartAnswers.OK;
				break;
			}
			
			if (result == null)
				return "+";
			else
				return FormatResponse(result);
		}
		
		string GeneralQueryResponse(GDBPacket packet)
		{
			string command = packet.GetCommandParameters()[0];
			if (command.StartsWith("Supported"))
				return "PacketSize=ffff";
			if (command.StartsWith("C"))
				return StandartAnswers.Empty;
			if (command.StartsWith("Attached"))
				return "1";
			if (command.StartsWith("TStatus"))
				return StandartAnswers.Empty;
			return StandartAnswers.OK;
		}

		string GetTargetHaltedReason(GDBPacket packet)
		{
			return StandartAnswers.HaltedReason;
		}
		
		string ReadRegisters(GDBPacket packet)
		{
			return String.Join("", Enumerable.Range(0, RegistersCount - 1).Select(i => GetRegisterAsHex(i)).ToArray());
		}
		
		string WriteRegisters(GDBPacket packet)
		{
			string regsData = packet.GetCommandParameters()[0];
			for (int i = 0, pos = 0; i < RegistersCount; i++)
			{
				int currentRegisterLength = GetRegisterSize(i) == RegisterSize.Word ? 4 : 2;
				SetRegister(i, regsData.Substring(pos, currentRegisterLength));
				pos += currentRegisterLength;
			}
			return StandartAnswers.OK;
		}
		
		string GetRegister(GDBPacket packet)
		{
			return GetRegisterAsHex(Convert.ToInt32(packet.GetCommandParameters()[0], 16));
		}
		
		string SetRegister(GDBPacket packet)
		{
			string[] parameters = packet.GetCommandParameters()[0].Split(new char[] { '=' });
			if (SetRegister(Convert.ToInt32(parameters[0], 16), parameters[1]))
					return StandartAnswers.OK;
				else
					return StandartAnswers.Error;
		}
		
		string ReadMemory(GDBPacket packet)
		{
			string[] parameters = packet.GetCommandParameters();
			var addr = Convert.ToUInt16(parameters[0], 16);
			if (int.Parse(parameters[1]) == 1)
				return emulator.GetCPU().RDMEM(addr).ToLowEndianHexString();
			else
				if (int.Parse(parameters[1]) == 2)
					return ((ushort)(emulator.GetCPU().RDMEM(addr) + (emulator.GetCPU().RDMEM((ushort)(addr + 1)) << 8))).ToLowEndianHexString();
			else
				return StandartAnswers.Error;
		}
			
		string WriteMemory(GDBPacket packet)
		{
			string[] parameters = packet.GetCommandParameters();
			ushort addr = Convert.ToUInt16(parameters[0], 16);
			if (int.Parse(parameters[1]) == 1)
				emulator.GetCPU().WRMEM(addr, (byte)Convert.ToUInt16(parameters[2], 16));
			else
				if (int.Parse(parameters[1]) == 2)
				{
					emulator.GetCPU().WRMEM(addr, (byte)Convert.ToUInt16(parameters[2].Substring(0, 2), 16));
					emulator.GetCPU().WRMEM((ushort)(addr + 1), (byte)Convert.ToUInt16(parameters[2].Substring(2, 2), 16));
				}
				else
					return StandartAnswers.Error;
			
			return StandartAnswers.OK;
		}
		
		string ExecutionRequest(GDBPacket packet)
		{
			string command = packet.GetCommandParameters()[0];
			if (command.StartsWith("Cont?"))
				return "";
			if (command.StartsWith("Cont"))
			{
				
			}
			return StandartAnswers.Empty;
		}
		
		string SetBreakpoint(GDBPacket packet)
		{
			string[] parameters = packet.GetCommandParameters();
			emulator.SetBreakpoint(Breakpoint.GetBreakpointType(int.Parse(parameters[0])), Convert.ToUInt16(parameters[1], 16));
			return StandartAnswers.OK;
		}
		
		string RemoveBreakpoint(GDBPacket packet)
		{
			string[] parameters = packet.GetCommandParameters();
			emulator.RemoveBreakpoint(Breakpoint.GetBreakpointType(int.Parse(parameters[0])), Convert.ToUInt16(parameters[1], 16));
			return StandartAnswers.OK;
		}
	}
}
