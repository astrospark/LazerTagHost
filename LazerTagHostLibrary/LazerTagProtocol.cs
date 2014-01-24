using System;
using System.Collections.Generic;
using System.Text;

namespace LazerTagHostLibrary
{
	public class LazerTagProtocol
	{
		#region Constructors/Destructor

		public LazerTagProtocol(HostGun hostGun, string portName)
		{
			_hostGun = hostGun;
			_serial = new LazerTagSerial();
			_serial.IoError += Serial_IoError;
			_serial.DataReceived += Serial_DataReceived;
			_serial.Connect(portName);
			_serial.Enqueue(new[] {(byte) '\r', (byte) '\n'});
		}

		#endregion

		#region Public Methods

		public bool SetDevice(string device)
		{
			_serial.Disconnect();
			return _serial.Connect(device);
		}

		public void TransmitSignature(Signature signature)
		{
			_serial.Enqueue(EncodeSignature(signature));
		}

		public void TransmitSignature(IEnumerable<Signature> signatures)
		{
			foreach (var signature in signatures)
			{
				TransmitSignature(signature);
			}
		}

		public void TransmitPacket(Packet packet)
		{
			var e = new PacketSendingEventArgs(packet);
			OnPacketSending(e);
			if (e.Cancel) return;

			Log.Add(Log.Severity.Debug, "TX {0}: {1}", packet.Type.ToString(), packet);
			TransmitSignature(packet.Signatures);
		}

		public void SendTag(TeamPlayerId teamPlayerId, int damage)
		{
			var signature = PacketPacker.Tag(teamPlayerId, damage);
			TransmitSignature(signature);
			Log.Add(Log.Severity.Information, "Shot {0} tags as player {1}.", damage, teamPlayerId);
		}

		public void SendRequestJoinGame(byte gameId, int preferredTeamNumber)
		{
			var taggerId = GenerateRandomId();
			var packet = PacketPacker.RequestJoinGame(gameId, taggerId, preferredTeamNumber);
			TransmitPacket(packet);
			Log.Add(Log.Severity.Information,
				"Sending request to join game 0x{0:X2} with tagger ID 0x{1:X2}. Requesting team {2}", gameId, taggerId,
				preferredTeamNumber);
		}

		public void SendTextMessage(string message)
		{
			var packet = PacketPacker.TextMessage(message);
			TransmitPacket(packet);
		}

		public static byte GenerateRandomId()
		{
			return (byte)(new Random().Next() & 0xff);
		}

		#endregion

		#region Public Events

		public event EventHandler<LazerTagSerial.IoErrorEventArgs> IoError;

		protected void OnIoError(LazerTagSerial.IoErrorEventArgs e)
		{
			if (IoError != null) IoError(this, e);
		}

		public event EventHandler<PacketSendingEventArgs> PacketSending;

		protected virtual void OnPacketSending(PacketSendingEventArgs e)
		{
			if (PacketSending != null) PacketSending(this, e);
		}

		public class PacketSendingEventArgs : EventArgs
		{
			public PacketSendingEventArgs(Packet packet)
			{
				Cancel = false;
				Packet = packet;
			}

			public bool Cancel { get; set; }
			public Packet Packet { get; set; }
		}

		#endregion

		#region Private Methods

		private void Serial_DataReceived(object sender, LazerTagSerial.DataReceivedEventArgs e)
		{
			if (e.Data == null) return;

			var parts = e.Data.Split(new[] {' '}, StringSplitOptions.RemoveEmptyEntries);
			switch (parts[0].ToUpperInvariant())
			{
				case "RCV":
					if (parts.Length < 4)
					{
						Log.Add(Log.Severity.Debug, "Received truncated response, '{0}'.", e.Data);
						break;
					}
					var data = Convert.ToUInt16(parts[1], 16);
					var bitCount = Convert.ToByte(parts[2], 16);
					var isBeacon = (Convert.ToUInt16(parts[3], 16) != 0);
					ProcessSignature(data, bitCount, isBeacon);
					break;
				case "ERROR":
					Log.Add(Log.Severity.Debug, e.Data);
					break;
				default:
					Log.Add(Log.Severity.Debug, "Received unrecognized response, '{0}'.", e.Data);
					break;
			}
		}

		private void Serial_IoError(object sender, LazerTagSerial.IoErrorEventArgs e)
		{
			OnIoError(e);
		}

		private void ProcessSignature(UInt16 data, byte bitCount, bool isBeacon)
		{
			if (isBeacon)
			{
				ProcessBeaconSignature(data, bitCount);
			}
			else
			{
				ProcessDataSignature(data, bitCount);
			}
		}

