using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace WebMConverter
{
    class FFmpeg : Process //Refactoring, faggots.
    {
        public string FFmpegPath = Path.Combine(Environment.CurrentDirectory, "ffmpeg/ffmpeg.exe");


        public FFmpeg(string argument)
        {
            this.StartInfo.FileName = FFmpegPath;
            this.StartInfo.Arguments = argument;
            this.StartInfo.RedirectStandardInput = true;
            this.StartInfo.RedirectStandardOutput = true;
            this.StartInfo.RedirectStandardError = true;
            this.StartInfo.UseShellExecute = false; //Required to redirect IO streams
            this.StartInfo.CreateNoWindow = true; //Hide console
            this.EnableRaisingEvents = true; 
        }

        new public void Start()
        {
            base.Start();
            this.BeginErrorReadLine();
            this.BeginOutputReadLine();
        }
    }
}
