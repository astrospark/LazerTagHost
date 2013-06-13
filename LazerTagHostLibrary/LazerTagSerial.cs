using System;
using System.IO.Ports;
using System.Collections.Generic;

namespace LazerTagHostLibrary
{
    public class LazerTagSerial
    {
        private SerialPort _serialPort;
        private const int InterPacketDelay = 50; // milliseconds
        private readonly Queue<byte[]> _queue;

        private readonly System.Threading.Thread _workerThread;
        private Boolean _enableWrite;

        public LazerTagSerial (string device)
        {
            if (!string.IsNullOrWhiteSpace(device))
            {
	            _serialPort = new SerialPort(device, 115200)
		            {
			            Parity = Parity.None,
			            StopBits = StopBits.One
		            };
	            _serialPort.Open();
            }

	        _queue = new Queue<byte[]>();

            _enableWrite = true;
	        _workerThread = new System.Threading.Thread(WriteThread)
		        {
			        IsBackground = true
		        };
	        _workerThread.Start();
        }

        public void Stop()
        {
            _enableWrite = false;
            _workerThread.Join();
            if (_serialPort != null && _serialPort.IsOpen)
			{
                _serialPort.Close();
                _serialPort = null;
            }
        }

        public string TryReadCommand()
        {
			if (_serialPort == null || !_serialPort.IsOpen || _serialPort.BytesToRead < 1) return null;

            var input = _serialPort.ReadLine();
            return input;
        }

        private void WriteThread()
        {
            try
			{
                while (_enableWrite)
				{
                    if (_queue.Count > 0 && _serialPort != null)
					{
                        var packet = _queue.Dequeue();
    
                        _serialPort.Write( packet, 0, 2 );
                        _serialPort.BaseStream.Flush();
                    }
                    System.Threading.Thread.Sleep(InterPacketDelay);
                }
            }
			catch (Exception ex)
			{
                Console.WriteLine(ex.ToString());
            }
        }

        public void EnqueueBeacon(UInt16 data, UInt16 bitCount)
        {
	        var signature = new[]
		        {
			        (byte) ((0x01 << 5) | ((bitCount & 0xf) << 1) | ((data >> 8) & 0x1)),
			        (byte) (data & 0xff)
		        };
            _queue.Enqueue(signature);
        }

        public void EnqueueData(UInt16 data, UInt16 bitCount)
        {
	        var signature = new[]
		        {
			        (byte) ((0x00 << 5) | ((bitCount & 0xf) << 1) | ((data >> 8) & 0x1)),
			        (byte) (data & 0xff)
		        };
            _queue.Enqueue(signature);
        }

        static public List<string> GetSerialPorts()
        {
            var serialPorts = new List<string>();

            try
			{
                var directoryInfo = new System.IO.DirectoryInfo("/dev");
                var deviceNodes = directoryInfo.GetFiles("ttyUSB*");
    
                foreach (var deviceNode in deviceNodes)
				{
                    serialPorts.Add(deviceNode.FullName);
                }
            }
			catch (Exception ex)
			{
				Console.WriteLine(ex.Message);
			}

            try
			{
                var ports = SerialPort.GetPortNames();
                foreach (var port in ports)
				{
                    serialPorts.Add(port);
                }
            }
			catch (Exception ex)
			{
				Console.WriteLine(ex.Message);
			}

            return serialPorts;
        }
    }
}
