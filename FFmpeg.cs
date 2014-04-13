using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace WebMConverter
{
    class FFmpeg //Wrapper class to run the FFmpeg process
    {
        public string FFmpegPath = Path.Combine(Environment.CurrentDirectory, "ffmpeg/ffmpeg.exe");
        public Process Process;

        public FFmpeg(string argument)
        {
            Process = new Process();

            ProcessStartInfo info = new ProcessStartInfo(FFmpegPath);
            info.RedirectStandardInput = true;
            info.RedirectStandardOutput = true;
            info.RedirectStandardError = true;
            info.UseShellExecute = false; //Required to redirect IO streams
            info.CreateNoWindow = true; //Hide console
            info.Arguments = argument;

            Process.StartInfo = info;
            Process.EnableRaisingEvents = true; //!!!!

            //Do the following calling start!
            // Process.ErrorDataReceived += stuff;
            // Process.OutputDataReceived += stuff;
            // Process.Exited += stuff

            //Start();
        }

        public void Start()
        {
            Process.Start();
            Process.BeginErrorReadLine();
            Process.BeginOutputReadLine();
        }
    }
}
