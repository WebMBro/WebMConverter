using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WebMConverter
{
    public partial class MainForm : Form
    {
        private string _template;
        public MainForm()
        {
            InitializeComponent();

            _template = "{1} -i \"{0}\" {2} -c:v libvpx {3} -crf 32 -b:v {4}K {5} -threads 0 {6} \"{7}\"";
            //{0} is input file
            //{1} is -ss TIME if seek enabled otherwise blank
            //{2} is -to TIME if to enabled otherwise blank
            //{3} is -an if no audio otherwise blank
            //{4} is bitrate in kb/s
            //{5} is -vf scale=WIDTH:HEIGHT if set otherwise blank
            //{6} is -fs 3M if 3MB limit enabled otherwise blank
            //{7} is output file

        }

        private void buttonBrowseIn_Click(object sender, EventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.CheckFileExists = true;
            dialog.CheckPathExists = true;
            dialog.ValidateNames = true;

            if (dialog.ShowDialog() == DialogResult.OK && !string.IsNullOrWhiteSpace(dialog.FileName))
                textBoxIn.Text = dialog.FileName;
        }

        private void buttonBrowseOut_Click(object sender, EventArgs e)
        {
            SaveFileDialog dialog = new SaveFileDialog();
            dialog.OverwritePrompt = true;
            dialog.ValidateNames = true;
            dialog.Filter = "WebM files|*.webm";

            if (dialog.ShowDialog() == DialogResult.OK && !string.IsNullOrWhiteSpace(dialog.FileName))
                textBoxOut.Text = dialog.FileName;
        }

        private void buttonGo_Click(object sender, EventArgs e)
        {
            string result = Go();
            if (!string.IsNullOrWhiteSpace(result))
                MessageBox.Show(result, "ERROR", MessageBoxButtons.OK, MessageBoxIcon.Error);

        }

        char[] invalidChars = Path.GetInvalidPathChars();

        private string Go()
        {
            string input = textBoxIn.Text;
            string output = textBoxOut.Text;

            if (string.IsNullOrWhiteSpace(input))
                return "No input file!";
            if (string.IsNullOrWhiteSpace(output))
                return "No output file!";

            if (invalidChars.Any(input.Contains))
                return "Input path contains invalid characters!\nInvalid characters: " + string.Join(" ", invalidChars);
            if (invalidChars.Any(output.Contains))
                return "Output path contains invalid characters!\nInvalid characters: " + string.Join(" ", invalidChars);

            if (!File.Exists(input))
                return "Input file doesn't exist!";

            int width = 0;
            int height = 0;

            if (!string.IsNullOrWhiteSpace(boxResW.Text) || !string.IsNullOrWhiteSpace(boxResH.Text))
            {
                if (!int.TryParse(boxResW.Text, out width))
                    return "Invalid width!";
                if (!int.TryParse(boxResH.Text, out height))
                    return "Invalid height!";
            }

            if ((!string.IsNullOrWhiteSpace(boxResW.Text) && string.IsNullOrWhiteSpace(boxResH.Text)) ||
                (string.IsNullOrWhiteSpace(boxResW.Text) && !string.IsNullOrWhiteSpace(boxResH.Text)))
                return "One of the width/height fields isn't filled in! Either fill none of them, or both of them!";

            //Try fo figure out if begin/end are correct
            //1. if it contains a :, it's probably a time, try to convert using DateTime.Parse
            //2. if not, try int.tryparse

            float startSeconds = 0;
            string start = "";
            string end = "";

            if (!string.IsNullOrWhiteSpace(boxCropFrom.Text))
            {
                if (boxCropFrom.Text.Contains(":"))
                {
                    TimeSpan timeStart;
                    if (!TimeSpan.TryParse(boxCropFrom.Text, CultureInfo.InvariantCulture, out timeStart))
                        return "Invalid start crop time!";
                    startSeconds = (float)timeStart.TotalSeconds;
                }
                else
                {
                    float timeStart;
                    if (!float.TryParse(boxCropFrom.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out timeStart))
                        return "Invalid start crop time!";
                    startSeconds = timeStart;
                }

                start = "-ss " + startSeconds.ToString(CultureInfo.InvariantCulture); //Convert comma to dot
            }

            float duration = 0;

            if (!string.IsNullOrWhiteSpace(boxCropTo.Text))
            {
                float endSeconds = 0;
                if (boxCropTo.Text.Contains(":"))
                {
                    TimeSpan timeEnd;
                    if (!TimeSpan.TryParse(boxCropTo.Text, CultureInfo.InvariantCulture, out timeEnd))
                        return "Invalid end crop time!";
                    endSeconds = (float)timeEnd.TotalSeconds;
                }
                else
                {
                    float timeEnd;
                    if (!float.TryParse(boxCropTo.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out timeEnd))
                        return "Invalid end crop time!";
                    endSeconds = timeEnd;
                }

                duration = endSeconds - startSeconds;
                if (duration <= 0)
                    return "Video is 0 or less seconds long!";

                end = "-to " + duration.ToString(CultureInfo.InvariantCulture); //Convert comma to dot
            }

            /*if (string.IsNullOrWhiteSpace(start) && !string.IsNullOrWhiteSpace(end) ||
                !string.IsNullOrWhiteSpace(start) && string.IsNullOrWhiteSpace(end))
                return "One of the crop time fields is empty! Either empty them both, or fill them both.";*/

            float limit = 0;
            string limitTo = "";
            if (!string.IsNullOrWhiteSpace(boxLimit.Text))
            {
                if (!float.TryParse(boxLimit.Text, out limit))
                    return "Invalid size limit!";
                limitTo = string.Format("-fs {0}M", limit.ToString(CultureInfo.InvariantCulture)); //Should turn comma into dot
            }

            string size = "";
            if (width != 0 && height != 0)
                size = string.Format("-vf scale={0}:{1}", width, height);

            //Calculate bitrate yourself!
            //1 megabyte = 8192 kilobits
            //3 megabytes = 24576 kilobits -> this is the limit
            //So if you have 60 seconds, the bitrate should be...
            //24576/60 = 409.6 kilobits/sec

            int bitrate = 900;
            if (duration != 0 && limit != 0) bitrate = (int)(8192 * limit / duration);

            if (!string.IsNullOrWhiteSpace(boxBitrate.Text))
                if (!int.TryParse(boxBitrate.Text, out bitrate))
                    return "Invalid bitrate!";

            string audio = "";
            if (!checkBoxAudio.Checked) //Remember, if the box ISN'T checked, the tag gets added
                audio = "-an";

            string arguments = string.Format(_template, input, start, end, audio, bitrate, size, limitTo, output);

            //Debug shit
            //MessageBox.Show(arguments);
            //return null;

            var form = new ConverterForm(arguments);
            form.ShowDialog();

            return null;
        }
    }
}
