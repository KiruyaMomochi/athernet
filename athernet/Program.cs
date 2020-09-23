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
        static int SampleRate = 48000;
        static int PacketLength = 100;
        static int SamplesPerBit = 44;
        static PSKModulator Modulator = new PSKModulator(SampleRate)
        {
            SamplesPerBit = SamplesPerBit
        };
        static FunctionPreambleBuilder PreambleBuilder = new FunctionPreambleBuilder(PreambleFunc)
        {
            SampleRate = SampleRate,
            Time = 1
        };
        static WaveInEvent Recorder = new WaveInEvent()
        {
            DeviceNumber = 0,
            WaveFormat = WaveFormat.CreateIeeeFloatWaveFormat(48000, 1)
        };

        static BitArray bitArray = new BitArray(PacketLength);

        static void Main(string[] args)
        {
            for (int i = 0; i < 100; i += 2)
            {
                bitArray.Set(i, true);
            }

            Play(bitArray);
            Record();
        }

        static void Play(BitArray data)
        {
            var packet = Modulator.Modulate(data);
            var preamble = PreambleBuilder.Build();
            var provider = new PacketSampleProvider(preamble, packet);

            using var wo = new WaveOutEvent();
            wo.Init(provider);
            wo.Play();
            while (wo.PlaybackState == PlaybackState.Playing)
            {
                Thread.Sleep(500);
            }
        }

        static float PreambleFunc(int nSample, int sampleRate, int sampleCount)
        {
            float totalTime = (float)sampleCount / sampleRate;
            float time = (float)nSample / sampleRate;
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
            float[] preambleBuffer = new float[SampleRate * 2];

            Preamble preamble = PreambleBuilder.Build();
            PacketRecorder packetRecorder = new PacketRecorder(SampleRate, PacketLength * SamplesPerBit);

            Console.WriteLine($"{Recorder.WaveFormat.SampleRate} {Recorder.WaveFormat.Channels} {Recorder.WaveFormat.AverageBytesPerSecond} {Recorder.WaveFormat.BitsPerSample} {Recorder.WaveFormat.Encoding}");

            int detectcnt = 0;
            DecodeState state = DecodeState.Syncing;

            Recorder.DataAvailable += (sender, args) =>
            {
                var buffer = new WaveBuffer(args.Buffer);

                float[] floatBuffer = Recorder.WaveFormat.BitsPerSample switch
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

                    if (detectcnt > 10)
                    {
                        state = DecodeState.Decoding;
                        int len = packetRecorder.AddSamples(preambleBuffer, pos + 1, preambleBuffer.Length - pos);

                        for (int i = 0; i < pos + len; i++)
                            preambleBuffer[i] = 0;

                        detectcnt = 0;
                    }
                }

                if (state == DecodeState.Decoding)
                {
                    packetRecorder.AddSamples(floatBuffer, 0, floatBuffer.Length);
                }
            };

            packetRecorder.NewPacket += (sender, packet) =>
            {
                state = DecodeState.Syncing;
                Console.WriteLine("New packet!");
                BitArray result = Modulator.Demodulate(packet);
                for (int i = 0; i < PacketLength; i++)
                {
                    if (result.Get(i) != bitArray.Get(i))
                    {
                        Console.WriteLine($"Wrong bit at {i}, should be {bitArray.Get(i)}");
                    }
                }
            };

            Recorder.StartRecording();
            Thread.Sleep(1000000);
            Recorder.StopRecording();
        }

    }
}
