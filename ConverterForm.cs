using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
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

        public ConverterForm(string[] args)
        {
            InitializeComponent();

            _arguments = args;

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
            bool multipass = true;
            if (_arguments.Length == 1)
            {
                multipass = false;
                argument = _arguments[0];
            }

            if (multipass)
                for (int i = 0; i < _arguments.Length; i++)
                    textBoxOutput.AppendText(string.Format("\nArguments for pass {0}: {1}", i+1, _arguments[i]));
            else
                textBoxOutput.AppendText("\nArguments: " + argument);

            string ffmpeg = Path.Combine(Environment.CurrentDirectory, "ffmpeg/ffmpeg.exe");

            if (multipass)
                MultiPass(_arguments, ffmpeg);
            else
                SinglePass(argument, ffmpeg);
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
                                                                                  if (_panic) return; //This should stop that one excetion when closing the converter
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

            _process.StandardInput.Write("y\n"); //should confirm overwrite?
        }

        int currentPass = 0;

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

            _process.StartInfo = info; //CRASH HERE?
            _process.EnableRaisingEvents = true; //!!!!

            _process.ErrorDataReceived += ProcessOnErrorDataReceived;
            _process.OutputDataReceived += ProcessOnOutputDataReceived;

            _process.Exited += (o, args) => textBoxOutput.Invoke((Action)(() =>
            {
                if (_panic) return; //This should stop that one excetion when closing the converter
                textBoxOutput.AppendText("\n--- FFMPEG HAS EXITED ---");

                currentPass++;
                if (currentPass < passes)
                {
                    textBoxOutput.AppendText(string.Format("\n--- ENTERING PASS {0} ---", currentPass));
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

            _process.StandardInput.Write("y\n"); //should confirm overwrite?
        }

        private void Exited(object sender, EventArgs eventArgs)
        {
            _timer.Stop();

            if (_process.ExitCode != 0)
            {
                textBoxOutput.AppendText(string.Format("\n\nffmpeg.exe exited with exit code {0}. That's usually bad.", _process.ExitCode));
                pictureBox.BackgroundImage = Properties.Resources.cross;
            }
            else
            {
                textBoxOutput.AppendText("\n\nVideo converted succesfully!");
                pictureBox.BackgroundImage = Properties.Resources.tick;
            }

            buttonCancel.Text = "Close";
           buttonCancel.Enabled = true;
           _ended = true;
        }

        private void buttonCancel_Click(object sender, EventArgs e)
        {
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
            //TODO: an exception gets thrown when the user closes the converter form while it's converting!
            _panic = true;
            buttonCancel_Click(sender, e);
        }
    }
}
