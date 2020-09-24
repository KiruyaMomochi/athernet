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
using NAudio.Wave.SampleProviders;

namespace athernet
{
    class Program
    {
        static int SampleRate = 48000;
        static int PacketLength = 100;
        static int SamplesPerBit = 44;
        static DPSKModulator Modulator = new DPSKModulator(SampleRate, 8000, 1)
        {
            SamplesPerBit = SamplesPerBit
        };
        static FunctionPreambleBuilder PreambleBuilder = new FunctionPreambleBuilder(PreambleFunc, 48000, 0.1f);
        static WaveInEvent Recorder = new WaveInEvent()
        {
            DeviceNumber = 0,
            WaveFormat = WaveFormat.CreateIeeeFloatWaveFormat(48000, 1)
        };

        static BitArray bitArray = new BitArray(PacketLength);

        static void Main(string[] args)
        {
            for (int i = 0, j = 0; i < 100; i += j, j++)
            {
                bitArray.Set(i, true);
            }

            var signal = new SignalGenerator(SampleRate, 1)
            {
                Type = SignalGeneratorType.Sin,
                Frequency = 8000,
                Gain = 1
            };

            //var rawSamples = new float[SampleRate * SamplesPerBit];
            //signal.Read(rawSamples, 0, rawSamples.Length);
            //writeTempCsv(rawSamples, "carrier.csv");

            Play(bitArray);
            Record();
        }

        static void Play(BitArray data)
        {
            var packet = Modulator.Modulate(data);
            var preamble = PreambleBuilder.Build();
            var provider = new PacketSampleProvider(preamble, packet);

            writeTempCsv(preamble.Data, "preamble_template.csv");
            writeTempCsv(packet.Samples, "template.csv");

            using var wo = new WaveOutEvent();
            wo.Init(provider);
            wo.Play();
            while (wo.PlaybackState == PlaybackState.Playing)
            {
                Thread.Sleep(500);
            }
        }

        static void writeTempCsv(float[] buffer, string fileName)
        {
            var path = Path.Combine(Path.GetTempPath(), fileName);
            File.WriteAllText(path, String.Join(", ", buffer));
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
            PacketRecorder packetRecorder = new PacketRecorder(SampleRate, (PacketLength + 1) * SamplesPerBit);

            //BinaryModulator Modulator = new BinaryPSKModulator(SampleRate, 8000, 1);

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

                    if (max > 100)
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
                        int len = packetRecorder.AddSamples(preambleBuffer, pos + 1, preambleBuffer.Length - pos - 1);
                        writeTempCsv(preambleBuffer.Skip(pos - preamble.Data.Length).Take(preamble.Data.Length).ToArray(), "preamble_sample.csv");
                        writeTempCsv(preambleBuffer, "raw_samples.csv");

                        for (int i = 0; i < pos + len; i++)
                            preambleBuffer[i] = 0;

                        detectcnt = 0;
                    }
                }

                if (state == DecodeState.Decoding)
                {
                    int len = packetRecorder.AddSamples(floatBuffer, 0, floatBuffer.Length);
                    for (int i = 0; i < len; i++)
                        preambleBuffer[i] = 0;
                }
            };

            packetRecorder.NewPacket += (sender, packet) =>
            {
                state = DecodeState.Syncing;
                Console.WriteLine("New packet!");

                writeTempCsv(packet.Samples, "samples.csv");

                BitArray result = Modulator.Demodulate(packet);

                int wrong = 0;

                for (int i = 0; i < PacketLength; i++)
                {
                    if (result.Get(i) != bitArray.Get(i))
                    {
                        wrong++;
                        //Console.WriteLine($"Wrong bit at {i}, should be {bitArray.Get(i)}");
                    }
                    //else
                    //{
                    //    Console.WriteLine($"Correct bit at {i}: {bitArray.Get(i)}");
                    //}
                }
                Console.WriteLine($"Wrong: {wrong}");
            };

            Recorder.StartRecording();
            Thread.Sleep(1000000);
            Recorder.StopRecording();
        }

    }
}
