using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Milkitic.ApplicationHost
{
    public abstract class AppHost
    {
        private Process _currentProcess;
        public event DataReceivedEventHandler DataReceived;
        public event DataReceivedEventHandler OutputDataReceived;
        public event DataReceivedEventHandler ErrorDataReceived;
        public event EventHandler Exited;

        public AppHost() : this(new HostSettings())
        {
        }

        public AppHost(HostSettings hostSettings) : this(null, hostSettings)
        {
        }

        public AppHost(string fileName)
            : this(fileName, new HostSettings())
        {
        }

        public AppHost(string fileName, HostSettings hostSettings)
            : this(fileName, null, hostSettings)
        {
        }

        public AppHost(string fileName, string args, HostSettings hostSettings)
        {
            HostSettings = hostSettings;
            FileName = fileName;
            Args = args;
        }

        public virtual void Run()
        {
            StartProcess();
        }

        public async Task RunAsync()
        {
            await Task.Run(() =>
            {
                Run();
                _currentProcess.WaitForExit();
            });
        }

        public virtual void Stop()
        {
            TryStopProcess();
        }

        public async Task StopAsync()
        {
            await Task.Run(() => Stop());
        }

        public void WaitForExit()
        {
            _currentProcess.WaitForExit();
        }

        public void SendMessage(string message)
        {
            if (HostSettings.RedirectStandardInput == false)
                return;
            _currentProcess.StandardInput.Write(message + _currentProcess.StandardInput.NewLine);
        }

        public override string ToString()
        {
            return $"{{{Guid}}} {base.ToString()}";
        }

        public Guid Guid { get; set; } = Guid.NewGuid();
        public string FileName { get; set; }
        public string Args { get; set; }
        public HostSettings HostSettings { get; set; }
        public bool? IsRunning
        {
            get
            {
                try
                {
                    return !_currentProcess?.HasExited;
                }
                catch (Exception e)
                {
                    return null;
                }
            }
        }

        public bool CanSendMessage => HostSettings.RedirectStandardInput;
        public Guid RunningGuid { get; set; }

        private void StartProcess()
        {
            Stop();
            _currentProcess = new Process
            {
                StartInfo =
                {
                    FileName = FileName,
                    Arguments = Args,
                    CreateNoWindow = !HostSettings.ShowWindow,
                    StandardOutputEncoding = HostSettings.Encoding,
                    UseShellExecute = false,
                    RedirectStandardInput = HostSettings.RedirectStandardInput,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    WindowStyle = HostSettings.ShowWindow ? ProcessWindowStyle.Normal : ProcessWindowStyle.Hidden,
                    WorkingDirectory = Path.GetDirectoryName(FileName)
                },
                EnableRaisingEvents = true
            };
            _currentProcess.OutputDataReceived += _currentProcess_OutputDataReceived;
            _currentProcess.ErrorDataReceived += _currentProcess_ErrorDataReceived;
            _currentProcess.OutputDataReceived += _currentProcess_DataReceived;
            _currentProcess.ErrorDataReceived += _currentProcess_DataReceived;
            _currentProcess.Exited += _currentProcess_Exited;

            try
            {
                _currentProcess.Start();
            }
            catch (Exception e)
            {
                _currentProcess.Dispose();
                _currentProcess = null;
                throw;
            }

            _currentProcess.BeginOutputReadLine();
            _currentProcess.BeginErrorReadLine();
            RunningGuid = Guid.NewGuid();
            Console.WriteLine($"[{DateTime.Now.ToLongTimeString()}] Process \"{_currentProcess.ProcessName}\" started.");
        }

        private void _currentProcess_Exited(object sender, EventArgs e)
        {
            Exited?.Invoke(sender, e);
        }

        private void _currentProcess_DataReceived(object sender, DataReceivedEventArgs e)
        {
            DataReceived?.Invoke(sender, e);
        }

        private void _currentProcess_ErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            ErrorDataReceived?.Invoke(sender, e);
        }

        private void _currentProcess_OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            OutputDataReceived?.Invoke(sender, e);
        }

        private void TryStopProcess()
        {
            if (_currentProcess == null) return;
            if (_currentProcess.HasExited) return;

            if (_currentProcess.CloseMainWindow())
            {
                if (!_currentProcess.WaitForExit(5000))
                {
                    _currentProcess.Kill();
                }
                if (!_currentProcess.WaitForExit(5000))
                {
                    throw new Exception();
                }
            }
            else
            {
                _currentProcess.Kill();

                if (!_currentProcess.WaitForExit(5000))
                {
                    throw new Exception();
                }
            }
        }
    }
}
