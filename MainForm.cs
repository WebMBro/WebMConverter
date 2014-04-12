using System;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace WebMConverter
{
    public partial class MainForm : Form
    {
        private string _template;
        private string _templateArguments;

        private string _autoOutput;
        private string _autoTitle;
        private string _autoArguments;
        private bool _argumentError;

        public Size AssumedInputSize; //This will get set as soon as the crop form generates an input file. It's assumed because the user could've changed the video after cropping.
        //Might want to get a definite, reliable way to get the size of the input video.

        public static string FFmpeg = Path.Combine(Environment.CurrentDirectory, "ffmpeg/ffmpeg.exe");

        public RectangleF CroppingRectangle  //This is in the [0-1] region, multiply it by the resolution to get the crop coordinates in pixels
        {
            get { return _croppingRectangle; }
            set
            {
                _croppingRectangle = value;
                if (_croppingRectangle == CropForm.FullCrop)
                    labelCrop.Text = "Don't crop";
                else
                    labelCrop.Text = string.Format(CultureInfo.InvariantCulture, "X:{0:0%} Y:{1:0%} W:{2:0%} H:{3:0%}",
                                                   _croppingRectangle.X,
                                                   _croppingRectangle.Y,
                                                   _croppingRectangle.Width,
                                                   _croppingRectangle.Height);
            }
        }
        private RectangleF _croppingRectangle; //Using a backing field so we can update the label as soon as something changed it!

        //public RectangleF CroppingRectangle; //This is in the [0-1] region, multiply it by the resolution to get the crop coordinates in pixels

        public MainForm()
        {
            InitializeComponent();

            CroppingRectangle = new RectangleF(0, 0, 1, 1); //Crop nothing by default

            AllowDrop = true;
            DragEnter += HandleDragEnter;
            DragDrop += HandleDragDrop;

            _templateArguments = "{0} -c:v libvpx -crf 32 -b:v {1}K {2} {3} -threads {4} {5} {6}";
            //{0} is -an if no audio, otherwise blank
            //{1} is bitrate in kb/s
            //{2} is -vf scale=WIDTH:HEIGHT if set otherwise blank
            //{3} is -filter:v "crop=out_w:out_h:x:y" if set otherwise blank
            //{4} is amount of threads to use
            //{5} is -fs 3M if 3MB limit enabled otherwise blank
            //{6} is -metadata title="TITLE" when specifying a title, otherwise blank
            _template = "{2} -i \"{0}\" {3} {4} {5} -f webm \"{1}\"";
            //{0} is input file
            //{1} is output file
            //{2} is TIME if seek enabled otherwise blank
            //{3} is TIME if to enabled otherwise blank
            //{4} is extra arguments
            //{5} is pass number if 2-pass enabled otherwise blank
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            //Keeping this disabled for now because threads are crashy

            //int threads = Environment.ProcessorCount;  //Set thread slider to default of 4
            //trackThreads.Value = Math.Min(trackThreads.Maximum, Math.Max(trackThreads.Minimum, threads));

            trackThreads_Scroll(sender, e); //Update label
        }

        private void buttonBrowseIn_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog dialog = new OpenFileDialog())
            {
                dialog.CheckFileExists = true;
                dialog.CheckPathExists = true;
                dialog.ValidateNames = true;

                if (dialog.ShowDialog() == DialogResult.OK && !string.IsNullOrWhiteSpace(dialog.FileName))
                    SetFile(dialog.FileName);
            }
        }

        private void SetFile(string path)
        {
            textBoxIn.Text = path;
            string fullPath = Path.GetDirectoryName(path);
            string name = Path.GetFileNameWithoutExtension(path);
            if (boxMetadataTitle.Text == _autoTitle || boxMetadataTitle.Text == "")
                boxMetadataTitle.Text = _autoTitle = name;
            if (textBoxOut.Text == _autoOutput || textBoxOut.Text == "")
                textBoxOut.Text = _autoOutput = Path.Combine(fullPath, name + ".webm");
        }

        private void HandleDragEnter(object sender, DragEventArgs e)
        {
            // show copy cursor for files
            bool dataPresent = e.Data.GetDataPresent(DataFormats.FileDrop);
            e.Effect = dataPresent ? DragDropEffects.Copy : DragDropEffects.None;
        }

        private void HandleDragDrop(object sender, DragEventArgs e)
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            foreach (string file in files) SetFile(file);
        }

        private void buttonBrowseOut_Click(object sender, EventArgs e)
        {
            using (SaveFileDialog dialog = new SaveFileDialog())
            {
                dialog.OverwritePrompt = true;
                dialog.ValidateNames = true;
                dialog.Filter = "WebM files|*.webm";

                if (dialog.ShowDialog() == DialogResult.OK && !string.IsNullOrWhiteSpace(dialog.FileName))
                    textBoxOut.Text = dialog.FileName;
            }
        }

        private void buttonGo_Click(object sender, EventArgs e)
        {
            string result = Convert();
            if (!string.IsNullOrWhiteSpace(result))
                MessageBox.Show(result, "ERROR", MessageBoxButtons.OK, MessageBoxIcon.Error);

        }

        char[] invalidChars = Path.GetInvalidPathChars();

        private string Convert()
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

            if (input == output)
                return "Input and output files are the same!";

            float startSeconds, endSeconds;

            try
            {
                startSeconds = ParseTime(boxCropFrom.Text);
            }
            catch (ArgumentException)
            {
                return "Invalid start crop time!";
            }
            try
            {
                endSeconds = ParseTime(boxCropTo.Text);
            }
            catch (ArgumentException)
            {
                return "Invalid end crop time!";
            }

            string options = textBoxArguments.Text;
            try
            {
                if (options.Trim() == "" || _argumentError)
                    options = GenerateArguments();
            }
            catch (ArgumentException e)
            {
                return e.Message;
            }

            string start = "";
            string end = "";

            if (startSeconds != 0.0)
            {
                start = "-ss " + startSeconds.ToString(CultureInfo.InvariantCulture); //Convert comma to dot
            }

            float duration = 0;

            if (endSeconds != 0.0)
            {
                duration = endSeconds - startSeconds;
                if (duration <= 0)
                    return "Video is 0 or less seconds long!";

                end = "-to " + duration.ToString(CultureInfo.InvariantCulture); //Convert comma to dot
            }

            string[] arguments;
            if (!checkBox2Pass.Checked)
                arguments = new[] { string.Format(_template, input, output, start, end, options, "") };
            else
            {
                int passes = 2; //Can you even use more than 2 passes?

                arguments = new string[passes];
                for (int i = 0; i < passes; i++)
                    arguments[i] = string.Format(_template, input, output, start, end, options, "-pass " + (i + 1));
            }

            var form = new ConverterForm(this, arguments);
            form.ShowDialog();

            return null;
        }

        public static float ParseTime(string text)
        {
            //Try fo figure out if begin/end are correct
            //1. if it contains a :, it's probably a time, try to convert using DateTime.Parse
            //2. if not, try int.tryparse

            if (!string.IsNullOrWhiteSpace(text))
            {
                if (text.Contains(":"))
                {
                    TimeSpan time;
                    if (!TimeSpan.TryParse(MakeParseFriendly(text), CultureInfo.InvariantCulture, out time))
                        throw new ArgumentException("Invalid time!");
                    return (float)time.TotalSeconds;
                }
                else
                {
                    float time;
                    if (!float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out time))
                        throw new ArgumentException("Invalid time!");
                    return time;
                }
            }
            return 0.0f;
        }

        private void UpdateArguments(object sender, EventArgs e)
        {
            try
            {
                string arguments = GenerateArguments();
                if (arguments != _autoArguments || _argumentError)
                {
                    textBoxArguments.Text = _autoArguments = arguments;
                    _argumentError = false;
                }
            }
            catch (ArgumentException argExc)
            {
                textBoxArguments.Text = "ERROR: " + argExc.Message;
                _argumentError = true;
            }
        }

        private string GenerateArguments()
        {
            string args = "";
            int width = 0;
            int height = 0;

            if (!string.IsNullOrWhiteSpace(boxResW.Text) || !string.IsNullOrWhiteSpace(boxResH.Text))
            {
                if (!int.TryParse(boxResW.Text, out width))
                    throw new ArgumentException("Invalid width!");
                if (!int.TryParse(boxResH.Text, out height))
                    throw new ArgumentException("Invalid height!");
            }

            if ((!string.IsNullOrWhiteSpace(boxResW.Text) && string.IsNullOrWhiteSpace(boxResH.Text)) ||
                (string.IsNullOrWhiteSpace(boxResW.Text) && !string.IsNullOrWhiteSpace(boxResH.Text)))
                throw new ArgumentException("One of the width/height fields isn't filled in! Either fill none of them, or both of them!");

            string sizeCrop = "";
            if (_croppingRectangle != CropForm.FullCrop)
            {
                //Okay so here's the plan
                //1. Get the width of the video. If this is -1, you need to get the aspect ratio of the video somehow and then calculate the width from the height
                //2. Get the height of the video. If this is -1, you need to get the aspect ratio of the video somehowand then calculate the height from the width
                //If you can't get them, disallow the use of -1 if you're cropping the video at all!
                //3. If they are both filled in, you're in luck. Now you can calculate the pixel values for all 4 parameters for the crop.

                int assumedWidth = width;
                int assumedHeight = height;
                if (width == -1 || height == -1)  //TODO: allow this
                    throw new ArgumentException("Sorry, but you can't crop while using -1 in one of the resolution fields.");
                if (width == 0 || height == 0) 
                {
                    //The AssumedInputSize is the size of the last preview image generated while using the cropping tool.
                    //It's a good assumption, unless the user changes the input video after cropping.
                    assumedWidth = AssumedInputSize.Width;
                    assumedHeight = AssumedInputSize.Height;

                    if (assumedWidth == 0 || assumedHeight == 0)
                        throw new ArgumentException("For some reason you've cropped without generating a preview image.");
                }

                int cropX = (int)(assumedWidth * _croppingRectangle.X);
                int cropY = (int)(assumedHeight * _croppingRectangle.Y);
                int cropW = (int)(assumedWidth * _croppingRectangle.Width);
                int cropH = (int)(assumedHeight * _croppingRectangle.Height);

                sizeCrop = string.Format("-filter:v crop=\"{0}:{1}:{2}:{3}\"", cropW, cropH, cropX, cropY);
            }

            float startSeconds = ParseTime(boxCropFrom.Text);

            float duration = 0;

            float endSeconds = ParseTime(boxCropTo.Text);
            if (endSeconds != 0.0)
            {
                duration = endSeconds - startSeconds;
                if (duration <= 0)
                    throw new ArgumentException("Video is 0 or less seconds long!");
            }

            float limit = 0;
            string limitTo = "";
            if (!string.IsNullOrWhiteSpace(boxLimit.Text))
            {
                if (!float.TryParse(boxLimit.Text, out limit))
                    throw new ArgumentException("Invalid size limit!");
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
            if (duration != 0 && limit != 0)
            {
                bitrate = (int)(8192 * limit / duration);
            }

            if (!string.IsNullOrWhiteSpace(boxBitrate.Text))
                if (!int.TryParse(boxBitrate.Text, out bitrate))
                    throw new ArgumentException("Invalid bitrate!");

            int threads = trackThreads.Value;

            string metadataTitle = "";
            if (!string.IsNullOrWhiteSpace(boxMetadataTitle.Text))
                metadataTitle = string.Format("-metadata title=\"{0}\"", boxMetadataTitle.Text.Replace("\"", "\\\""));

            string audioEnabled = boxAudio.Checked ? "" : "-an"; //-an if no audio
            return string.Format(_templateArguments, audioEnabled, bitrate, size, sizeCrop, threads, limitTo, metadataTitle);
        }

        private static string MakeParseFriendly(string text)
        {
            //This method adds "00:" in front of text, if the text format is in either 00:00 or 00:00.00 format.
            //This pattern should work.

            string pattern = @"^[0-5][0-9]:[0-5][0-9](\.[0-9]+)?$";
            Regex regex = new Regex(pattern, RegexOptions.Singleline);
            if (regex.IsMatch(text))
                return "00:" + text;
            return text;
        }

        private void trackThreads_Scroll(object sender, EventArgs e)
        {
            labelThreads.Text = trackThreads.Value.ToString();
            UpdateArguments(sender, e);
        }

        private void buttonOpenCrop_Click(object sender, EventArgs e)
        {
            new CropForm(this).ShowDialog();
            UpdateArguments(sender, e);
        }
    }
}
