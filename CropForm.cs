using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace WebMConverter
{
    public partial class CropForm : Form
    {
        public CropForm()
        {
            InitializeComponent();

            //Stub for video crop form
            //Template:
                //-filter:v "crop=out_w:out_h:x:y"

            //TODO: use this form to determine a cropping area for the video
            //TODO: run ffmpeg here to get a picture of the first frame of the video, this way you can indirectly determine the size
            //TODO: use the click/drag events to draw a rectangle that will be used for the cropping
                //OR: have 4 lines that can be slided using the mouse? Probably bad idea, only allows symmetrical cropping.
                //OR: draw a rectangle with 4 white corners, then the user can drag the corners to change the rectangle
            //TODO: when pressing Confirm, put the values in a new text box that'll be added to the video options
        }
    }
}
