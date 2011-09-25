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
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace z80gdbserver
{
	public class GDBNetworkServer
	{
		ASCIIEncoding encoder = new ASCIIEncoding();
		
		TcpListener listener;
		IEmulator emulator;
		
		List<TcpClient> clients = new List<TcpClient>();
		
		public GDBNetworkServer(IEmulator emulator)
		{
			this.emulator = emulator;
			
			listener = new TcpListener(IPAddress.Any, 2000);
			listener.Start ();
			
			Thread socketListener = new Thread(ListeningThread);
			socketListener.Start();
		}
		
		public void Breakpoint(Breakpoint breakpoint)
		{
			foreach (var client in clients.Where(c => c.Connected))
			{
				var stream = client.GetStream();
				if (stream != null)
					SendResponse(stream, GDBSession.FormatResponse(GDBSession.StandartAnswers.Breakpoint));
			}
		}
		
		private void ListeningThread(object obj)
		{
			while (true) {
				TcpClient client = listener.AcceptTcpClient();
				
				clients.Add(client);
				clients.RemoveAll(c => !c.Connected);
				
				Thread clientThread = new Thread(GDBClientConnected);
				clientThread.Start(client);
			}
		}
		
		private void GDBClientConnected(object client)
		{
			TcpClient tcpClient = (TcpClient)client;
			NetworkStream clientStream = tcpClient.GetStream();
			GDBSession session = new GDBSession(emulator);

			byte[] message = new byte[0x1000];
			int bytesRead;
			
			emulator.Pause();
			
			while (true) {
				bytesRead = 0;
				
				try {
					bytesRead = clientStream.Read(message, 0, 4096);
				} catch {
					//a socket error has occured
					break;
				}
				
				if (bytesRead == 0) {
					//the client has disconnected from the server
					break;
				}
				
				if (bytesRead > 0)
				{
					GDBPacket packet = new GDBPacket(message, bytesRead);
					Console.WriteLine("--> " + packet.ToString());
					string response = session.ParseRequest(packet);
					if (response != null)
					{
						SendResponse(clientStream, response);
					}
				}
			}
			tcpClient.Close ();
		}
		
		void SendResponse(Stream stream, string response)
		{
			Console.WriteLine("<-- " + response);
			byte[] bytes = encoder.GetBytes(response);
			stream.Write(bytes, 0, bytes.Length);	
		}
	}
}

