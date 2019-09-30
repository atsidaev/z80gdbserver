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
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using z80gdbserver.Interfaces;

namespace z80gdbserver.Gdb
{
	public class GDBNetworkServer : IDisposable
	{
		private readonly ASCIIEncoding _encoder = new ASCIIEncoding();

		private TcpListener _listener;
		private readonly IDebugTarget _target;
		private readonly int _port;

		private readonly object _clientsLock = new object();
		private readonly List<TcpClient> _clients = new List<TcpClient>();

		public GDBNetworkServer(IDebugTarget target, int port)
		{
			_target = target;
			_port = port;
		}

		public async Task StartServer()
		{
			_listener = new TcpListener(IPAddress.Any, _port);
			_listener.Start();

			await WaitForClients();
		}

		public async Task Breakpoint(Breakpoint breakpoint)
		{
			// We do not need old breakpoints because GDB will set them again
			_target.ClearBreakpoints();

			await SendGlobal(GDBSession.FormatResponse(GDBSession.StandartAnswers.Breakpoint));
		}

		private async Task SendGlobal(string message)
		{
			List<TcpClient> connectedClients;

			lock (_clientsLock)
			{
				connectedClients = _clients.Where(c => c.Connected).ToList();
			}

			await Task.WhenAll(connectedClients.Select(c => SendResponse(c.GetStream(), message)));
		}

		private async Task WaitForClients()
		{
			try
			{
				while (true)
				{
					TcpClient client = await _listener.AcceptTcpClientAsync();

					lock (_clientsLock)
					{
						_clients.Add(client);
						_clients.RemoveAll(c => !c.Connected);
					}

					ProcessGdbClient(client);
				}
			}
			catch(Exception ex)
			{
				_target.LogException?.Invoke(ex);
			}
		}

		private async Task ProcessGdbClient(TcpClient tcpClient)
		{
			NetworkStream clientStream = tcpClient.GetStream();
			GDBSession session = new GDBSession(_target);

			byte[] message = new byte[0x1000];
			int bytesRead;

			_target.DoStop();

			while (true)
			{
				try
				{
					bytesRead = await clientStream.ReadAsync(message, 0, 4096);
				}
				catch (IOException iex)
				{
					var sex = iex.InnerException as SocketException;
					if (sex == null || sex.SocketErrorCode != SocketError.Interrupted)
					{
						_target.LogException?.Invoke(sex);
					}
					break;
				}
				catch (SocketException sex)
				{
					if (sex.SocketErrorCode != SocketError.Interrupted)
					{
						_target.LogException?.Invoke(sex);
					}
					break;
				}
				catch (Exception ex)
				{
					_target.LogException?.Invoke(ex);
					break;
				}

				if (bytesRead == 0)
				{
					//the client has disconnected from the server
					break;
				}

				if (bytesRead > 0)
				{
					GDBPacket packet = new GDBPacket(message, bytesRead);
					_target.Log?.Invoke($"--> {packet}");

					bool isSignal;
					string response = session.ParseRequest(packet, out isSignal);
					if (response != null)
					{
						if (isSignal)
							await SendGlobal(response);
						else
							await SendResponse(clientStream, response);
					}
				}
			}

			tcpClient.Client.Shutdown(SocketShutdown.Both);
		}

		private async Task SendResponse(Stream stream, string response)
		{
			_target.Log?.Invoke($"<-- {response}");

			byte[] bytes = _encoder.GetBytes(response);
			await stream.WriteAsync(bytes, 0, bytes.Length);
		}

		public void Dispose()
		{
			if (_listener != null)
			{
				_listener.Stop();

				lock (_clientsLock)
				{
					foreach (var client in _clients)
						if (client.Connected)
							client.Client.Shutdown(SocketShutdown.Both);
				}

				_listener.Server.Shutdown(SocketShutdown.Both);
			}
		}
	}
}

