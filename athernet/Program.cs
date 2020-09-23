using athernet.SampleProviders;
using NAudio.Wave;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Collections;
using athernet.Modulators;
using athernet.Preambles.PreambleBuilders;
using athernet.Preambles;
using athernet.Recorders;

namespace athernet
{
    class Program
    {
        static void Main(string[] args)
        {   
            Test();
            //Preamble();
            Record();
            //Recorver();
        }

        static void Test()
        {
            PSKModulator pSK = new PSKModulator(48000)
            {
                SamplesPerBit = 88
            };

            //bool[] b = new bool[] { true, false, true, false, true, false };

            //BitArray bitArray = new BitArray(b);
            BitArray bitArray = new BitArray(100);
            for (int i = 0; i < 100; i += 2)
            {
                bitArray.Set(i, true);
            }

            var packet = pSK.Modulate(bitArray);
            //var res = pSK.Demodulate(packet);
            var preamble = new Preamble(PreambleBuilder().ToArray());

            var provider = new PacketSampleProvider(preamble, packet);


            PSKModulator rawPSK = new PSKModulator(48000)
            {
                SamplesPerBit = 44,
                Gain = new[]{1.0, 1.0}
            };
            var rawPacket = rawPSK.Modulate(bitArray);
            float[] rawreal = preamble.Data.Concat(rawPacket.Samples).ToArray();
            File.WriteAllText(Path.Combine(Path.GetTempPath(), "raw.csv"), String.Join('\n', rawreal));

            float[] real = preamble.Data.Concat(packet.Samples).ToArray();

            Console.WriteLine(Path.Combine(Path.GetTempPath(), "template.csv"));
            File.WriteAllText(Path.Combine(Path.GetTempPath(), "template.csv"), String.Join('\n', real));

            using var wo = new WaveOutEvent();
            wo.Init(provider);
            wo.Play();
            while (wo.PlaybackState == PlaybackState.Playing)
            {
                Thread.Sleep(500);
            }
        }

        static float[] PreambleBuilder(int sampleRate = 48000)
        {
            return new FunctionPreambleBuilder(PreambleFunc)
            {
                SampleRate = sampleRate,
                SampleCount = sampleRate
            }.Preamble.Data;
        }

        static float PreambleFunc(int nSample, int sampleRate, int sampleCount)
        {
            float totalTime = (float) sampleCount / sampleRate;
            float time = (float) nSample / sampleRate;
            float frequencyMin = 4000;
            float frequencyMax = 9000;

            float a = (frequencyMax - frequencyMin) * 2 / totalTime;
            float soundVoice = 1;

            if (nSample < sampleCount / 2)
            {
                float phase = time * time * a * (float)Math.PI + time * frequencyMin * (float)Math.PI * 2;
                float anss = (float)Math.Cos(phase) * soundVoice;
                return anss;
            }
            else
            {
                float phase = -time * time * a * (float)Math.PI + time * frequencyMax * 2 * (float)Math.PI;
                float anss = (float)Math.Cos(phase) * soundVoice;
                return anss;
            }
        }

        public enum DecodeState
        {
            Syncing,
            Decoding
        }

        static void Record()
        {
            float[] preambleBuffer = new float[48000 * 2];

            var recorder = new WaveInEvent()
            {
                DeviceNumber = 0,
                WaveFormat = WaveFormat.CreateIeeeFloatWaveFormat(48000, 1)
            };

            Preamble preamble = new Preamble(PreambleBuilder(48000).ToArray());
            PacketRecorder packetRecorder = new PacketRecorder(48000, 150 * 88);

            Console.WriteLine($"{recorder.WaveFormat.SampleRate} {recorder.WaveFormat.Channels} {recorder.WaveFormat.AverageBytesPerSecond} {recorder.WaveFormat.BitsPerSample} {recorder.WaveFormat.Encoding}");

            int detectcnt = 0;
            DecodeState state = DecodeState.Syncing;

            PSKModulator pSK = new PSKModulator(48000)
            {
                SamplesPerBit = 88
            };

            recorder.DataAvailable += (sender, args) =>
            {
                var buffer = new WaveBuffer(args.Buffer);

                float[] floatBuffer = recorder.WaveFormat.BitsPerSample switch
                {
                    16 => buffer.ShortBuffer.Take(args.BytesRecorded / 2).Select(x => (float)x).ToArray(),
                    32 => buffer.FloatBuffer.Take(args.BytesRecorded / 4).ToArray(),
                    _ => throw new Exception(),
                };

                preambleBuffer = preambleBuffer.Skip(floatBuffer.Length).Concat(floatBuffer).ToArray();

                if (state == DecodeState.Syncing)
                {
                    float max;
                    int pos;

                    (max, pos) = preamble.Detect(preambleBuffer);

                    if (max > 300)
                    {
                        Console.WriteLine($"Detected maximum correlation: {max} at position {pos}");
                        detectcnt++;
                    }
                    else
                    {
                        detectcnt = 0;
                    }

                    if (detectcnt > 10 & pos < 55000)
                    {
                        packetRecorder.AddSamples(preambleBuffer, pos + 1, preambleBuffer.Length - pos);

                        File.WriteAllText(Path.Combine(Path.GetTempPath(), "fuck.csv"), String.Join('\n', preambleBuffer));

                        detectcnt = 0;
                        state = DecodeState.Decoding;
                    }
                }
                if (state == DecodeState.Decoding)
                {
                    packetRecorder.AddSamples(floatBuffer, 0, floatBuffer.Length);
                }
            };

            packetRecorder.NewPacket += (sender, packet) =>
            {
                Console.WriteLine("New packet!");
                //state = DecodeState.Syncing;
                pSK.Demodulate(packet);
            };

            //packetRecorder.StartRecording();
            recorder.StartRecording();
            Thread.Sleep(1000000);
            recorder.StopRecording();
            //packetRecorder.StopRecording();
        }

    }
}
