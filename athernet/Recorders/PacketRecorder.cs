using athernet.Modulators;
using athernet.Packets;
using NAudio.Wave;
using System;
using System.ComponentModel;
using System.Text;

namespace athernet.Recorders
{
    class PacketRecorder
    {
        private int nSample = 0;
        private Packet packet;

        public int SampleRate => packet.SamplingRate;
        public int SampleLength => packet.Length;

        public WaveFormat WaveFormat { get; set; }

        public PacketRecorder(int sampleRate, int sampleLength)
        {
            packet = new Packet(sampleRate, sampleLength);
        }

        public delegate void NewPacketHandler(object sender, Packet p);

        public event NewPacketHandler NewPacket;

        public int AddSamples(float[] samples, int offset, int count)
        {
            for (int i = 0; i < count; i++)
            {
                packet.Samples[nSample] = samples[offset + i];

                nSample++;

                if (nSample == SampleLength)
                {
                    RaiseNewPacketEvent();
                    nSample = 0;
                    return i;
                }
            }
            Console.WriteLine($"Add samples {offset} and {count}");

            return count;
        }

        protected void RaiseNewPacketEvent()
        {
            nSample = 0;
            NewPacket?.Invoke(this, packet.Copy());
        }
    }
}
