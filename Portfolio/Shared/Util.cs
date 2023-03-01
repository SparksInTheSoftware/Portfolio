using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;

namespace Portfolio.Shared
    {
    public class Util
        {
        public static Rectangle BiggestCenteredSquare(int width, int height)
            {
            int x = 0;
            int y = 0;

            int delta = width - height;
            if (delta < 0)
                {
                y += -delta/2;
                height = width;
                }
            else
                {
                x += delta/2;
                width = height;
                }

            return new Rectangle(x, y, width, height);
            }
        public static Rectangle BiggestCenteredRect3x2(int width, int height)
            {
            Rectangle rect = new Rectangle(0, 0, width, height);

            if (2*width < height*3)
                {
                // Width is max
                rect.Height = 2*width/3;
                rect.Y = (height - rect.Height)/2;
                }
            else
                {
                // Height is max
                rect.Width = 3*height/2;
                rect.X = (width - rect.Width)/2;
                }


            return rect;
            }
        }
    }
