using System;
using System.Collections.Generic;
using System.Net;
using ToyNet.IpInterface;
namespace ToyNet.Gateway
{
    public record NatEntry
    {
        public byte      Protocol;
        public IPAddress Ip;
        public ushort    Id;

        public NatEntry(byte protocol, IPAddress ip, ushort id) 
            => (Protocol, Ip, Id) = (protocol, ip, id);
    }
    public class Nat
    {
        private Dictionary<NatEntry, NatEntry> _dict;

        public Nat()
        {
            _dict = new();
        }
        public void Add(NatEntry key, NatEntry value)
        {
            _dict.Add(key, value);
        }
        public bool Lookup(NatEntry key, out NatEntry value)
        {
            return _dict.TryGetValue(key, out value);
        }
        public NatEntry GetNatEntry(NatEntry key)
        {
            NatEntry value;
            if (Lookup(key, out value))
            {
                return value;
            }
            else // Failed to get value and return NULL
            {
                return null;
            }
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