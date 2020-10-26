using NAudio.Wave;
using System;
using System.Collections;
using Athernet.Modulators;
using Athernet.Preambles.PreambleBuilders;
using System.Threading;
using System.IO;
using System.Linq;
using Athernet.Physical;
using Athernet.Projects.Project1;

namespace AthernetCLI
{
    class Program
    {
        static void Main(string[] args)
        {
            // using USpeakers 2
            // var speaker = new Guid("50437f1f-ec8f-48ab-934c-c5beb21b0fe0");
            // var outDevice = new DirectSoundOut(speaker);

            var modulator = new DpskModulator(48000, 8000)
            {
                BitDepth = 36,
                FrameBytes = 100
            };
            var preamble = new WuPreambleBuilder(48000, 0.05f).Build();
            var physical = new Physical(modulator)
            {
                Preamble = preamble,
                PlayChannel = Channel.Mono
            };

            byte[] template = new byte[modulator.FrameBytes];
            byte[] result = null;
            
            for (int i = 0; i < template.Length; i++)
            {
                template[i] = (byte) i;
            }

            var ewh = new EventWaitHandle(false, EventResetMode.AutoReset);
            
            physical.StartReceive();
            physical.Play(template);
            
            physical.PacketDetected += (sender, eventArgs)
                => Console.WriteLine("New packet detected.");
            physical.DataAvailable += (sender, eventArgs) =>
            {
                result = eventArgs.Data;
                ewh.Set();
            };
            
            ewh.WaitOne();
            physical.StopReceive();

            if (result != null)
            {
                var wrong = result.Select((t, i) => (t != template[i] ? 1 : 0)).Sum();
                Console.WriteLine($"Wrong num: {wrong}");
            }
            else
            {
                Console.WriteLine("No data.");
            }
        }

        static void ListOutputDevice()
        {
            foreach (var device in DirectSoundOut.Devices)
            {
                Console.WriteLine($"{device.Description} {device.Guid}");
            }
        }
    }
}