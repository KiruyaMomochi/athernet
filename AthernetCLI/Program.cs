﻿using NAudio.Wave;
using System;
using System.Collections;
using System.Diagnostics;
using Athernet.Modulators;
using Athernet.Preambles.PreambleBuilders;
using System.Threading;
using System.IO;
using System.Linq;
using Athernet.Mac;
using Athernet.Physical;
using NAudio.Wave.SampleProviders;

namespace AthernetCLI
{
    class Program
    {
        private static Physical _physical;

        private static unsafe void Main(string[] args)
        {
            // ListOutputDevice();
            Trace.Listeners.Add(new TextWriterTraceListener(Console.Out));
            
            var watch = System.Diagnostics.Stopwatch.StartNew();
            var ewh = new EventWaitHandle(false, EventResetMode.AutoReset);

            // Console.WriteLine(frame.Src);
            
            var modulator = new DpskModulator(48000, 8000)
            {
                BitDepth = 3
            };
            var preamble = new WuPreambleBuilder(48000, 0.015f).Build();
                        _physical = new Physical(modulator, 1000)
            {
                Preamble = preamble,
                PlayChannel = Channel.Mono
            };
            _physical.StartPlaying();
            
            PlayReceive();

            _physical.PlayStopped += (sender, eventArgs) => ewh.Set();
            ewh.WaitOne();
            
            watch.Stop();
            Console.WriteLine($"Time elapsed: {watch.ElapsedMilliseconds} ms.");
        }

        private static void ListAllDevices()
        {
            foreach (var dev in DirectSoundOut.Devices)
            {
                Console.WriteLine($"{dev.Guid} {dev.ModuleName} {dev.Description}");
            }
        }

        private static void PlayTest()
        {
            var waveIn = new WaveInEvent()
            {
                WaveFormat = WaveFormat.CreateIeeeFloatWaveFormat(48000, 1)
            };

            using var writer = new WaveFileWriter(Path.Combine(Path.GetTempPath(), "test.wav"), waveIn.WaveFormat);

            waveIn.DataAvailable += (sender, eventArgs) =>
                writer.Write(eventArgs.Buffer, 0, eventArgs.BytesRecorded);
            waveIn.StartRecording();
            Thread.Sleep(500);
            PlayOnly();
            Thread.Sleep(500);
            waveIn.StopRecording();
        }

        private static void PlayOnly()
        {
            var modulator = new DpskModulator(48000, 8000)
            {
                BitDepth = 48
            };
            var preamble = new WuPreambleBuilder(48000, 0.1f).Build();
            // Athernet.Utils.Debug.WriteTempWav(preamble, "real_preamble.wav");

            var physical = new Physical(modulator, 6250)
            {
                Preamble = preamble,
                PlayChannel = Channel.Mono
            };

            byte[] template = new byte[physical.PayloadBytes];

            for (int i = 0; i < template.Length; i++)
            {
                template[i] |= (byte) i;
            }

            var ewh = new EventWaitHandle(false, EventResetMode.AutoReset);

            physical.AddPayload(template);
            physical.PlayStopped += (sender, args) => ewh.Set();

            ewh.WaitOne();
        }

        private static int PlayReceive()
        {
            // Athernet.Utils.Debug.WriteTempWav(preamble, "real_preamble.wav");

            byte[] template = new byte[(int) (_physical.PayloadBytes)];
            byte[] result = new byte[0];

            int wrongcnt = 0;

            for (int i = 0; i < template.Length; i++)
            {
                template[i] |= (byte) (i % 255);
            }

            var ewh = new EventWaitHandle(false, EventResetMode.AutoReset);
            
            // physical.StartReceive();
            for (int i = 0; i < 6250 / _physical.PayloadBytes; i++)
            {
                _physical.AddPayload(template);
            }

            int irecv = 0;
            _physical.PacketDetected += (sender, eventArgs)
                => Console.WriteLine("New packet detected.");
            _physical.DataAvailable += (sender, eventArgs) =>
            {
                irecv++;
                
                if (eventArgs.Data == null)
                {
                    Console.WriteLine("Crc failed.");
                    return;
                }

                result = eventArgs.Data;
                if (result.Length != template.Length) return;
                var wrong = result.Select((t, i) => (t != template[i] ? i : -1)).Where(t => t != -1);
                var ints = wrong as int[] ?? wrong.ToArray();
                Console.WriteLine($"Wrong num: {ints.Length}");
                foreach (var t in ints)
                {
                    Console.Write($"{t} ");
                }

                if (ints.Length != 0)
                {
                    wrongcnt++;
                }

                Console.WriteLine();

                if (irecv == 6250 / _physical.PayloadBytes)
                {
                    ewh.Set();
                }
            };

            // ewh.WaitOne();
            // physical.StopReceive();

            return wrongcnt;
        }

        static void ListOutputDevice()
        {
            foreach (var device in DirectSoundOut.Devices)
            {
                Console.WriteLine($"{device.Description} {device.Guid}");
            }
        }

        static void ListUsingDevice()
        {
            for (int i = -1; i < WaveIn.DeviceCount; i++)
            {
                var caps = WaveIn.GetCapabilities(i);
                Console.WriteLine($"WaveIn device #{i}: {caps.ProductName}");
            }

            Console.WriteLine();
            
            for (int i = -1; i < WaveOut.DeviceCount; i++)
            {
                var caps = WaveOut.GetCapabilities(i);
                Console.WriteLine($"WaveOut device #{i}: {caps.ProductName}");
            }
        }
    }
}
