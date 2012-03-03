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
using ZXMAK2.Engine.Interfaces;

namespace z80gdbserver
{
	public class GDBNetworkServer : IDisposable
	{
		ASCIIEncoding encoder = new ASCIIEncoding();

		IDebuggable emulator;
		GDBJtagDevice jtagDevice;

		TcpListener listener;
		Thread socketListener;
		List<TcpClient> clients = new List<TcpClient>();

		bool log = true;

		public GDBNetworkServer(IDebuggable emulator, GDBJtagDevice jtagDevice)
		{
			this.emulator = emulator;
			this.jtagDevice = jtagDevice;
	
			listener = new TcpListener(IPAddress.Any, 2000);
			listener.Start ();
			
			socketListener = new Thread(ListeningThread);
			socketListener.Start();
		}
		
		public void Breakpoint(Breakpoint breakpoint)
		{
			// emulator.IsRunning= false;

			// We do not need old breakpoints because GDB will set them again
			emulator.ClearBreakpoints();
			jtagDevice.ClearBreakpoints();

			SendGlobal(GDBSession.FormatResponse(GDBSession.StandartAnswers.Breakpoint));
		}

		private void SendGlobal(string message)
		{
			foreach (var client in clients.Where(c => c.Connected))
			{
				var stream = client.GetStream();
				if (stream != null)
					SendResponse(stream, message);
			}
		}
		
		private void ListeningThread(object obj)
		{
			try
			{
				while (true)
				{
					TcpClient client = listener.AcceptTcpClient();

					clients.Add(client);
					clients.RemoveAll(c => !c.Connected);

					Thread clientThread = new Thread(GDBClientConnected);
					clientThread.Start(client);
				}
			}
			catch
			{
				// Here can be an exception because we interrupting blocking AcceptTcpClient()
				// call on Dispose. We should not fail here, so try/catching
			}
		}
		
		private void GDBClientConnected(object client)
		{
			TcpClient tcpClient = (TcpClient)client;
			NetworkStream clientStream = tcpClient.GetStream();
			GDBSession session = new GDBSession(emulator, jtagDevice);

			byte[] message = new byte[0x1000];
			int bytesRead;

			// log = new StreamWriter("c:\\temp\\log.txt");
			// log.AutoFlush = true;

			emulator.DoStop();
			
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
					if (log) 
						ZXMAK2.Logging.Logger.GetLogger().LogTrace("--> " + packet.ToString());

					bool isSignal;
					string response = session.ParseRequest(packet, out isSignal);
					if (response != null)
					{
						if (isSignal)
							SendGlobal(response);
						else
							SendResponse(clientStream, response);
					}
				}
			}
			tcpClient.Close ();
		}
		
		void SendResponse(Stream stream, string response)
		{
			if (log) 
				ZXMAK2.Logging.Logger.GetLogger().LogTrace("<-- " + response);

			byte[] bytes = encoder.GetBytes(response);
			stream.Write(bytes, 0, bytes.Length);	
		}

		public void Dispose()
		{
			if (socketListener != null)
			{
				listener.Stop();

				foreach (var client in clients)
					if (client.Connected)
						client.Close();

				socketListener.Abort();
			}
		}
	}
}

