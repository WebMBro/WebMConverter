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
        private string _arguments;
        private Process _process;
        private Timer _timer;
        private bool _ended;
        private bool _panic;

        public ConverterForm(string arg)
        {
            InitializeComponent();

            _arguments = arg;

        }

        private void ConverterForm_Load(object sender, EventArgs e)
        {
            textBoxOutput.AppendText("Starting...");
            textBoxOutput.AppendText("\nArguments: " + _arguments);

            string ffmpeg = Path.Combine(Environment.CurrentDirectory, "ffmpeg/ffmpeg.exe");

            _process = new Process();

            ProcessStartInfo info = new ProcessStartInfo(ffmpeg);
            info.RedirectStandardInput = true;
            info.RedirectStandardOutput = true;
            info.RedirectStandardError = true;
            info.UseShellExecute = false; //Required to redirect IO streams
            info.CreateNoWindow = true; //Hide console
            info.Arguments = _arguments;

            _process.StartInfo = info;
            _process.EnableRaisingEvents = true; //!!!!

            _process.ErrorDataReceived += (o, args) =>
                                             {
                                                 if (args.Data != null)
                                                     textBoxOutput.Invoke((Action)(() => textBoxOutput.AppendText("\n" + args.Data)));
                                             };

            _process.OutputDataReceived += (o, args) =>
                                              {
                                                  if (args.Data != null)
                                                      textBoxOutput.Invoke((Action)(() => textBoxOutput.AppendText("\n" + args.Data)));
                                              };

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

            textBoxOutput.AppendText("\nffmpeg.exe is now converting your video.");

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
