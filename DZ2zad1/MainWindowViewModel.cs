using Microsoft.Win32;
using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace DZ2zad1
{
    public class MainWindowViewModel:BaseViewModel,IResetable
    {
        const int minPwdLength = 6;
        const int defaultSize = 4096;
        const int maxBuffSize = 65535;
        private ManualResetEvent mreWorking;
        private ManualResetEvent mreStop;
        private long cryptedBytes, restoredBytes;
        private int secondsWorking;
        private DispatcherTimer timer;
        private string path;

        public string Pathh
        {
            get { return path; }
            set
            {
                path = value;
                OnPropertyChanged("Pathh");
            }
        }
        private int sizeBuff;

        public string SizeBuff
        {
            get { return sizeBuff.ToString(); }
            set
            {
                int.TryParse(value, out sizeBuff);
                if (sizeBuff == 0 || sizeBuff > maxBuffSize)
                {
                    sizeBuff = defaultSize;
                }
                OnPropertyChanged("SizeBuff");
            }
        }

        private double progress;

        public double Progress
        {
            get { return progress; }
            set
            {
                if (progress!=value)
                {
                    progress = value;
                    OnPropertyChanged("Progress");
                }
            }
        }
        private string speed;
        public string Speed
        {
            get => speed;
            set
            {
                if (speed != value)
                {
                    speed = $"{value} KB/s";
                    OnPropertyChanged("Speed");
                }
            }
        }

        private string log;
        public string Log
        {
            get => log;
            set
            {
                log += $"[{DateTime.Now.ToLongTimeString()}] {value}{Environment.NewLine}";
                OnPropertyChanged("Log");
            }
        }
        public ICommand OpenFileCommand { get; }
        public ICommand StartCommand { get; }
        public ICommand StopCommand { get; }
        public MainWindowViewModel()
        {
            SizeBuff = defaultSize.ToString();

            mreStop = new ManualResetEvent(false);
            mreWorking = new ManualResetEvent(false);

            OpenFileCommand =new SimpleCommand(FileBrowseDialog);
            StartCommand = new AsyncCommand(Start, null, enable => !mreWorking.WaitOne(0));
            StopCommand = new SimpleCommand(Cancel, enable => mreWorking.WaitOne(0) && !mreStop.WaitOne(0));

            timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1d) };
            timer.Tick += SpeedMonitor;
        }

        private void SpeedMonitor(object sender, EventArgs e)
        {
            secondsWorking++;
            var speedInKbPerSec = (double)(cryptedBytes + restoredBytes) / secondsWorking / 1024;
            Speed = Math.Round(speedInKbPerSec, 2).ToString();
            secondsWorking++;
        }

        private void Cancel()
        {
            mreStop.Set();
            Log = "Cancel...";
        }

        private void Start(object obj)
        {
            try
            {
                Validate(obj);
                Log = "Crypting begin...";
                mreWorking.Set();
                timer.Start();
                var key =
                    System.Security.Cryptography
                        .MD5
                        .Create()
                        .ComputeHash(
                            Encoding.Default.GetBytes(
                                (obj as PasswordBox)?.Password));

                var buff = new byte[sizeBuff];

                using (var fs = new FileStream(path, FileMode.Open, FileAccess.ReadWrite))
                {
                    Encrypt(fs, buff, key);
                }
                Log = "Crypting finished";
            }
            catch (ArgumentNullException ex)
            {
                MessageBox.Show(ex.Message, "Null refference", MessageBoxButton.OK, MessageBoxImage.Error);
                Log = ex.Message;
            }
            catch (FormatException)
            {
                MessageBox.Show("Key size must be more than 6 symbols", "KEY error", MessageBoxButton.OK, MessageBoxImage.Warning);
                Log = "KEY error";
            }
            catch (FileNotFoundException ex)
            {
                MessageBox.Show(ex.Message, "Wrong file path", MessageBoxButton.OK, MessageBoxImage.Stop);
                Log = "Wrong file path";
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                Log = ex.Message;
            }
            finally
            {
                Reset();
            }
        }
        private void Encrypt(Stream stream, byte[] buff, byte[] key)
        {
            var fileSize = stream.Length;
            var onePercentInBytes = fileSize / 100;
            var percentBreakPoints = onePercentInBytes;

            var crypter = new StreamCrypt(new CryptXor());

            while (cryptedBytes < fileSize)
            {
                if (mreStop.WaitOne(0))
                {
                    Decrypt(stream, buff, key);
                    break;
                }
                
                var count = crypter.Crypt(stream, cryptedBytes, buff, key);

                cryptedBytes += count;
                if (cryptedBytes >= percentBreakPoints)
                {
                    Progress++;
                    percentBreakPoints += onePercentInBytes;
                }
            }
        }

        private void Decrypt(Stream stream, byte[] buff, byte[] key)
        {
            var fileSize = stream.Length;
            var onePercentInBytes = fileSize / 100;
            var percentBreakPoints = onePercentInBytes;

            var crypter = new StreamCrypt(new CryptXor());

            while (restoredBytes < cryptedBytes)
            {
                var count = crypter.Crypt(stream, restoredBytes, buff, key);

                restoredBytes += count;
                if (restoredBytes >= percentBreakPoints)
                {
                    Progress--;
                    percentBreakPoints += onePercentInBytes;
                }
            }
        }

        private void Validate(object o)
        {
            if (!File.Exists(path))
            {
                throw new FileNotFoundException(path);
            }
            if (!(o is PasswordBox pwd))
            {
                throw new ArgumentNullException(nameof(pwd));
            }

            if (pwd.Password.Length < minPwdLength)
            {
                throw new FormatException(nameof(pwd));
            }
        }

        private void FileBrowseDialog()
        {
            OpenFileDialog open=new OpenFileDialog();
            if (open.ShowDialog() == true)
            {
                Pathh = open.FileName;
            }
        }

        public void Reset()
        {
            Progress = 0d;
            timer.Stop();
            cryptedBytes = 0;
            restoredBytes = 0;
            secondsWorking = 0;
            Speed = string.Empty;
            mreStop.Reset();
            mreWorking.Reset();
        }
    }
}
