using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace WebMConverter
{
    public partial class CropForm : Form
    {

        private Corner _heldCorner = Corner.None;
        private bool _held;

        private bool _insideForm; //Is the cursor inside the picturebox?
        private bool _insideRectangle; //Is the cursor inside the cropping rectangle?
        private Point _mousePos;
        private Point _mouseOffset; //This is in pixels, not a float

        public static readonly RectangleF FullCrop = new RectangleF(0, 0, 1, 1);
        private RectangleF _rectangle; //Rectangle is in [0-1, 0-1] format, this should make scaling easier

        private const int MaxDistance = 6; //Max distance to mouse from corner dots
        private Font _font = new Font(FontFamily.GenericSansSerif, 11f);

        private Process _process;
        private MainForm _owner;

        private string _previewFile = Path.Combine(Environment.CurrentDirectory, "tempPreview.png");
        private bool _generating;
        private string _message;
        private Image _image;

        private enum Corner
        {
            TopLeft,
            TopRight,
            BottomLeft,
            BottomRight,
            None,
        }

        public CropForm(MainForm mainForm)
        {
            _owner = mainForm;
            _rectangle = new RectangleF(0.25f, 0.25f, 0.5f, 0.5f); //Make sure the user knows where the dots are

            InitializeComponent();

            if (mainForm.CroppingRectangle != FullCrop)
                _rectangle = mainForm.CroppingRectangle;

            GeneratePreview();

            //Stub for video crop form
            //Template:
            //-filter:v "crop=out_w:out_h:x:y"

            //TODO: use this form to determine a cropping area for the video
            //Also, important, scale the preview to the size options the user entered in width x height. Take note of the -1 thing too!
            //I think ffmpeg first scales it down, then crops it.
            //TODO: when pressing Confirm, put the values in a new label that'll be added to the video options
            //TODO: maybe convert the float values from the rect to out_w/out_h/x/y yourself
        }

        private void GeneratePreview()
        {
            string argument = ConstructArguments();

            if (string.IsNullOrWhiteSpace(argument))
            {
                return;
            }

            _generating = true;

            _process = new Process();

            ProcessStartInfo info = new ProcessStartInfo(MainForm.FFmpeg);
            info.RedirectStandardInput = true;
            info.RedirectStandardOutput = true;
            info.RedirectStandardError = true;
            info.UseShellExecute = false; //Required to redirect IO streams
            info.CreateNoWindow = true; //Hide console
            info.Arguments = argument;

            _process.StartInfo = info;
            _process.EnableRaisingEvents = true; //!!!!

            _process.ErrorDataReceived += (sender, args) => Console.WriteLine(args.Data);
            _process.OutputDataReceived += (sender, args) => Console.WriteLine(args.Data);

            _process.Exited += (o, args) => pictureBoxVideo.Invoke((Action)(() =>
                                                                                {
                                                                                    _generating = false;

                                                                                    if (_process.ExitCode != 0)
                                                                                    {
                                                                                        _message = string.Format("ffmpeg.exe exited with exit code {0}. That's usually bad.", _process.ExitCode);
                                                                                        return;
                                                                                    }

                                                                                    if (!File.Exists(_previewFile))
                                                                                    {
                                                                                        _message = "The preview file wasn't generated, that means ffmpeg.exe failed. Confirm the following:\n- Is the input time actually smaller than the length than the input video?";
                                                                                        return;
                                                                                    }

                                                                                    try
                                                                                    {

                                                                                        //_image = Image.FromFile(_previewFile); //Thank you for not releasing the lock on the file afterwards, Microsoft

                                                                                        using (FileStream stream = new FileStream(_previewFile, FileMode.Open, FileAccess.Read))
                                                                                        {
                                                                                            _image = Image.FromStream(stream);
                                                                                        }

                                                                                        pictureBoxVideo.BackgroundImage = _image;
                                                                                        File.Delete(_previewFile);

                                                                                        _owner.AssumedInputSize = _image.Size; //We assume the size of the preview will also be the size of the input used for the conversion

                                                                                        float aspectRatio = _image.Width / (float)_image.Height;
                                                                                        ClientSize = new Size((int)(ClientSize.Height * aspectRatio), ClientSize.Height);
                                                                                    }
                                                                                    catch (Exception e)
                                                                                    {
                                                                                        _message = e.ToString();
                                                                                    }
                                                                                }));

            _process.Start();
            _process.BeginErrorReadLine();
            _process.BeginOutputReadLine();

            _process.StandardInput.Write("y\n"); //Confirm overwrite
        }

        private string ConstructArguments()
        {
            string template = "{1} -i \"{0}\" -f image2 -vframes 1 \"{2}\"";
            //{0} is input video
            //{1} is -ss TIME or blank (inaccurate but fast because it's before -i)
            //{2} is output image

            string input = _owner.textBoxIn.Text;
            if (string.IsNullOrWhiteSpace(input))
            {
                _message = "No input file!";
                return null;
            }
            if (!File.Exists(input))
            {
                _message = "Input file doesn't exist";
                return null;
            }

            var time = MainForm.ParseTime(_owner.boxCropFrom.Text);
            _message = string.Format("Previewing video at {0}", TimeSpan.FromSeconds(time));
            //We can actually allow invalid times here: we just use the preview from the very start of the video (0.0)

            return string.Format(template, input, "-ss " + time, _previewFile);
        }

        private void pictureBoxVideo_MouseDown(object sender, MouseEventArgs e)
        {
            //This checks the distance from the rectangle corner point to the mouse, and then selects the one with the smallest distance
            //That one will be dragged along with the mouse

            var closest = GetClosestPointDistance(new Point(e.X, e.Y));

            if (closest.Value < MaxDistance * MaxDistance) //Comparing squared distance
            {
                _heldCorner = closest.Key;
                _held = true;

            }
            else if (_insideRectangle) //Or, if there's no closest dot and the mouse is inside the cropping rectangle, drag the entire rectangle
            {
                _mouseOffset = new Point((int)(_rectangle.X * pictureBoxVideo.Width - e.X), (int)(_rectangle.Y * pictureBoxVideo.Height - e.Y));
                _heldCorner  = Corner.None;
                _held = true;
            }


            pictureBoxVideo.Invalidate();
        }

        private KeyValuePair<Corner, float> GetClosestPointDistance(Point e)
        {
            var distances = new Dictionary<Corner, float>();
            distances[Corner.TopLeft] = (float)(Math.Pow(e.X - _rectangle.Left * pictureBoxVideo.Width, 2) + Math.Pow(e.Y - _rectangle.Top * pictureBoxVideo.Height, 2));
            distances[Corner.TopRight] = (float)(Math.Pow(e.X - _rectangle.Right * pictureBoxVideo.Width, 2) + Math.Pow(e.Y - _rectangle.Top * pictureBoxVideo.Height, 2));
            distances[Corner.BottomLeft] = (float)(Math.Pow(e.X - _rectangle.Left * pictureBoxVideo.Width, 2) + Math.Pow(e.Y - _rectangle.Bottom * pictureBoxVideo.Height, 2));
            distances[Corner.BottomRight] = (float)(Math.Pow(e.X - _rectangle.Right * pictureBoxVideo.Width, 2) + Math.Pow(e.Y - _rectangle.Bottom * pictureBoxVideo.Height, 2));

            return distances.OrderBy(a => a.Value).First();

        }

        private void pictureBoxVideo_MouseUp(object sender, MouseEventArgs e)
        {
            _held = false;
            _heldCorner = Corner.None;
            pictureBoxVideo.Invalidate();
        }

        private void pictureBoxVideo_MouseMove(object sender, MouseEventArgs e)
        {
            _mousePos = new Point(e.X, e.Y);
            _insideRectangle = _rectangle.Contains(e.X / (float)pictureBoxVideo.Width, e.Y / (float)pictureBoxVideo.Height);

            if (_held)
            {
                //Can't use Inflate or Offset here, Inflate changes size from the center and Offset doesn't change size at all!
                //Anyway here we change the size of the rectangle if the mouse is actually held down

                //Clamp mouse pos to picture box, that way you shouldn't be able to move the cropping rectangle out of bounds
                Point min = /*pictureBoxVideo.PointToScreen*/(new Point(0, 0));
                Point max = /*pictureBoxVideo.PointToScreen*/(new Point(pictureBoxVideo.Size));
                float clampedMouseX = Math.Max(min.X, Math.Min(max.X, e.X));
                float clampedMouseY = Math.Max(min.Y, Math.Min(max.Y, e.Y));

                float newWidth = 0;
                float newHeight = 0;
                switch (_heldCorner)
                {
                    case Corner.TopLeft:
                        newWidth = _rectangle.Width - (clampedMouseX / (float)pictureBoxVideo.Width - _rectangle.X);
                        newHeight = _rectangle.Height - (clampedMouseY / (float)pictureBoxVideo.Height - _rectangle.Y);
                        _rectangle.X = clampedMouseX / (float)pictureBoxVideo.Width;
                        _rectangle.Y = clampedMouseY / (float)pictureBoxVideo.Height;
                        break;

                    case Corner.TopRight:
                        newWidth = _rectangle.Width + (clampedMouseX / (float)pictureBoxVideo.Width - _rectangle.Right);
                        newHeight = _rectangle.Height - (clampedMouseY / (float)pictureBoxVideo.Height - _rectangle.Y);
                        _rectangle.Y = clampedMouseY / (float)pictureBoxVideo.Height;
                        break;

                    case Corner.BottomLeft:
                        newWidth = _rectangle.Width - (clampedMouseX / (float)pictureBoxVideo.Width - _rectangle.X);
                        newHeight = _rectangle.Height + (clampedMouseY / (float)pictureBoxVideo.Height - _rectangle.Bottom);
                        _rectangle.X = clampedMouseX / (float)pictureBoxVideo.Width;
                        break;

                    case Corner.BottomRight:
                        newWidth = _rectangle.Width + (clampedMouseX / (float)pictureBoxVideo.Width - _rectangle.Right);
                        newHeight = _rectangle.Height + (clampedMouseY / (float)pictureBoxVideo.Height - _rectangle.Bottom);
                        break;

                    case Corner.None: //Drag entire rectangle
                        //This is a special case, because the mouse needs to be clamped according to rectangle size too!
                        float actualRectW = _rectangle.Width * pictureBoxVideo.Width;
                        float actualRectH = _rectangle.Height * pictureBoxVideo.Height;
                        clampedMouseX = Math.Max(min.X - _mouseOffset.X, Math.Min(max.X - _mouseOffset.X - actualRectW, e.X));
                        clampedMouseY = Math.Max(min.Y - _mouseOffset.Y, Math.Min(max.Y - _mouseOffset.Y - actualRectH, e.Y));
                        _rectangle.X = (clampedMouseX + _mouseOffset.X) / (float)pictureBoxVideo.Width;
                        _rectangle.Y = (clampedMouseY + _mouseOffset.Y) / (float)pictureBoxVideo.Height;
                        break;
                }

                if (newWidth != 0)
                    _rectangle.Width = newWidth;
                if (newHeight != 0)
                    _rectangle.Height = newHeight;

                //Do a out of bounds check
                //This doesn't work, I have a great idea though: limit the mouse cursor position to the picturebox!
                /*
                _rectangle.X = Math.Max(0, _rectangle.X);
                _rectangle.Y = Math.Max(0, _rectangle.Y);
                if (_rectangle.Right > 1)
                    _rectangle.Width = 1 - _rectangle.X;
                if (_rectangle.Bottom > 1)
                    _rectangle.Height = 1 - _rectangle.Y;*/
            }

            pictureBoxVideo.Invalidate();
        }

        private void pictureBoxVideo_Paint(object sender, PaintEventArgs e)
        {
            var g = e.Graphics;

            //g.SmoothingMode = SmoothingMode.HighQuality;
            //TODO: this is really slow for some reason. Investigate using profiling or something.

            var edgePen = new Pen(Color.White, 1f);
            var dotBrush = new SolidBrush(Color.White);
            var outsideBrush = new HatchBrush(HatchStyle.Percent50, Color.Transparent);
            //var outsideBrush = new SolidBrush(Color.FromArgb(100, 0, 0, 0));

            var maxW = pictureBoxVideo.Width;
            var maxH = pictureBoxVideo.Height;
            var x = _rectangle.X * pictureBoxVideo.Width;
            var y = _rectangle.Y * pictureBoxVideo.Height;
            var w = _rectangle.Width * maxW;
            var h = _rectangle.Height * maxH;

            //Darken background
            g.FillRectangle(outsideBrush, 0, 0, maxW, y);
            g.FillRectangle(outsideBrush, 0, y, x, h);
            g.FillRectangle(outsideBrush, x + w, y, maxW - (x + w), h);
            g.FillRectangle(outsideBrush, 0, y + h, maxW, maxH);

            //Edge
            g.DrawRectangle(edgePen, x, y, w, h);

            if (_insideForm) //Draw corner dots if mouse is inside the picture box
            {
                float diameter = 6;
                float diameterEdge = diameter * 2;

                g.FillEllipse(dotBrush, x - diameter / 2, y - diameter / 2, diameter, diameter);
                g.FillEllipse(dotBrush, x + w - diameter / 2, y - diameter / 2, diameter, diameter);
                g.FillEllipse(dotBrush, x - diameter / 2, y + h - diameter / 2, diameter, diameter);
                g.FillEllipse(dotBrush, x + w - diameter / 2, y + h - diameter / 2, diameter, diameter);

                var closest = GetClosestPointDistance(_mousePos);
                if (closest.Value < MaxDistance * MaxDistance)  //Comparing squared distance to avoid worthless square roots
                {
                    Cursor = Cursors.Hand;
                    //Draw outlines on the dots to indicate they can be selected and moved
                    if (closest.Key == Corner.TopLeft) g.DrawEllipse(edgePen, x - diameterEdge / 2, y - diameterEdge / 2, diameterEdge, diameterEdge);
                    if (closest.Key == Corner.TopRight) g.DrawEllipse(edgePen, x + w - diameterEdge / 2, y - diameterEdge / 2, diameterEdge, diameterEdge);
                    if (closest.Key == Corner.BottomLeft) g.DrawEllipse(edgePen, x - diameterEdge / 2, y + h - diameterEdge / 2, diameterEdge, diameterEdge);
                    if (closest.Key == Corner.BottomRight) g.DrawEllipse(edgePen, x + w - diameterEdge / 2, y + h - diameterEdge / 2, diameterEdge, diameterEdge);
                }
                else if (_insideRectangle)
                    Cursor = Cursors.SizeAll;
                else if (Cursor != Cursors.Default) //Reduntant???
                    Cursor = Cursors.Default;
            }

            //Draw a shadow below the text so it's still readable on a white/black background

            if (_generating)
            {
                for (int i = 0; i < 2; i++)
                    g.DrawString("Generating preview...", _font, new SolidBrush(Color.FromArgb(i * 255, i * 255, i * 255)), 5, 5 - i);
            }
            else if (!string.IsNullOrWhiteSpace(_message))
            {
                for (int i = 0; i < 2; i++)
                    g.DrawString(_message, _font, new SolidBrush(Color.FromArgb(i * 255, i * 255, i * 255)), 5, 5 - i);
            }
        }

        private void pictureBoxVideo_MouseEnter(object sender, EventArgs e)
        {
            _insideForm = true;
            pictureBoxVideo.Invalidate();
        }

        private void pictureBoxVideo_MouseLeave(object sender, EventArgs e)
        {
            _insideForm = false;
            pictureBoxVideo.Invalidate();
        }

        private void pictureBoxVideo_Resize(object sender, EventArgs e)
        {
            pictureBoxVideo.Invalidate();
        }

        private void buttonReset_Click(object sender, EventArgs e)
        {
            _rectangle = FullCrop;
            pictureBoxVideo.Invalidate();
        }

        private void buttonConfirm_Click(object sender, EventArgs e)
        {
            if (_rectangle.Left >= _rectangle.Right || _rectangle.Top >= _rectangle.Bottom)
            {
                MessageBox.Show("You messed up your crop! Press the reset button and try again.", "ERROR", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            float tolerance = 0.1f; //Account for float inprecision

            if (_rectangle.Left < 0 - tolerance || _rectangle.Top < 0 - tolerance || _rectangle.Right > 1 + tolerance || _rectangle.Bottom > 1 + tolerance)
            {
                MessageBox.Show("Your crop is outside the valid range! Press the reset button and try again.", "ERROR", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            _rectangle.X = Math.Max(0, _rectangle.X);
            _rectangle.Y = Math.Max(0, _rectangle.Y);
            if (_rectangle.Right > 1)
                _rectangle.Width = 1 - _rectangle.X;
            if (_rectangle.Bottom > 1)
                _rectangle.Height = 1 - _rectangle.Y;

            DialogResult = DialogResult.OK;
            _owner.CroppingRectangle = _rectangle;

            Close();
        }
    }
}
