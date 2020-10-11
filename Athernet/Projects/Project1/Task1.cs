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
                WaveFormat = new WaveFormat(48000, 2)
            };
            string tmpfile = Path.GetTempFileName();
            string file = tmpfile.Replace(".tmp", ".wav");
            File.Move(tmpfile, file);

            var writer = new WaveFileWriter(file, waveIn.WaveFormat);

            waveIn.DataAvailable += (s, a) =>
            {
                writer.Write(a.Buffer, 0, a.BytesRecorded);
                writer.Flush();
                if (writer.TotalTime.TotalMilliseconds >= 10000)
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
            var athernet = new Athernet.Physical()
            {
                Preamble = new WuPreambleBuilder(48000, 0.1f).Build(),
                PlayChannel = Athernet.Physical.Channel.Right,
                FrameBodyBits = 2000,
                Modulator = new DPSKModulator(48000, 8000, 1)
                {
                    BitDepth = 32
                }
            };

            var template = Athernet.Utils.General.FileToBits(fileName);
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
            var athernet = new Athernet.Physical()
            {
                Preamble = new WuPreambleBuilder(48000, 0.1f).Build(),
                PlayChannel = Athernet.Physical.Channel.Right,
                FrameBodyBits = 2000,
                Modulator = new DPSKModulator(48000, 8000, 1)
                {
                    BitDepth = 32
                }
            };

            BitArray bitArray = new BitArray(maxLength);
            int wrong = 0;
            int idx = 0;

            athernet.DataAvailable += (s, e) =>
            {
                int lcWrong = 0;
                for (int i = 0; i < e.Data.Length && idx < maxLength; i++)
                {
                    bitArray[idx] = e.Data[i];
                    idx++;
                }
                wrong += lcWrong;
                Console.WriteLine(lcWrong);

                if (idx == maxLength)
                {
                    ewh.Set();
                    athernet.StopRecording();
                }
            };
            athernet.StartRecording();
            ewh.WaitOne();

            return bitArray;
        }
    }
}

