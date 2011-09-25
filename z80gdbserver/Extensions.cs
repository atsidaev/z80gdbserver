using System;
namespace z80gdbserver
{
	public static class Extensions
	{
		public static string ToLowEndianHexString(this byte number)
		{
			return number.ToString("X").PadLeft(2, '0');
		}
		
		public static string ToLowEndianHexString(this ushort number)
		{
			return ((byte)(number & 0xFF)).ToLowEndianHexString() + ((byte)(number >> 8)).ToLowEndianHexString();
		}
	}
}

