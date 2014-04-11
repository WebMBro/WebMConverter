using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace WebMConverter
{
    public partial class CropForm : Form
    {

        private Corner _heldCorner = Corner.None;
        private bool _held;

        private RectangleF _rectangle; //Rectangle is in [0-1, 0-1] format, this should make scaling easier
        private bool _insideForm; //Is the cursor inside the picturebox?
        private bool _insideRectangle; //Is the cursor inside the cropping rectangle?
        private Point _mousePos;
        private Point _mouseOffset; //This is in pixels, not a float

        private const int MaxDistance = 6;

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
            InitializeComponent();

            _rectangle = new RectangleF(0.25f, 0.25f, 0.5f, 0.5f);

            //Stub for video crop form
            //Template:
                //-filter:v "crop=out_w:out_h:x:y"

            //TODO: use this form to determine a cropping area for the video
            //TODO: run ffmpeg here to get a picture of the first frame of the video, this way you can indirectly determine the size
                //Don't freeze up the interface while waiting for the preview, okay?
                //Also, important, scale the preview to the size options the user entered in width x height. Take note of the -1 thing too!
                //I think ffmpeg first scales it down, then crops it.
            //TODO: when pressing Confirm, put the values in a new text box that'll be added to the video options
            //TODO: maybe convert the float values from the rect to out_w/out_h/x/y yourself
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

                float newWidth = 0;
                float newHeight = 0;
                switch (_heldCorner)
                {
                    case Corner.TopLeft:
                        newWidth = _rectangle.Width - (e.X / (float)pictureBoxVideo.Width - _rectangle.X);
                        newHeight = _rectangle.Height - (e.Y / (float)pictureBoxVideo.Height - _rectangle.Y);
                        _rectangle.X = e.X / (float)pictureBoxVideo.Width;
                        _rectangle.Y = e.Y / (float)pictureBoxVideo.Height;
                        break;

                    case Corner.TopRight:
                        newWidth = _rectangle.Width + (e.X / (float)pictureBoxVideo.Width - _rectangle.Right);
                        newHeight = _rectangle.Height - (e.Y / (float)pictureBoxVideo.Height - _rectangle.Y);
                        _rectangle.Y = e.Y / (float)pictureBoxVideo.Height;
                        break;

                    case Corner.BottomLeft:
                        newWidth = _rectangle.Width - (e.X / (float)pictureBoxVideo.Width - _rectangle.X);
                        newHeight = _rectangle.Height + (e.Y / (float)pictureBoxVideo.Height - _rectangle.Bottom);
                        _rectangle.X = e.X / (float)pictureBoxVideo.Width;
                        break;

                    case Corner.BottomRight:
                        newWidth = _rectangle.Width + (e.X / (float)pictureBoxVideo.Width - _rectangle.Right);
                        newHeight = _rectangle.Height + (e.Y / (float)pictureBoxVideo.Height - _rectangle.Bottom);
                        break;

                    case Corner.None: //Drag entire rectangle
                        _rectangle.X = (e.X + _mouseOffset.X) / (float)pictureBoxVideo.Width;
                        _rectangle.Y = (e.Y + _mouseOffset.Y) / (float)pictureBoxVideo.Height;
                        break;
                }

                if (newWidth != 0)
                    _rectangle.Width = newWidth;
                if (newHeight != 0)
                    _rectangle.Height = newHeight;
            }

            pictureBoxVideo.Invalidate();
        }

        Font f = new Font(FontFamily.GenericSansSerif, 11f);

        private void pictureBoxVideo_Paint(object sender, PaintEventArgs e)
        {
            var g = e.Graphics;

            g.SmoothingMode = SmoothingMode.HighQuality;

            for (int i = 0; i < 2; i++)
            {
                g.DrawString("Generating preview...", f, new SolidBrush(Color.FromArgb(i * 255, i * 255, i * 255)), 5, 5 - i);
            }

            return; //Todo: actually do something here

            var edgePen = new Pen(Color.White, 1f);
            var dotBrush = new SolidBrush(Color.White);
            var outsideBrush = new SolidBrush(Color.FromArgb(100, 0, 0, 0));

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
                    if (closest.Key == Corner.TopLeft) g.DrawEllipse(edgePen, x - diameterEdge / 2, y - diameterEdge / 2, diameterEdge, diameterEdge);
                    if (closest.Key == Corner.TopRight) g.DrawEllipse(edgePen, x + w - diameterEdge / 2, y - diameterEdge / 2, diameterEdge, diameterEdge);
                    if (closest.Key == Corner.BottomLeft) g.DrawEllipse(edgePen, x - diameterEdge / 2, y + h - diameterEdge / 2, diameterEdge, diameterEdge);
                    if (closest.Key == Corner.BottomRight) g.DrawEllipse(edgePen, x + w - diameterEdge / 2, y + h - diameterEdge / 2, diameterEdge, diameterEdge);
                }
                else if (_insideRectangle)
                    Cursor = Cursors.SizeAll;
                else if (Cursor != Cursors.Default)
                    Cursor = Cursors.Default;
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
    }
}
