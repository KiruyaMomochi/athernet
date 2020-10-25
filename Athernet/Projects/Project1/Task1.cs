using Athernet.Modulators;
using Athernet.Preambles.PreambleBuilders;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace Athernet.Projects.Project1
{
    public static class Task1
    {
        public static string Record(int seconds = 10, int deviceNumber = 0)
        {
            var ewh = new EventWaitHandle(false, EventResetMode.AutoReset);

            using var waveIn = new WaveInEvent()
            {
                DeviceNumber = deviceNumber,
                WaveFormat = WaveFormat.CreateIeeeFloatWaveFormat(48000, 1)
            };
            string tmpfile = Path.GetTempFileName();
            string file = tmpfile.Replace(".tmp", ".wav");
            File.Move(tmpfile, file);

            var writer = new WaveFileWriter(file, waveIn.WaveFormat);

            waveIn.DataAvailable += (s, a) =>
            {
                writer.Write(a.Buffer, 0, a.BytesRecorded);
                writer.Flush();
                if (writer.TotalTime.TotalMilliseconds >= seconds * 1000)
                {
                    waveIn.StopRecording();
                }
            };
            waveIn.RecordingStopped += (s, a) =>
            {
                writer?.Dispose();
                writer = null;
                ewh.Set();
            };
            waveIn.StartRecording();

            ewh.WaitOne();

            return file;
        }

        public static void Play(string fileName, int deviceNumber = 0)
        {
            var ewh = new EventWaitHandle(false, EventResetMode.AutoReset);

            using var waveOut = new WaveOutEvent()
            {
                DeviceNumber = deviceNumber
            };
            var reader = new WaveFileReader(fileName);
            waveOut.Init(reader);
            waveOut.Play();
            waveOut.PlaybackStopped += (s, a) =>
            {
                reader?.Dispose();
                reader = null;
                ewh.Set();
            };

            ewh.WaitOne();
        }
    }

    public static class Task2
    {
        public static void Play(string fileName, int maxPlayTime, int deviceNumber = 0)
        {
            using var waveOut = new WaveOutEvent()
            {
                DeviceNumber = deviceNumber
            };
            var reader = new WaveFileReader(fileName);
            waveOut.Init(reader);
            waveOut.Play();
            waveOut.PlaybackStopped += (s, a) =>
            {
                reader?.Dispose();
                reader = null;
            };

            Thread.Sleep(TimeSpan.FromSeconds(maxPlayTime));
            waveOut.Stop();
        }
    }

    public static class Task3
    {
        public static WaveOutEvent Play(int deviceNumber = 0)
        {
            var waveFormat = new WaveFormat(48000, 1);
            var waveOut = new WaveOutEvent()
            {
                DeviceNumber = deviceNumber
            };
            var mixer = new MixingSampleProvider(new[] {
                new SignalGenerator(waveFormat.SampleRate, waveFormat.Channels)
                {
                    Frequency = 1000
                },
                new SignalGenerator(waveFormat.SampleRate, waveFormat.Channels)
                {
                    Frequency = 10000
                }
            });

            waveOut.Init(mixer);
            return waveOut;
        }
    }

    public static class Task4
    {
        public static void Play(string fileName)
        {
            var ewh = new EventWaitHandle(false, EventResetMode.AutoReset);
            var modulator = new DpskModulator(48000, 8000, 1)
            {
                FrameBytes = 250,
                BitDepth = 32
            };
            var athernet = new Physical.Physical(modulator)
            {
                Preamble = new WuPreambleBuilder(48000, 0.1f).Build(),
                PlayChannel = Physical.Channel.Right
            };

            var bits = Athernet.Utils.General.FileToBits(fileName);
            var template = new byte[bits.Count / 8];
            bits.CopyTo(template, 0);
            athernet.Play(template);

            athernet.PlayStopped += (s, a) =>
            {
                ewh.Set();
            };
            ewh.WaitOne();
        }

        public static BitArray Receive(int maxLength)
        {
            var ewh = new EventWaitHandle(false, EventResetMode.AutoReset);
            var modulator = new DpskModulator(48000, 8000, 1)
            {
                FrameBytes = 250,
                BitDepth = 32
            };
            var athernet = new Physical.Physical(modulator)
            {
                Preamble = new WuPreambleBuilder(48000, 0.1f).Build(),
                PlayChannel = Physical.Channel.Right
            };

            BitArray bitArray = new BitArray(maxLength);
            int wrong = 0;
            int idx = 0;

            athernet.DataAvailable += (s, e) =>
            {
                int lcWrong = 0;
                BitArray data = new BitArray(e.Data);
                for (int i = 0; i < e.Data.Length && idx < maxLength; i++)
                {
                    bitArray[idx] = data[i];
                    idx++;
                }
                wrong += lcWrong;
                Console.WriteLine(lcWrong);

                if (idx == maxLength)
                {
                    ewh.Set();
                    athernet.StopReceive();
                }
            };
            athernet.StartReceive();
            ewh.WaitOne();

            return bitArray;
        }
    }
}

