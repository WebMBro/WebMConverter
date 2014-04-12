using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;
using Timer = System.Windows.Forms.Timer;

namespace WebMConverter
{
    public partial class ConverterForm : Form
    {
        private string[] _arguments;
        private Process _process;
        private Timer _timer;
        private bool _ended;
        private bool _panic;

        int currentPass = 0;
        private bool _multipass;
        private bool _cancelMultipass;

        private MainForm _owner;

        public ConverterForm(MainForm mainForm, string[] args)
        {
            InitializeComponent();

            _arguments = args;
            _owner = mainForm;
        }

        private void ProcessOnErrorDataReceived(object sender, DataReceivedEventArgs args)
        {
            if (args.Data != null)
                textBoxOutput.Invoke((Action)(() => textBoxOutput.AppendText("\n" + args.Data)));
        }

        private void ProcessOnOutputDataReceived(object sender, DataReceivedEventArgs args)
        {
            if (args.Data != null)
                textBoxOutput.Invoke((Action)(() => textBoxOutput.AppendText("\n" + args.Data)));
        }

        private void ConverterForm_Load(object sender, EventArgs e)
        {
            string argument = null;
           _multipass = true;
            if (_arguments.Length == 1)
            {
                _multipass = false;
                argument = _arguments[0];
            }

            if (_multipass)
                for (int i = 0; i < _arguments.Length; i++)
                    textBoxOutput.AppendText(string.Format("\nArguments for pass {0}: {1}", i + 1, _arguments[i]));
            else
                textBoxOutput.AppendText("\nArguments: " + argument);

            if (_multipass)
                MultiPass(_arguments, MainForm.FFmpeg);
            else
                SinglePass(argument, MainForm.FFmpeg);
        }

        private void SinglePass(string argument, string ffmpeg)
        {
            _process = new Process();

            ProcessStartInfo info = new ProcessStartInfo(ffmpeg);
            info.RedirectStandardInput = true;
            info.RedirectStandardOutput = true;
            info.RedirectStandardError = true;
            info.UseShellExecute = false; //Required to redirect IO streams
            info.CreateNoWindow = true; //Hide console
            info.Arguments = argument;

            _process.StartInfo = info;
            _process.EnableRaisingEvents = true; //!!!!

            _process.ErrorDataReceived += ProcessOnErrorDataReceived;
            _process.OutputDataReceived += ProcessOnOutputDataReceived;

            _process.Exited += (o, args) => textBoxOutput.Invoke((Action)(() =>
                                                                              {
                                                                                  if (_panic) return; //This should stop that one exception when closing the converter
                                                                                  textBoxOutput.AppendText("\n--- FFMPEG HAS EXITED ---");
                                                                                  buttonCancel.Enabled = false;

                                                                                  _timer = new Timer();
                                                                                  _timer.Interval = 500;
                                                                                  _timer.Tick += Exited;
                                                                                  _timer.Start();
                                                                              }));

            _process.Start();
            _process.BeginErrorReadLine();
            _process.BeginOutputReadLine();
        }

        private void MultiPass(string[] arguments, string ffmpeg)
        {
            int passes = arguments.Length;

            //What a shame, so much copy paste going on here.
            _process = new Process();

            ProcessStartInfo info = new ProcessStartInfo(ffmpeg);
            info.RedirectStandardInput = true;
            info.RedirectStandardOutput = true;
            info.RedirectStandardError = true;
            info.UseShellExecute = false; //Required to redirect IO streams
            info.CreateNoWindow = true; //Hide console
            info.Arguments = arguments[currentPass];

            _process.StartInfo = info; 
            _process.EnableRaisingEvents = true; //!!!!

            _process.ErrorDataReceived += ProcessOnErrorDataReceived;
            _process.OutputDataReceived += ProcessOnOutputDataReceived;

            _process.Exited += (o, args) => textBoxOutput.Invoke((Action)(() =>
            {
                if (_panic) return; //This should stop that one exception when closing the converter
                textBoxOutput.AppendText("\n--- FFMPEG HAS EXITED ---");

                currentPass++;
                if (currentPass < passes && !_cancelMultipass)
                {
                    textBoxOutput.AppendText(string.Format("\n--- ENTERING PASS {0} ---", currentPass + 1));

                    MultiPass(arguments, ffmpeg); //Sort of recursion going on here, be careful with stack overflows and shit
                    return;
                }

                buttonCancel.Enabled = false;

                _timer = new Timer();
                _timer.Interval = 500;
                _timer.Tick += Exited;
                _timer.Start();
            }));

            _process.Start();
            _process.BeginErrorReadLine();
            _process.BeginOutputReadLine();
        }

        private void Exited(object sender, EventArgs eventArgs)
        {
            _timer.Stop();

            if (_process.ExitCode != 0)
            {
                if (_cancelMultipass)
                    textBoxOutput.AppendText("\n\nConversion cancelled.");
                else
                    textBoxOutput.AppendText(string.Format("\n\nffmpeg.exe exited with exit code {0}. That's usually bad.", _process.ExitCode));
                pictureBox.BackgroundImage = Properties.Resources.cross;

                if (_process.ExitCode == -1073741819) //This error keeps happening for me if I set threads to anything above 1, might happen for other people too
                    MessageBox.Show("It appears ffmpeg.exe crashed because of a thread error. Set the amount of threads to 1 in the advanced tab and try again.", "FYI", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                textBoxOutput.AppendText("\n\nVideo converted succesfully!");
                pictureBox.BackgroundImage = Properties.Resources.tick;

                buttonPlay.Enabled = true;
            }

            buttonCancel.Text = "Close";
            buttonCancel.Enabled = true;
            _ended = true;
        }

        private void buttonCancel_Click(object sender, EventArgs e)
        {
            _cancelMultipass = true;

            if (!_ended || _panic) //Prevent stack overflow
            {
                if (!_process.HasExited)
                    _process.Kill();
            }
            else
                Close();
        }

        private void ConverterForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            _panic = true; //Shut down while avoiding exceptions
            buttonCancel_Click(sender, e);
        }

        private void ConverterForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            _process.Dispose();
        }

        private void buttonPlay_Click(object sender, EventArgs e)
        {
            Process.Start(_owner.textBoxOut.Text); //Play result video
        }
    }
}
