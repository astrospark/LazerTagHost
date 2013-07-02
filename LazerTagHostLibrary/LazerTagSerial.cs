using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.IO.Ports;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;

namespace LazerTagHostLibrary
{
    public class LazerTagSerial
    {
		private const int InterSignatureDelayMilliseconds = 50;
		
		private SerialPort _serialPort;
	    private string _readBuffer; 
	    private BlockingCollection<byte[]> _writeQueue;
		private Thread _writeThread;

		static public List<string> GetSerialPorts()
		{
			var serialPorts = new List<string>();

			try
			{
				var directoryInfo = new DirectoryInfo("/dev");
				var deviceNodes = directoryInfo.GetFiles("ttyUSB*");
				serialPorts.AddRange(deviceNodes.Select(deviceNode => deviceNode.FullName));
			}
			catch (DirectoryNotFoundException)
			{
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex);
			}

			try
			{
				var portNames = SerialPort.GetPortNames();
				serialPorts.AddRange(portNames);
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex);
			}

			return serialPorts;
		}

		public bool Connect(string device)
		{
			try
			{
				_readBuffer = string.Empty;
				_writeQueue = new BlockingCollection<byte[]>();

				if (string.IsNullOrWhiteSpace(device)) return false;

				_serialPort = new SerialPort(device, 115200)
					{
						Parity = Parity.None,
						StopBits = StopBits.One,
					};
				_serialPort.DataReceived += SerialDataReceived;
				_serialPort.Open();

				_writeThread = new Thread(WriteThread)
					{
						IsBackground = true
					};
				_writeThread.Start();
			}
			catch (Exception ex)
			{
				Debug.WriteLine(ex);
				return false;
			}

			return true;
		}
	
        public void Disconnect()
        {
	        if (_serialPort == null || !_serialPort.IsOpen) return;
			_serialPort.Close();
            _serialPort = null;
		}

	    private void SerialDataReceived(object sender, SerialDataReceivedEventArgs e)
	    {
		    if (sender != _serialPort) return;
		    _readBuffer += _serialPort.ReadExisting();

			foreach (var line in SplitBufferLines(_readBuffer, out _readBuffer))
			{
				OnDataReceived(new DataReceivedEventArgs(line));
			}
	    }

		private static IEnumerable<string> SplitBufferLines(string buffer, out string remainingBuffer)
		{
			var lines = new List<string>();
			var match = Regex.Match(buffer, @"(.+?)(\r?\n|\r)");
			var lastMatchPosition = 0;
			while (match.Success)
			{
				if (match.Groups.Count < 2) continue;
				lines.Add(match.Groups[1].Value);

				lastMatchPosition = match.Index + match.Length;
				match = match.NextMatch();
			}

			remainingBuffer = buffer.Substring(lastMatchPosition);

			return lines;
		}

	    public void Enqueue(byte[] bytes)
		{
			_writeQueue.Add(bytes);
		}

		private void WriteThread()
        {
			try
			{
				while (_serialPort != null && _serialPort.IsOpen)
				{
					var data = _writeQueue.Take();

					_serialPort.Write(data, 0, data.Length);
					_serialPort.BaseStream.Flush();

					// TODO: Move this to the device firmware
					Thread.Sleep(InterSignatureDelayMilliseconds);
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.ToString());
				OnIoError(new IoErrorEventArgs(null, ex));
			}
        }

	    public class IoErrorEventArgs : EventArgs
	    {
		    public IoErrorEventArgs(string message, Exception ex)
		    {
			    Message = message;
			    Exception = ex;
		    }

		    public string Message;
		    public Exception Exception;
	    }

		public delegate void IoErrorEventHandler(object sender, IoErrorEventArgs e);

		public event IoErrorEventHandler IoError;

		protected virtual void OnIoError(IoErrorEventArgs e)
		{
			if (IoError != null) IoError(this, e);
		}

	    public delegate void DataReceivedEventHandler(object sender, DataReceivedEventArgs e);

	    public event DataReceivedEventHandler DataReceived;

		protected virtual void OnDataReceived(DataReceivedEventArgs e)
		{
			if (DataReceived != null) DataReceived(this, e);
		}

	    public class DataReceivedEventArgs : EventArgs
	    {
			public DataReceivedEventArgs(string data)
			{
				Data = data;
			}

			public string Data { get; set; }
	    }
	}
}
