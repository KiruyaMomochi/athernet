using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace WavPlayer
{
    class Program
    {
        static void Main(string[] args)
        {
            var rootCommand = new RootCommand
            {
                new Option<FileInfo>(
                    "--file",
                    getDefaultValue: () => new FileInfo(@"C:\Users\xtyzw\Downloads\Jamming.wav"),
                    description: "The file to play"
                   )
            };

            rootCommand.Handler = CommandHandler.Create<FileInfo>(PlayWavForAllDevices);
            rootCommand.InvokeAsync(args).Wait();
        }

        private static void PlayWavForAllDevices(FileInfo file)
        {
            if (!file.Exists)
            {
                throw new FileNotFoundException($"The wav file {file.FullName} is not found.");
            }

            var taskList = new List<Task>();

            for (int i = -1; i < WaveOut.DeviceCount; i++)
            {
                var caps = WaveOut.GetCapabilities(i);
                Console.WriteLine($"WaveOut device #{i}\n\t {caps.ProductName}: {caps.ProductGuid}");

                if (caps.ProductName.Contains("USB"))
                {
                    var device = i;
                    taskList.Add(Task.Run(() => PlayWav(file, device)));
                }
            }

            Task.WaitAll(taskList.ToArray());
        }

        private static void PlayWav(FileInfo file, int device)
        {
            var ewh = new EventWaitHandle(false, EventResetMode.AutoReset);

            if (!file.Exists)
            {
                throw new FileNotFoundException($"The wav file {file.FullName} is not found.");
            }
            using var stream = file.Open(FileMode.Open, FileAccess.Read, FileShare.Read);
            var wo = new WaveOutEvent() { DeviceNumber = device };
            using var reader = new WaveFileReader(stream);
            wo.Init(reader);
            wo.Play();
            wo.PlaybackStopped += (s, e) => ewh.Set();
            ewh.WaitOne();
        }
    }
}
