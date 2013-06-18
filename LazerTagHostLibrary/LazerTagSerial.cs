using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.IO.Ports;
using System.Collections.Generic;
using System.Threading;

namespace LazerTagHostLibrary
{
    public class LazerTagSerial
    {
		private const int InterSignatureDelayMilliseconds = 50;
		
		private SerialPort _serialPort;
	    private ConcurrentQueue<byte[]> _queue;
		private Thread _workerThread;

		static public List<string> GetSerialPorts()
		{
			var serialPorts = new List<string>();

			try
			{
				var directoryInfo = new DirectoryInfo("/dev");
				var deviceNodes = directoryInfo.GetFiles("ttyUSB*");

				foreach (var deviceNode in deviceNodes)
				{
					serialPorts.Add(deviceNode.FullName);
				}
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
			if (string.IsNullOrWhiteSpace(device)) return false;

			try
			{
				_queue = new ConcurrentQueue<byte[]>();

				_serialPort = new SerialPort(device, 115200)
					{
						Parity = Parity.None,
						StopBits = StopBits.One,
						ReadTimeout = 1,
					};
				_serialPort.Open();

				_workerThread = new Thread(WriteThread)
					{
						IsBackground = true
					};
				_workerThread.Start();
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

		// TODO: Make this asynchronous
        public string Read()
        {
	        if (_serialPort == null || !_serialPort.IsOpen) return null; // throw new IOException();
	        if (_serialPort.BytesToRead < 1) return null;
	        try
	        {
		        return _serialPort.ReadLine();
	        }
	        catch (TimeoutException)
	        {
		        return null;
	        }
        }

		public void Enqueue(UInt16 data, UInt16 bitCount, bool isBeacon = false)
		{
			var signatureData = new[]
				{
					(byte) ((isBeacon ? 0x01 : 0x00 << 5) | ((bitCount & 0xf) << 1) | ((data >> 8) & 0x1)),
					(byte) (data & 0xff)
				};
			_queue.Enqueue(signatureData);
		}

		private void WriteThread()
        {
			try
			{
				while (_serialPort != null && _serialPort.IsOpen)
				{
					byte[] signatureData;
					if (_queue.TryDequeue(out signatureData))
					{
						if (signatureData.Length != 2) continue;

						_serialPort.Write(signatureData, 0, signatureData.Length);
						_serialPort.BaseStream.Flush();

						Thread.Sleep(InterSignatureDelayMilliseconds);
					}
					else
					{
						Thread.Sleep(0);
					}
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
			IoError(this, e);
		}
	}
}
