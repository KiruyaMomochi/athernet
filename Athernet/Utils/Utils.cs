using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using NAudio.Wave;

namespace Athernet.Utils
{
    public static class General
    {
        public static BitArray FileToBits(string fileName)
        {
            var file = File.ReadAllText(fileName);
            var arr = file.Split()[0].Select(x => x switch
            {
                '0' => false,
                '1' => true,
                _ => throw new NotImplementedException(),
            });
            return new BitArray(arr.ToArray());
        }
    }

    public static class Maths
    {
        // From https://graphics.stanford.edu/~seander/bithacks.html
        /// <summary>
        /// Round up to next higher power of 2
        /// (return <paramref name="x"/> if already a power of 2)
        /// </summary>
        /// <param name="x">The number to be rounded</param>
        /// <returns>The rounded number</returns>
        public static int Power2RoundUp(int x)
        {
            if (x < 0)
                return 0;

            // comment out to always take the next biggest power of two, 
            // even if x is already a power of two
            --x;

            x |= x >> 1;
            x |= x >> 2;
            x |= x >> 4;
            x |= x >> 8;
            x |= x >> 16;
            return x + 1;
        }

        public static IEnumerable<bool> ToBits(IEnumerable<byte> bytes, Endianness endianness)
        {
            var mask = endianness switch
            {
                Endianness.LittleEndian => LittleByteMask,
                Endianness.BigEndian => BigByteMask,
                _ => throw new NotImplementedException()
            };

            foreach (var b in bytes)
            foreach (var b1 in mask)
                yield return (b & b1) != 0;
        }

        public static IEnumerable<byte> ToBytes(BitArray bitArray, Endianness endianness)
        {
            var bits = new bool[bitArray.Length];
            bitArray.CopyTo(bits, 0);
            return ToBytes(bits, endianness);
        }

        public static IEnumerable<byte> ToBytes(IEnumerable<bool> bits, Endianness endianness)
        {
            var mask = endianness switch
            {
                Endianness.LittleEndian => LittleByteMask,
                Endianness.BigEndian => BigByteMask,
                _ => throw new NotImplementedException()
            };

            byte b = 0;
            var idx = 0;

            foreach (var bit in bits)
            {
                if (bit)
                    b |= mask[idx];
                idx++;
                if (idx != 8)
                    continue;

                yield return b;
                b = 0;
                idx = 0;
            }
        }

        public enum Endianness
        {
            LittleEndian,
            BigEndian
        }

        public static readonly byte[] LittleByteMask = {1 << 0, 1 << 1, 1 << 2, 1 << 3, 1 << 4, 1 << 5, 1 << 6, 1 << 7};
        public static readonly byte[] BigByteMask = {1 << 7, 1 << 6, 1 << 5, 1 << 4, 1 << 3, 1 << 2, 1 << 1, 1 << 0};
    }

    public static class Network
    {
        public static void ICMPListener()
        {
            var icmpListener = new Socket(AddressFamily.InterNetwork, SocketType.Raw, ProtocolType.Icmp);
            icmpListener.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveTimeout, 5000);
            icmpListener.Bind(new IPEndPoint(IPAddress.Any, 0));

            // SIO_RCVALL Control Code
            // https://docs.microsoft.com/en-us/windows/win32/winsock/sio-rcvall
            //icmpListener.IOControl(IOControlCode.ReceiveAll, BitConverter.GetBytes(3), null);

            var buffer = new byte[100];
            EndPoint remoteEndPoint = new IPEndPoint(IPAddress.Any, 0);
            //icmpListener.SendTo(new byte[32], (EndPoint)(new IPEndPoint(IPAddress.Parse("10.20.216.184"), 0)));
            Console.WriteLine("Transfered.");

            while (true)
            {
                var bytesRead = icmpListener.ReceiveFrom(buffer, ref remoteEndPoint);

                Console.WriteLine($"ICMPListener received {bytesRead} from {remoteEndPoint}");
                Console.WriteLine(BitConverter.ToString(buffer));
            }
        }
    }

    public static class Audio
    {
        public static IEnumerable<float> ToFloatBuffer(in byte[] buffer, in int bytesRecorded, in int bitsPerSample)
        {
            var wave = new WaveBuffer(buffer);
                
            var floatBuffer = bitsPerSample switch
            {
                16 => wave.ShortBuffer.Take(bytesRecorded / 2).Select(x => (float) x),
                32 => wave.FloatBuffer.Take(bytesRecorded / 4),
                _ => throw new NotImplementedException(),
            };

            return floatBuffer;
        }
    }

    public static class Debug
    {
        public static void WriteTempCsv(float[] buffer, string fileName)
        {
            var path = Path.Combine(Path.GetTempPath(), fileName);
            File.WriteAllText(path, String.Join(", ", buffer));
        }

        public static void WriteTempWav(float[] samples, string filename)
        {
            var path = Path.Combine(Path.GetTempPath(), filename);
            var file = new FileInfo(path);
            try
            {
                using Stream stream = file.Open(FileMode.OpenOrCreate, FileAccess.Write, FileShare.None);
                using var wave = new WaveFileWriter(stream, WaveFormat.CreateIeeeFloatWaveFormat(48000, 1));
                var provider = new Athernet.SampleProviders.MonoRawSampleProvider(samples).ToWaveProvider();
                var bytebuffer = new byte[48000];

                var len = 48000;

                do
                {
                    len = provider.Read(bytebuffer, 0, 48000);
                    wave.Write(bytebuffer, 0, len);
                } while (len != 0);

                Trace.WriteLine($"--- {filename} write success.");
            }
            catch (IOException)
            {
                Trace.WriteLine($"--- {filename} write failed.");
            }
        }

        public static void PrintResult(BitArray bits)
        {
            foreach (var bit in bits)
            {
                Console.Write(bit switch
                {
                    true => "\nT",
                    false => "F",
                    _ => throw new NotImplementedException()
                } + " ");
            }

            Console.WriteLine();
        }

        public static void PlaySamples(IEnumerable<float> samples)
        {
            var ewh = new EventWaitHandle(false, EventResetMode.AutoReset);

            var w = new WaveOutEvent();
            w.Init(new Athernet.SampleProviders.MonoRawSampleProvider(samples));
            w.Play();

            w.PlaybackStopped += (sender, args) => ewh.Set();
            ewh.WaitOne();
        }

        public static void PlaySamples(IEnumerable<float> samples, Guid deviceGuid)
        {
            var ewh = new EventWaitHandle(false, EventResetMode.AutoReset);
            using var w = new DirectSoundOut(deviceGuid);
            w.Init(new Athernet.SampleProviders.MonoRawSampleProvider(samples));
            w.Play();

            w.PlaybackStopped += (sender, args) => ewh.Set();
            ewh.WaitOne();
        }
    }
}