		private static void ProcessBeaconSignature(UInt16 data, UInt16 bitCount)
		{
			switch (bitCount)
			{
				case 5:
					{
						var teamNumber = (data >> 3) & 0x3;
						var tagReceived = ((data >> 2) & 0x1) != 0;
						var flags = data & 0x3;

						var teamText = teamNumber == 0 ? "solo" : string.Format("team {0}", teamNumber);

						string typeText;
						var tagsReceivedText = "";
						if (!tagReceived && flags != 0)
						{
							var zoneType = (HostGun.ZoneType)flags;
							typeText = string.Format("zone beacon ({0})", zoneType);
						}
						else if (tagReceived)
						{
							typeText = "hit beacon";
							tagsReceivedText = string.Format(" Player received {0} tags.", flags + 1);
						}
						else
						{
							typeText = "beacon";
						}

						Log.Add(Log.Severity.Debug, "Received {0} {1}.{2}", teamText, typeText, tagsReceivedText);
						break;
					}
				case 9:
					{
						var tagReceived = ((data >> 8) & 0x1) != 0;
						var shieldActive = ((data >> 7) & 0x1) != 0;
						var tagsRemaining = (data >> 5) & 0x3;
						//var flags = (data >> 2) & 0x7;
						var teamNumber = data & 0x3;

						var teamText = teamNumber != 0 ? "solo" : string.Format("team {0}", teamNumber);
						var typeText = tagReceived ? "hit beacon" : "beacon";
						var shieldText = shieldActive ? " Shield active." : "";

						string tagsText;
						switch (tagsRemaining)
						{

							case 0x3:
								{
									tagsText = "50-100%";
									break;
								}
							case 0x2:
								{
									tagsText = "25-50%";
									break;
								}
							case 0x1:
								{
									tagsText = "1-25%";
									break;
								}
							default:
								{
									tagsText = "0";
									break;
								}
						}

						Log.Add(Log.Severity.Debug, "Received {0} {1}. {2} tags remaining.{3}", teamText, typeText, tagsText, shieldText);
						break;
					}
			}
		}

		private void ProcessDataSignature(UInt16 data, byte bitCount)
		{
			if (bitCount == 9)
			{
				if ((data & 0x100) == 0) // packet type
				{
					_incomingPacket = new Packet
					{
						PacketTypeSignature = new Signature(SignatureType.Packet, data),
					};
				}
				else // checksum
				{
					if (_incomingPacket == null)
					{
						Log.Add(Log.Severity.Debug, "Stray checksum signature received.");
						return;
					}

					if (!(_incomingPacket.PacketTypeSignatureValid && _incomingPacket.DataValid))
					{
						Log.Add(Log.Severity.Debug, "Checksum received for invalid packet: {0}", _incomingPacket);
						_incomingPacket = null;
						return;
					}

					_incomingPacket.Checksum = new Signature(SignatureType.Checksum, data);

					if (_incomingPacket.ChecksumValid)
					{
						Log.Add(Log.Severity.Debug, "RX {0}: {1}", _incomingPacket.Type.ToString(), _incomingPacket);

						// TODO: Make this event driven
						if (!_hostGun.ProcessPacket(_incomingPacket))
						{
							Log.Add(Log.Severity.Warning, "ProcessPacket() failed: {0}", _incomingPacket);
						}
					}
					else
					{
						Log.Add(Log.Severity.Debug, "Invalid checksum received. {0}", _incomingPacket);
					}

					_incomingPacket = null;
				}
			}
			else if (bitCount == 8) // data
			{
				if (_incomingPacket == null || !_incomingPacket.PacketTypeSignatureValid)
				{
					Log.Add(Log.Severity.Debug, "Stray data packet received. 0x{0:X2} ({1})", data, bitCount);
					_incomingPacket = null;
					return;
				}

				_incomingPacket.Data.Add(new Signature(SignatureType.Data, data));
			}
			else if (bitCount == 7) // tag
			{
				_incomingPacket = null;
				ProcessTag(new Signature(SignatureType.Tag, data, bitCount));
			}
			else
			{
				Log.Add(Log.Severity.Debug, "Stray data packet received. 0x{0:X2} ({1})", data, bitCount);
			}
		}

		private static void ProcessTag(Signature signature)
		{
			var teamPlayerId = TeamPlayerId.FromPacked23((UInt16) ((signature.Data >> 2) & 0x1f));
			var strength = (signature.Data & 0x3) + 1;
			Log.Add(Log.Severity.Debug, "Received shot from player {0} with {1} tags.", teamPlayerId, strength);
		}

		private static byte[] EncodeSignature(Signature signature)
		{
			if (signature == null) throw new ArgumentException("signature");

			var isBeacon = signature.Type == SignatureType.Beacon;

			var data = signature.Data;
			byte bitCount;

			switch (signature.Type)
			{
				case SignatureType.Packet:
					data &= 0xff;
					bitCount = 9;
					break;
				case SignatureType.Checksum:
					data &= 0xff;
					data |= 0x100; // set the 9th bit
					bitCount = 9;
					break;
				default:
					bitCount = signature.BitCount;
					break;
			}

			var command = string.Format("CMD 10 {0:X} {1:X} {2:X}\r\n", data, bitCount, isBeacon ? 1 : 0);

			return Encoding.ASCII.GetBytes(command);
		}

		#endregion

		#region Private Fields

		private readonly LazerTagSerial _serial;
		private readonly HostGun _hostGun;
		private Packet _incomingPacket;

		#endregion
	}
}
