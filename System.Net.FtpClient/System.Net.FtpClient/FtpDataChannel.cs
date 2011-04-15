﻿using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Diagnostics;

namespace System.Net.FtpClient {
	public class FtpDataChannel : FtpChannel {
		FtpCommandChannel _cmdChan = null;
		/// <summary>
		/// The command channel that opened this data channel
		/// </summary>
		public FtpCommandChannel CommandChannel {
			get { return _cmdChan; }
			private set { _cmdChan = value; }
		}

		/// <summary>
		/// The local port we are going out or listening on
		/// </summary>
		public Int32 LocalPort {
			get {
				// need to test if the socket has been created first!
				return ((IPEndPoint)this.Socket.LocalEndPoint).Port;
			}
		}

		/// <summary>
		/// The local IP address the socket is using
		/// </summary>
		public IPAddress LocalIPAddress {
			get {
				// need to test if the socket has been created first!
				return ((IPEndPoint)this.Socket.LocalEndPoint).Address;
			}
		}

		/// <summary>
		/// The report port we are connected to
		/// </summary>
		public Int32 RemotePort {
			get {
				if (this.Connected) {
					return ((IPEndPoint)this.Socket.RemoteEndPoint).Port;
				}

				return 0;
			}
		}

		/// <summary>
		/// The remote IP address this socket is connected to
		/// </summary>
		public IPAddress RemoteIPAddress {
			get {
				if (this.Connected) {
					return ((IPEndPoint)this.Socket.RemoteEndPoint).Address;
				}

				return null;
			}
		}

		/// <summary>
		/// Base network stream, could be NetworkStream or SslStream
		/// depending on if ssl is enabled.
		/// </summary>
		public override Stream BaseStream {
			get {
				// authenticate the stream if it isn't already
				if (this.CommandChannel.SslEnabled && !this.SecurteStream.IsAuthenticated) {
					this.AuthenticateConnection();
				}

				return base.BaseStream;
			}
		}

		/// <summary>
		/// Reads a line from the FTP channel socket. Use with discretion,
		/// can cause the code to freeze if you're trying to read data when no data
		/// is being sent.
		/// </summary>
		/// <returns></returns>
		public string ReadLine() {
			if (!this.Connected) {
				this.Connect();
			}

			if (this.StreamReader != null) {
				string buf = this.StreamReader.ReadLine();
#if DEBUG
				Debug.WriteLine(string.Format("> {0}", buf));
#endif
				return buf;
			}

			throw new FtpException("The reader object is null. Are we connected?");
		}

		/// <summary>
		/// Reads bytes off the socket
		/// </summary>
		/// <param name="buf"></param>
		/// <param name="offset"></param>
		/// <param name="size"></param>
		public int Read(byte[] buf, int offset, int size) {
			if (!this.Connected) {
				this.Connect();
			}

			if (this.BaseStream != null) {
				return this.BaseStream.Read(buf, 0, size);
			}

			throw new FtpException("The network stream is null. Are we connected?");
		}

		/// <summary>
		/// Writes the specified byte array to the network stream
		/// </summary>
		public void Write(byte[] buf, int offset, int count) {
			if (!this.Connected) {
				this.Connect();
			}

			if (this.BaseStream != null) {
				this.BaseStream.Write(buf, offset, count);
			}
			else {
				throw new FtpException("The network stream is null. Are we connected?");
			}
		}

		/// <summary>
		/// Connects active or passive channels
		/// </summary>
		public override void Connect() {
			if (!this.Connected) {
				if (this.CommandChannel.DefaultDataMode == FtpDataMode.Active) {
					this.ConnectActiveChannel();
				}
				else {
					base.Connect();
				}
			}
		}

		/// <summary>
		/// Intializes the active channel socket
		/// </summary>
		public void InitalizeActiveChannel() {
			this.Socket.Bind(new IPEndPoint(((IPEndPoint)this.CommandChannel.LocalEndPoint).Address, 0));
			this.Socket.Listen(1);
#if DEBUG
			System.Diagnostics.Debug.WriteLine(string.Format("Active channel initalized and waiting: {0}:{1}",
				this.LocalIPAddress, this.LocalPort));
#endif
		}
				
		private void ConnectActiveChannel() {
			Socket s = this.Socket.Accept();

			this.Socket.Close();
			this.Socket = null;
			this.Socket = s;
			this.AuthenticateConnection();

#if DEBUG
			System.Diagnostics.Debug.WriteLine(string.Format("Connected from: {0}:{1}",
				this.RemoteIPAddress, this.RemotePort));
#endif
		}

		/// <summary>
		/// Cleans up any resources the DataChannel was using, also
		/// terminates any active connections.
		/// </summary>
		public new void Dispose() {
			base.Dispose();
			this.CommandChannel = null;
		}

		public FtpDataChannel(FtpCommandChannel cmdchan) {
			this.CommandChannel = cmdchan;
		}
	}
}