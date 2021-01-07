using System.Collections.Generic;
using System.Net;

namespace Athernet.IPLayer
{
    public record NatEntry
    {
        public byte Protocol { get; set; }
        public IPAddress Ip { get; set; }
        public ushort Id { get; set; }

        public NatEntry(byte protocol, IPAddress ip, ushort id)
            => (Protocol, Ip, Id) = (protocol, ip, id);
    }

    public class Nat
    {
        private readonly Dictionary<NatEntry, NatEntry> _dict;

        public Nat() => _dict = new Dictionary<NatEntry, NatEntry>();

        public void Add(NatEntry key, NatEntry value) => _dict.Add(key, value);

        public bool Lookup(NatEntry key, out NatEntry value) => _dict.TryGetValue(key, out value);

        public NatEntry GetNatEntry(NatEntry key)
        {
            return Lookup(key, out var value) ? value : null;
        }

        public NatEntry GetNatEntry(byte protocol, IPAddress ip, ushort id)
        {
            NatEntry key = new(protocol, ip, id);
            return GetNatEntry(key);
        }

        public NatEntry GetNatEntry(byte protocol, string ipString, ushort id)
        {
            return GetNatEntry(protocol, IPAddress.Parse(ipString), id);
        }
    }
}