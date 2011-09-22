using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace z80gdbserver
{
	class MainClass
	{
		static TcpListener listener;

		public static void Main (string[] args)
		{
			listener = new TcpListener (2000);
			listener.Start ();
			
			while (true) {
				//blocks until a client has connected to the server
				TcpClient client = listener.AcceptTcpClient();
				
				//create a thread to handle communication
				//with connected client
				Thread clientThread = new Thread (new ParameterizedThreadStart (HandleClientComm));
				clientThread.Start (client);
			}
		}

		private static void HandleClientComm (object client)
		{
			TcpClient tcpClient = (TcpClient)client;
			NetworkStream clientStream = tcpClient.GetStream ();
			
			byte[] message = new byte[4096];
			int bytesRead;
			
			while (true) {
				bytesRead = 0;
				
				try {
					//blocks until a client sends a message
					bytesRead = clientStream.Read (message, 0, 4096);
				} catch {
					//a socket error has occured
					break;
				}
				
				if (bytesRead == 0) {
					//the client has disconnected from the server
					break;
				}
				
				//message has successfully been received
				ASCIIEncoding encoder = new ASCIIEncoding ();
				string request = encoder.GetString(message, 0, bytesRead);
				if (bytesRead > 0)
				{
					Console.WriteLine("--> " + request);
					string response = ParseRequest(request);
					if (response != null)
					{
						Console.WriteLine("<-- " + response);
						byte[] bytes = encoder.GetBytes(response);
						clientStream.Write(bytes, 0, bytes.Length);
					}
				}
				//
				//Console.WriteLine ();
			}
			
			tcpClient.Close ();
		}
		
		static bool noAckMode = false;
		
		static string GeneralQueryResponse(string request)
		{
			if (request.StartsWith("qSupported"))
				return "PacketSize=3fff";
			if (request.StartsWith("qC"))
				return "";
			if (request.StartsWith("qAttached"))
				return "1";
			if (request.StartsWith("qTStatus"))
				return "";
			return "OK";
		}

		static string GetTargetHaltedReason(string request)
		{
			return "T05thread:01;";
		}
		
		static string CalculateCRC(string str)
		{
			ASCIIEncoding encoder = new ASCIIEncoding();
			byte[] bytes = encoder.GetBytes(str);
			int crc = 0;
			for (int i = 0; i < bytes.Length; i++)
				crc += bytes[i];
			
			return (crc & 0xff).ToString("X").PadLeft(2, '0').ToLowerInvariant();
		}
		
		static string ReadRegisters(string request)
		{
			// "a", "f", "bc", "de", "hl", "ix", "iy", "sp", "i", "r",
			// "ax", "fx", "bcx", "dex", "hlx", "pc"

			return "0102030405060708090A0B0C0D0E0F101112131415161718191A";
		}
		
		static string ReadMemory(string request)
		{
			var parts = request.Split(new char[] {',', '#'});
			return String.Empty.PadLeft(2 * int.Parse(parts[1]), 'F');
		}
			
		static string ParseRequest(string request)
		{
			string result = "";
			
			Regex removePrefix = new Regex(@"^[\+\$]+", RegexOptions.Compiled);
			request = removePrefix.Replace(request, "");
			if (String.IsNullOrEmpty(request))
				return null;
			
			switch(request[0])
			{
			case 'q':
				result = GeneralQueryResponse(request); break;
			case 'Q':
				result = GeneralQueryResponse(request); break;
			case '?':
				result = GetTargetHaltedReason(request); break;
			case '!':
				
			case 'D': // Detach from client
				result = "OK"; break;
			case 'g':
				result = ReadRegisters(request); break;
			case 'G':
				result = "OK"; break;
			case 'm':
				result = ReadMemory(request);break;
			/*case 'H': // set thread
			case 'k': // Kill the target
			case 'm':
			case 'M':
			case 'p':
			case 'P':
			case 'v':
			case 'X':
			case 'z':
			case 'Z':
				break;*/
			default:
				result = "OK"; break;
			}
			
			return (noAckMode ? "" : "+") + "$" + result + "#" + CalculateCRC(result);
		}
	}
}

