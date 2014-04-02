using System;
using System.Collections.Concurrent;
using System.IO.Ports;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading;
using Microsoft.Win32;

namespace LazerTagHostLibrary
{
    public class LazerTagSerial : IDisposable
    {
		private const int InterSignatureDelayMilliseconds = 50;

	    private string _portName;
		private SerialPort _serialPort;
	    private string _readBuffer; 
	    private readonly BlockingCollection<byte[]> _writeQueue;
		private Thread _writeThread;

		public LazerTagSerial()
		{
			_writeQueue = new BlockingCollection<byte[]>();
			SystemEvents.PowerModeChanged += PowerModeChanged;
		}

	    ~LazerTagSerial()
	    {
		    Dispose(false);
	    }

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposing)
		{
			if (disposing) // release managed resources
			{
				Disconnect();
				if (_serialPort != null) _serialPort.Dispose();
				if (_writeQueue != null) _writeQueue.Dispose();
			}

			// release unmanaged resources
		}

		static public string[] GetSerialPorts()
		{
			try
			{
				return SerialPort.GetPortNames();
			}
			catch (Exception ex)
			{
				Log.Add("GetSerialPorts() failed.", ex);
				return new string[] {};
			}
		}

	    public bool Connect(string portName)
	    {
		    try
		    {
			    if (string.IsNullOrWhiteSpace(portName)) return false;
			    _portName = portName;

			    if (_serialPort != null)
			    {
				    Disconnect();
				    Thread.Sleep(5);
			    }

			    _serialPort = new SerialPort(_portName, 115200, Parity.None, 8, StopBits.One)
			    {
				    Handshake = Handshake.None,
				    DtrEnable = true,
				    RtsEnable = true
			    };
				_serialPort.PinChanged += SerialPinChanged;
				_serialPort.DataReceived += SerialDataReceived;
				_serialPort.ErrorReceived += SerialErrorReceived;
			    _serialPort.Open();

			    _writeThread = new Thread(WriteThread)
			    {
				    IsBackground = true
			    };
			    _writeThread.Start();
		    }
		    catch (Exception ex)
		    {
			    Log.Add("Connect() failed.", ex);
				Disconnect();
			    return false;
		    }

		    return true;
	    }

	    public void Disconnect()
        {
			AbortWriteThread();

	        if (_serialPort == null) return;

		    try
		    {
				_serialPort.Close();
			}
			catch (Exception ex)
			{
				Log.Add("LazerTagSerial.Disconnect() failed.", ex);
			}

            _serialPort = null;
		}

		private static void SerialPinChanged(object sender, SerialPinChangedEventArgs e)
		{
			Log.Add(Log.Severity.Debug, "SerialPinChanged(): {0}", e.EventType);
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

		private static void SerialErrorReceived(object sender, SerialErrorReceivedEventArgs e)
		{
			Log.Add(Log.Severity.Debug, "SerialErrorReceived(): {0}", e.EventType);
		}

		public void Enqueue(byte[] bytes)
	    {
		    if (_serialPort == null || !_serialPort.IsOpen)
		    {
			    if (!Connect(_portName)) return;
		    }
				
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
			catch (ThreadAbortException)
			{
				Log.Add(Log.Severity.Debug, "LazerTagSerial.WriteThread() abort requested.");
			}
			catch (Exception ex)
			{
				Log.Add("WriteThread() failed.", ex);
				OnIoError(new IoErrorEventArgs(null, ex));
			}
			finally
			{
				// not much use if we can't send anymore so disconnect it
				Disconnect();
			}
			Log.Add(Log.Severity.Debug, "LazerTagSerial.WriteThread() exiting.");
        }

		private void AbortWriteThread()
		{
			if (_writeThread == null) return;

			if (_serialPort != null && _serialPort.IsOpen)
			{
				try
				{
					_serialPort.DiscardOutBuffer();
				}
				catch (Exception ex)
				{
					Log.Add("LazerTagSerial.AbortWriteThread() failed at _serialPort.DiscardOutBuffer().", ex);
				}
			}

			try
			{
				if ((_writeThread.ThreadState & (ThreadState.AbortRequested | ThreadState.Aborted)) == 0)
				{
					_writeThread.Abort();
				}
			}
			catch (Exception ex)
			{
				Log.Add("LazerTagSerial.AbortWriteThread() failed.", ex);
			}
		}

		private void PowerModeChanged(object sender, PowerModeChangedEventArgs e)
		{
			Log.Add(Log.Severity.Debug, "LazerTagSerial.PowerModeChanged(): {0}", e.Mode);
			if (e.Mode == PowerModes.Resume) Connect(_portName);
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

		public class DataReceivedEventArgs : EventArgs
		{
			public DataReceivedEventArgs(string data)
			{
				Data = data;
			}

			public string Data { get; set; }
		}

		public delegate void DataReceivedEventHandler(object sender, DataReceivedEventArgs e);

	    public event DataReceivedEventHandler DataReceived;

		protected virtual void OnDataReceived(DataReceivedEventArgs e)
		{
			if (DataReceived != null) DataReceived(this, e);
		}
    }
}
