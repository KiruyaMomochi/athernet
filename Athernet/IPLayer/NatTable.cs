using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

namespace Athernet.IPLayer
{
    public record NatEntry
    {
        public ProtocolType Protocol { get; set; }
        public string Ip { get; set; }
        public ushort Id { get; set; }

        public NatEntry(ProtocolType protocol, string ip, ushort id)
            => (Protocol, Ip, Id) = (protocol, ip, id);
    }

    public class NatTable
    {
        private readonly Dictionary<NatEntry, NatEntry> _dict;

        public NatTable() => _dict = new Dictionary<NatEntry, NatEntry>();

        public void Add(NatEntry key, NatEntry value)
        {
            _dict.Add(key, value);
            _dict.Add(value, key);
        }

        public bool Lookup(NatEntry key, out NatEntry value) => _dict.TryGetValue(key, out value);

        public NatEntry GetNatEntry(NatEntry key)
        {
            return Lookup(key, out var value) ? value : null;
        }

        public NatEntry GetNatEntry(ProtocolType protocol, IPAddress ip, ushort id)
        {
            NatEntry key = new(protocol, ip.ToString(), id);
            return GetNatEntry(key);
        }

        public NatEntry GetNatEntry(ProtocolType protocol, string ipString, ushort id)
        {
            NatEntry key = new(protocol, ipString, id);
            return GetNatEntry(key);
        }
    }
}