using System;
using System.Text;
using System.Text.RegularExpressions;

namespace z80gdbserver
{
	public class GDBPacket
	{
		private byte[] message;
		private int length;
		private string text;
		
		char commandName;
		string[] parameters;
		
		static Regex removePrefix = new Regex(@"^[\+\$]+", RegexOptions.Compiled);
		
		public GDBPacket(byte[] message, int length)
		{
			this.message = message;
			this.length = length;
			
			ASCIIEncoding encoder = new ASCIIEncoding();
			text = encoder.GetString(message, 0, length);
			
			string request = removePrefix.Replace(text, "");
			if (String.IsNullOrEmpty(request))
				commandName = '\0';
			else
			{
				commandName = request[0];
				parameters = request.Substring(1).Split(new char[] { ',', '#', ':', ';' });
			}
		}
		
		public override string ToString ()
		{
			return text;
		}
		
		public byte[] GetBytes()
		{
			return message;
		}
		
		public int Length
		{
			get { return length; }
		}
		
		public char CommandName
		{
			get { return commandName; }
		}
		
		public string[] GetCommandParameters()
		{
			return parameters;
		}
		
		static public string CalculateCRC(string str)
		{
			ASCIIEncoding encoder = new ASCIIEncoding();
			byte[] bytes = encoder.GetBytes(str);
			int crc = 0;
			for (int i = 0; i < bytes.Length; i++)
				crc += bytes[i];
			
			return ((byte)crc).ToLowEndianHexString().ToLowerInvariant();
		}
	}
}

