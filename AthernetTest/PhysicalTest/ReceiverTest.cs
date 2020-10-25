﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Athernet.Modulators;
using Athernet.Utils;
using NAudio.Wave.SampleProviders;
using Xunit;
using Xunit.Abstractions;

namespace AthernetTest.PhysicalTest
{
    public class ReceiverTest
    {
        private readonly Athernet.Physical.Receiver _receiver;
        private readonly ITestOutputHelper _output;
        
        public ReceiverTest(ITestOutputHelper output)
        {
            IModulator modulator = new Athernet.Modulators.DpskModulator(48000, 8000)
            {
                FrameBytes = 250,
                BitDepth = 32
            };

            _receiver = new Athernet.Physical.Receiver(modulator)
            {
                Preamble = new Athernet.Preambles.PreambleBuilders.WuPreambleBuilder(48000, 0.1f).Build()
            };

            _output = output;
        }

        [Fact]
        public void DecodeWirelessData()
        {
            const string cmpFile = "PhysicalTest/input.txt";
            const string wavFile = "PhysicalTest/test.wav";

            Assert.True(System.IO.File.Exists(cmpFile));
            Assert.True(System.IO.File.Exists(wavFile));

            var correct = Athernet.Utils.Maths.ToBytes(string
                .Join("", File.ReadLines(cmpFile))
                .Select(x => x switch
                {
                    '0' => false,
                    '1' => true,
                    _ => throw new InvalidCastException()
                }), Maths.Endianness.LittleEndian).ToArray();
            
            IEnumerable<byte> result = new byte[0];
            
            var wav = new NAudio.Wave.WaveFileReader(wavFile);
            var samples = new WaveToSampleProvider(wav);
            
            var floats = new float[wav.Length * 8 / wav.WaveFormat.BitsPerSample / wav.WaveFormat.Channels];
            var readLen = samples.Read(floats, 0, floats.Length);
            Assert.Equal(floats.Length, readLen);
            
            _receiver.DataAvailable += (sender, args) => result = result.Concat(args.Data);
            _receiver.ReceiveSamples(floats);

            using var res = result.GetEnumerator();
            var wrongIdx = new List<int>();
            for (int i = 0; i < correct.Length; i++)
            {
                res.MoveNext();
                if (correct[i] != res.Current)
                    wrongIdx.Add(i);
            }

            _output.WriteLine($"\n{wrongIdx.Count} {(wrongIdx.Count > 0 ? wrongIdx.First() : ' ')}");
            Assert.True(wrongIdx.Count < 10);
        }
    }
}