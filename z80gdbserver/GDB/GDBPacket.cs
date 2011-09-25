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

