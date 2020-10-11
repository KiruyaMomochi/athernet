using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System.Threading.Tasks;
using System.Threading;
using ABI.System.ComponentModel;
using Windows.Storage.Pickers;
using WinRT;

using System.Runtime.InteropServices;
using System.Collections;
using ABI.Windows.Storage;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace AthernetGUI
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class Project1Page : Page
    {
        public Project1Page()
        {
            this.InitializeComponent();
        }

        private string T1File = null;
        private string T2File = null;
        private string T2PlayFile = null;
        private string T4PlayFile = null;
        private string T4SaveFile = null;
        private BitArray T4CompareBits = null;
        private BitArray T4Data = null;

        private NAudio.Wave.WaveOutEvent playEvent = null;

        private async void Task1RecordButton_Click(object sender, RoutedEventArgs e)
        {
            if (T1File != null && File.Exists(T1File))
            {
                File.Delete(T1File);
            }

            int time = 10;

            var rec = Task.Run(() => Athernet.Projects.Project1.Task1.Record(time));

            Task1Status.Text = "Recording";
            Task1Progress.Value = 0;
            while (Task1Progress.Value < 100 && !rec.IsCompleted)
            {
                Task1Progress.Value += 1;
                Task1Detail.Text = $"{Math.Ceiling(Task1Progress.Value * time * 10 / 1000)} s";
                await Task.Delay(time * 10);
            }

            T1File = await rec;
            Task1Status.Text = "Finished";
            Task1Detail.Text = T1File;
            Task1Progress.Value = 100;
            Task1PlayButton.IsEnabled = true;
        }

        private async void Task1PlayButton_Click(object sender, RoutedEventArgs e)
        {
            int time = 10;
            var play = Task.Run(() => Athernet.Projects.Project1.Task1.Play(T1File));

            Task1Status.Text = "Playing";
            Task1Progress.Value = 0;
            while (Task1Progress.Value < 100 && !play.IsCompleted)
            {
                Task1Progress.Value += 1;
                Task1Detail.Text = $"{Math.Ceiling(Task1Progress.Value * time * 10 / 1000)} s";
                await Task.Delay(time * 10);
            }

            await play;
            Task1Progress.Value = 100;
            Task1Status.Text = "Finished";
            Task1Detail.Text = T1File;
        }

        private async void Task2PickButton_Click(object sender, RoutedEventArgs e)
        {
            var picker = new Windows.Storage.Pickers.FileOpenPicker();
            //Make file Picker work in Win32
            IntPtr windowHandle = (App.Current as App).WindowHandle;
            InitializeWithWindowWrapper initializeWithWindowWrapper = InitializeWithWindowWrapper.FromAbi(picker.ThisPtr);
            initializeWithWindowWrapper.Initialize(windowHandle);
            picker.FileTypeFilter.Add(".wav");

            Windows.Storage.StorageFile file = await picker.PickSingleFileAsync();
            if (file != null)
            {
                Task2Status.Text = "Picked ";
                Task2Detail.Text = file.Name;
                T2PlayFile = file.Path;

                Task2RecordButton.IsEnabled = true;
            }
            else
            {
                Task2Status.Text = "Please choose a file!";
            }
        }

        private async void Task2RecordButton_Click(object sender, RoutedEventArgs e)
        {
            if (T2File != null && File.Exists(T2File))
            {
                File.Delete(T2File);
            }

            int time = 10;

            var play = Task.Run(() => Athernet.Projects.Project1.Task2.Play(T2PlayFile, 10));
            var rec = Task.Run(() => Athernet.Projects.Project1.Task1.Record(time));

            Task2Status.Text = "Recording";
            Task2Progress.Value = 0;
            while (Task2Progress.Value < 100 && !rec.IsCompleted)
            {
                Task2Progress.Value += 1;
                Task2Detail.Text = $"{Math.Ceiling(Task2Progress.Value * time * 10 / 1000)} s";
                await Task.Delay(time * 10);
            }

            T2File = await rec;
            Task2Status.Text = "Finished";
            Task2Detail.Text = T2File;
            Task2Progress.Value = 100;
            Task2PlayButton.IsEnabled = true;
        }

        private async void Task2PlayButton_Click(object sender, RoutedEventArgs e)
        {
            int time = 10;
            var play = Task.Run(() => Athernet.Projects.Project1.Task1.Play(T2File));

            Task2Status.Text = "Playing";
            Task2Progress.Value = 0;
            while (Task2Progress.Value < 100 && !play.IsCompleted)
            {
                Task2Progress.Value += 1;
                Task2Detail.Text = $"{Math.Ceiling(Task2Progress.Value * time * 10 / 1000)} s";
                await Task.Delay(time * 10);
            }

            await play;
            Task2Progress.Value = 100;
            Task2Status.Text = "Finished";
            Task2Detail.Text = T2File;
        }

        private async void Task3PlayButton_Click(object sender, RoutedEventArgs e)
        {
            playEvent?.Dispose();
            playEvent = await Task.Run(() => Athernet.Projects.Project1.Task3.Play());
            playEvent.Play();
            Task3PlayButton.IsEnabled = false;
            Task3StopButton.IsEnabled = true;
            Task3ProgressRing.IsActive = true;
            Task3Status.Text = "Playing";
        }

        private async void Task3StopButton_Click(object sender, RoutedEventArgs e)
        {
            await Task.Run(() => playEvent.Stop());

            Task3PlayButton.IsEnabled = true;
            Task3StopButton.IsEnabled = false;
            Task3ProgressRing.IsActive = false;
            Task3Status.Text = "Stopped";
        }


        private async void Task4PickButton_Click(object sender, RoutedEventArgs e)
        {
            var picker = new Windows.Storage.Pickers.FileOpenPicker();
            //Make file Picker work in Win32
            IntPtr windowHandle = (App.Current as App).WindowHandle;
            InitializeWithWindowWrapper initializeWithWindowWrapper = InitializeWithWindowWrapper.FromAbi(picker.ThisPtr);
            initializeWithWindowWrapper.Initialize(windowHandle);
            picker.FileTypeFilter.Add(".txt");

            Windows.Storage.StorageFile file = await picker.PickSingleFileAsync();
            if (file != null)
            {
                Task4Status.Text = "Picked";
                Task4Detail.Text = file.Name;
                T4PlayFile = file.Path;

                Task4PlayButton.IsEnabled = true;
            }
            else
            {
                Task4Status.Text = "Please choose a file!";
            }
        }

        private async void Task4PlayButton_Click(object sender, RoutedEventArgs e)
        {
            Task4Status.Text = "Playing";
            var play = Task.Run(() => Athernet.Projects.Project1.Task4.Play(T4PlayFile));
            await play;
            Task4Status.Text = "Finished";
        }


        private async void Task4RecordButton_Click(object sender, RoutedEventArgs e)
        {
            Task4RecordStatus.Text = "Recording";
            Task4RecordDetail.Text = null;
            var play = Task.Run(() => Athernet.Projects.Project1.Task4.Receive(10000));
            T4Data = await play;
            Task4RecordStatus.Text = "Finished";
            Task4SaveButton.IsEnabled = true;

            Compare();
        }

        private void Compare()
        {
            if (T4CompareBits != null && T4Data != null)
            {
                int wrong = 0;
                for (int i = 0; i < Math.Min(T4Data.Length, T4CompareBits.Length); i++)
                {
                    if (T4Data[i] != T4CompareBits[i]) wrong++;
                }
                Task4RecordDetail.Text = $"{wrong} wrongs";
            }
        }

        //private async void TestButton_Click(object sender, RoutedEventArgs e)
        //{
        //    var savePicker = new Windows.Storage.Pickers.FileSavePicker();
        //    (savePicker.FileTypeChoices as Windows.Storage.Pickers.FilePickerFileTypesOrderedMap).Add("Plain Text", new List<string>() { ".txt" });
        //    savePicker.SuggestedFileName = "OUTPUT";

        //    //Make file Picker work in Win32
        //    IntPtr windowHandle = (App.Current as App).WindowHandle;
        //    InitializeWithWindowWrapper initializeWithWindowWrapper = InitializeWithWindowWrapper.FromAbi(savePicker.ThisPtr);
        //    initializeWithWindowWrapper.Initialize(windowHandle);

        //    Windows.Storage.StorageFile file = await savePicker.PickSaveFileAsync();
        //    if (file != null)
        //    {
        //        // Prevent updates to the remote version of the file until
        //        // we finish making changes and call CompleteUpdatesAsync.
        //        Windows.Storage.CachedFileManager.DeferUpdates(file);
        //        // write to file
        //        await Windows.Storage.FileIO.WriteTextAsync(file, file.Name);
        //        // Let Windows know that we're finished changing the file so
        //        // the other app can update the remote version of the file.
        //        // Completing updates may require Windows to ask for user input.
        //        Windows.Storage.Provider.FileUpdateStatus status =
        //            await Windows.Storage.CachedFileManager.CompleteUpdatesAsync(file);
        //        if (status == Windows.Storage.Provider.FileUpdateStatus.Complete)
        //        {
        //            TestStatus.Text = "Saved file";
        //        }
        //        else
        //        {
        //            TestStatus.Text = "Failed to save file";
        //        }
        //        TestDetail.Text = file.Name;
        //    }
        //}

        private async void Task4SaveButton_Click(object sender, RoutedEventArgs e)
        {
            var arr = new bool[T4Data.Length];
            T4Data.CopyTo(arr, 0);
            var str = string.Join(string.Empty, arr.Select(x => x switch
            {
                true => "0",
                false => "1"
            }));

            var folderPicker = new Windows.Storage.Pickers.FolderPicker();
            //Make file Picker work in Win32
            IntPtr windowHandle = (App.Current as App).WindowHandle;
            InitializeWithWindowWrapper initializeWithWindowWrapper = InitializeWithWindowWrapper.FromAbi(folderPicker.ThisPtr);
            initializeWithWindowWrapper.Initialize(windowHandle);
            folderPicker.FileTypeFilter.Add("*");

            Windows.Storage.StorageFolder folder = await folderPicker.PickSingleFolderAsync();
            if (folder == null)
            {
                return;
            }

            var file = await folder.CreateFileAsync("OUTPUT.txt");
            T4SaveFile = Task4SaveDetail.Text = file.Name;
            // Prevent updates to the remote version of the file until
            // we finish making changes and call CompleteUpdatesAsync.
            Windows.Storage.CachedFileManager.DeferUpdates(file);
            // write to file
            await Windows.Storage.FileIO.WriteTextAsync(file, str);
            // Let Windows know that we're finished changing the file so
            // the other app can update the remote version of the file.
            // Completing updates may require Windows to ask for user input.
            Windows.Storage.Provider.FileUpdateStatus status =
                await Windows.Storage.CachedFileManager.CompleteUpdatesAsync(file);
            if (status == Windows.Storage.Provider.FileUpdateStatus.Complete)
            {
                Task4SaveStatus.Text = "Saved file";
            }
            else
            {
                Task4SaveStatus.Text = "Failed to save file";
            }
            Task4SaveDetail.Text = file.Name;
        }

        private async void Task4CompareButton_Click(object sender, RoutedEventArgs e)
        {
            var picker = new Windows.Storage.Pickers.FileOpenPicker();
            //Make file Picker work in Win32
            IntPtr windowHandle = (App.Current as App).WindowHandle;
            InitializeWithWindowWrapper initializeWithWindowWrapper = InitializeWithWindowWrapper.FromAbi(picker.ThisPtr);
            initializeWithWindowWrapper.Initialize(windowHandle);
            picker.FileTypeFilter.Add(".txt");

            Windows.Storage.StorageFile file = await picker.PickSingleFileAsync();
            if (file != null)
            {
                Task4CompareStatus.Text = "Comparing with";
                Task4CompareDetail.Text = file.Name;
                T4CompareBits = Athernet.Utils.General.FileToBits(file.Path);
                Compare();
            }
        }

        #region IInitializeWithWindow
        [ComImport]
        [Guid("3E68D4BD-7135-4D10-8018-9FB6D9F33FA1")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        public interface IInitializeWithWindow
        {
            void Initialize(IntPtr hwnd);
        }

        // This is the actual wrapper for CSWinRT  
        [Guid("3E68D4BD-7135-4D10-8018-9FB6D9F33FA1")]
        internal class InitializeWithWindowWrapper : IInitializeWithWindow
        {
            [Guid("3E68D4BD-7135-4D10-8018-9FB6D9F33FA1")]
            public struct Vftbl
            {
                public delegate int _Initialize_0(IntPtr thisPtr, IntPtr hwnd);

                internal global::WinRT.Interop.IUnknownVftbl IUnknownVftbl;
                public _Initialize_0 Initialize_0;

                public static readonly Vftbl AbiToProjectionVftable;
                public static readonly IntPtr AbiToProjectionVftablePtr;

                static Vftbl()
                {
                    AbiToProjectionVftable = new Vftbl
                    {
                        IUnknownVftbl = global::WinRT.Interop.IUnknownVftbl.AbiToProjectionVftbl,
                        Initialize_0 = Do_Abi_Initialize_0
                    };
                    AbiToProjectionVftablePtr = Marshal.AllocHGlobal(Marshal.SizeOf<Vftbl>());
                    Marshal.StructureToPtr(AbiToProjectionVftable, AbiToProjectionVftablePtr, false);
                }

                private static int Do_Abi_Initialize_0(IntPtr thisPtr, IntPtr windowHandle)
                {
                    try
                    {
                        ComWrappersSupport.FindObject<IInitializeWithWindow>(thisPtr).Initialize(windowHandle);
                    }
                    catch (Exception ex)
                    {
                        return Marshal.GetHRForException(ex);
                    }
                    return 0;
                }
            }
            internal static ObjectReference<Vftbl> FromAbi(IntPtr thisPtr) => ObjectReference<Vftbl>.FromAbi(thisPtr);

            public static implicit operator InitializeWithWindowWrapper(IObjectReference obj) => (obj != null) ? new InitializeWithWindowWrapper(obj) : null;
            protected readonly ObjectReference<Vftbl> _obj;
            public IObjectReference ObjRef { get => _obj; }
            public IntPtr ThisPtr => _obj.ThisPtr;
            public ObjectReference<I> AsInterface<I>() => _obj.As<I>();
            public A As<A>() => _obj.AsType<A>();
            public InitializeWithWindowWrapper(IObjectReference obj) : this(obj.As<Vftbl>()) { }
            internal InitializeWithWindowWrapper(ObjectReference<Vftbl> obj)
            {
                _obj = obj;
            }

            public void Initialize(IntPtr windowHandle)
            {
                Marshal.ThrowExceptionForHR(_obj.Vftbl.Initialize_0(ThisPtr, windowHandle));
            }
        }
        #endregion
    }
}
