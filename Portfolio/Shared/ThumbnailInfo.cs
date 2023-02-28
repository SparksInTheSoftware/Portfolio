using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;

namespace Portfolio.Shared
    {
    public class ThumbnailInfo
        {
        public int ImageHeight { get; set; }
        public Rectangle CropSquareRect { get; set; }
        public Rectangle CropSquareRectScaledToHeight(int height)
            {
            return ScaleToHeight(CropSquareRect, height);
            }

        public Rectangle Crop3x2Rect { get; set; }
        public Rectangle Crop3x2RectScaledToHeight(int height)
            {
            return ScaleToHeight(Crop3x2Rect, height);
            }

        private Rectangle ScaleToHeight(Rectangle rect, int height)
            {
            if (ImageHeight == height)
                {
                return rect;
                }

            double scale = (double)height/(double)ImageHeight;

            return new()
                {
                X = (int)(rect.X*scale),
                Y = (int)(rect.Y*scale),
                Width = (int)(rect.Width*scale),
                Height = (int)(rect.Height*scale)
                };
            }
        }
    }
