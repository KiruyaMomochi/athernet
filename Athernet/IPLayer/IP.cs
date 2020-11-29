using System;
using System.Linq;
using System.Net;
using Athernet.IPLayer.Packet;
using Athernet.MacLayer;

namespace Athernet.IPLayer
{
    public class IP
    {
        public IPAddress IpAddress;
        private Mac _mac;
        
        public int PlayDeviceNumber => _mac.PlayDeviceNumber;
        public int RecordDeviceNumber => _mac.RecordDeviceNumber;

        public IP(IPAddress ipAddress, Mac mac)
        {
            IpAddress = ipAddress;
            _mac = mac;
            SubscribeMac();
        }

        public IP(IPAddress ipAddress, byte macAddress, int playDeviceNumber = 0, int recordDeviceNumber = 0,
            int maxDataBytes = 1017)
        {
            IpAddress = ipAddress;
            _mac = new Mac(macAddress, playDeviceNumber, recordDeviceNumber, maxDataBytes)
            {
                NeedAck = false
            };
            SubscribeMac();
        }

        private void SubscribeMac()
        {
            _mac.DataAvailable += MacOnDataAvailable;
        }

        private void MacOnDataAvailable(object sender, DataAvailableEventArgs e)
        {
            OnPacketAvailable(Ipv4Packet.Parse(e.Data));
        }

        public event EventHandler<PacketAvailableEventArgs> PacketAvailable;

        public void SendPacket(Ipv4Packet ipv4Packet)
        {
            var packet = ipv4Packet.GetProtocolPacketBytes();
            var length = packet.Length + 7;
            var msb = Athernet.Utils.Maths.MostSignificantBitMask(length);
            if (length != msb)
            {
                msb <<= 1;
            }
            _mac.AddPayload(1, packet.Concat(new byte[msb - length]).ToArray());
        }

        protected virtual void OnPacketAvailable(Ipv4Packet packet)
        {
            PacketAvailable?.Invoke(this, new PacketAvailableEventArgs(){Packet = packet});
        }
        
        
        public void StartReceive() => _mac.StartReceive();
        public void StopReceive() => _mac.StopReceive();
    }
}