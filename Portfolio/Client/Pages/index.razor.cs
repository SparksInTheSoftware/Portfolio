using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Drawing;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using System.Xml.Schema;
using Blazor.Extensions;
using Blazor.Extensions.Canvas.Canvas2D;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using Portfolio.Client.Shared;

namespace Portfolio.Client.Pages
    {
    public partial class Index : ComponentBase
        {
        [Inject] IJSRuntime JSRuntime { get; set; }

        private ElementReference containerDiv;
        private ElementReference imageRef;
        private Canvas canvas;
        private bool fullScreen = false;
        private string fullScreenClass { get; set; } = "oi oi-fullscreen-enter";

        private string CurFileName
            {
            get { return "http://0x151.com/Portfolio/" + this.fileNames[this.iCurFile]; }
            }
        private int iCurFile = 0;
        private string[] fileNames = {
            "DSC02662.jpg",
            "DSC02663.jpg",
            "DSC02664.jpg",
            "DSC02673.jpg",
            "DSC02674.jpg",
            "DSC02679.jpg",
            "DSC02686.jpg",
            "DSC02687.jpg",
        };

        private void DoFullScreen()
            {
            string func = this.fullScreen ? "exitFullScreen" : "enterFullScreen";

            this.fullScreen = !this.fullScreen;
            fullScreenClass = this.fullScreen ? "oi oi-fullscreen-exit" : "oi oi-fullscreen-enter";

            IJSInProcessRuntime js = JSRuntime as IJSInProcessRuntime;
            string outValuesBase64 = js.Invoke<string>(func);
            }

        private Canvas2DContext _context;

        protected override async Task OnAfterRenderAsync(bool firstRender)
            {
            if (firstRender)
                {
                await this.containerDiv.FocusAsync();
                CenterImage();
                await DrawImage();
                }
            }

        private static Point origin = new (0, 0);
        private Size canvasSize = new() { Width = 0, Height = 0 };

        // canvasAnchor is the point on the canvas that corresponds to imageAnchor.
        // It starts out in the center of the canvas, but is set to the location of the mouse
        // when the image is moved/zoomed.
        private Point canvasAnchor;

        private Size imageNativeSize = new() { Width = 0, Height = 0 };

        // imageDisplayRect is the rectangle in canvas co-ordinates where the image is displayed.
        // This reflects any zooming or moving of the image.
        private Rectangle imageDisplayRect;

        // imageSize is in canvas co-ordinate space
        private Size imageSize = new() { Width = 0, Height = 0 };
        private Size imageSizeZoomed = new() { Width = 0, Height = 0 };

        // imageAnchor is the point in image rect that corresponds to canvasAnchor
        private Point imageAnchor;
        private Point imageAnchorZoomed;

        private Fraction imageToCanvas;

        private Fraction zoom = new(100, 100);
        private bool displayInfo = false;
        private bool displayAnchor = false;

        private Point ImageToCanvas(Point imagePoint)
            {
            return new Point()
                {
                X = imagePoint.X * this.imageToCanvas,
                Y = imagePoint.Y * this.imageToCanvas
                };
            }

        private Size ImageToCanvas(Size imageSize)
            {
            return new Size()
                {
                Width = imageSize.Width * this.imageToCanvas,
                Height = imageSize.Height * this.imageToCanvas
                };
            }

        private Rectangle ImageToCanvas(Rectangle imageRect)
            {
            return new Rectangle()
                {
                X = imageRect.X * this.imageToCanvas,
                Y = imageRect.Y * this.imageToCanvas,
                Width = imageRect.Width * this.imageToCanvas,
                Height = imageRect.Height * this.imageToCanvas
                };
            }

        private Point CanvasToImage(Point canvasPoint)
            {
            return new Point()
                {
                X = canvasPoint.X / this.imageToCanvas,
                Y = canvasPoint.Y / this.imageToCanvas
                };
            }

        private Rectangle CanvasToImage(Rectangle canvasRect)
            {
            return new Rectangle()
                {
                X = canvasRect.X / this.imageToCanvas,
                Y = canvasRect.Y / this.imageToCanvas,
                Width = canvasRect.Width / this.imageToCanvas,
                Height = canvasRect.Height / this.imageToCanvas
                };
            }

        private void ZoomPlus(int delta)
            {
            ZoomTo(this.zoom.numerator + delta);
            }
        private void ZoomTo(int value)
            {
            Fraction prevZoom = this.zoom;
            if (value < 100)
                value = 100;
            else if (value > 3200)
                value = 3200;

            this.zoom.numerator = value;

            // this.zoom.denominator and prevZoom.denominator are the same,
            // so the unzoom the zoom operation can be performed with just the numerators.
            // This prevents rounding to nearest 100 caused by first dividing by 100 and then multiplying by it.
            this.imageAnchorZoomed.X = (this.imageAnchorZoomed.X * this.zoom.numerator) / prevZoom.numerator;
            this.imageAnchorZoomed.Y = (this.imageAnchorZoomed.Y * this.zoom.numerator) / prevZoom.numerator;
            ComputeImageDisplayRect();
            }

        private void ComputeImageDisplayRect()
            {
            // Figure out where the image should be in canvas co-ordinates
            //     1. Scale the image rect
            //     2. Line up the anchor points
            this.imageSizeZoomed = this.imageSize * this.zoom;
            this.imageDisplayRect = new(origin, this.imageSizeZoomed);

            Console.WriteLine($"imageDisplayRect (after zoom) = {this.imageDisplayRect.ToString()}");

            // Shift the image so that its anchor point lines up
            // with the anchor point on the canvas.
            // The anchor points are initially the centers of the image and the canvas,
            // but the anchor point changes to the mouse position when dragging and zooming.
            Point offset = new()
                {
                X = this.canvasAnchor.X - this.imageAnchorZoomed.X,
                Y = this.canvasAnchor.Y - this.imageAnchorZoomed.Y
                };
            this.imageDisplayRect.Offset(offset);
            Console.WriteLine($"imageDisplayRect (after offset) = {this.imageDisplayRect.ToString()}");
            }

        private async Task DrawImage()
            {
            // Make sure the canvas is the same size as its conainer
            Size newCanvasSize = await JSRuntime.InvokeAsync<Size>("CanvasResize", this.containerDiv, this.canvas.GetCanvasRef());
            if ((newCanvasSize.Width != this.canvasSize.Width) || (newCanvasSize.Width != this.canvasSize.Width))
                {
                this.canvasSize = new (newCanvasSize.Width, newCanvasSize.Height);
                RecomputeImageScale();
                ComputeImageDisplayRect();
                }

            if ((this.imageNativeSize.Width > 0) && (this.imageNativeSize.Height > 0))
                {

                this._context = await this.canvas.CreateCanvas2DAsync();

                await this._context.BeginBatchAsync();
                await this._context.ClearRectAsync(0, 0, this.canvasSize.Width, this.canvasSize.Height);

                await this._context.DrawImageAsync(this.imageRef,
                    0, 0, this.imageNativeSize.Width, this.imageNativeSize.Height,
                    this.imageDisplayRect.X, this.imageDisplayRect.Y, this.imageDisplayRect.Width, this.imageDisplayRect.Height);

                Console.WriteLine($"zoom = {this.zoom.ToString()}");
                await this._context.BeginPathAsync();
                await this._context.SetStrokeStyleAsync("gray");
                await this._context.SetLineWidthAsync(3);
                await this._context.RectAsync(this.imageDisplayRect.X, this.imageDisplayRect.Y, this.imageDisplayRect.Width, this.imageDisplayRect.Height);
                await this._context.StrokeAsync();
                await this._context.EndBatchAsync();

                if (this.displayAnchor)
                    {
                    await this._context.BeginBatchAsync();
                    await this._context.BeginPathAsync();
                    await this._context.SetStrokeStyleAsync("red");
                    await this._context.MoveToAsync(this.canvasAnchor.X, this.canvasAnchor.Y - 10);
                    await this._context.LineToAsync(this.canvasAnchor.X, this.canvasAnchor.Y + 10);
                    await this._context.MoveToAsync(this.canvasAnchor.X - 10, this.canvasAnchor.Y);
                    await this._context.LineToAsync(this.canvasAnchor.X + 10, this.canvasAnchor.Y);
                    await this._context.StrokeAsync();
                    await this._context.EndBatchAsync();
                    }

                if (this.displayInfo)
                    {
                    await this._context.BeginBatchAsync();
                    await this._context.SetStrokeStyleAsync("white");
                    await this._context.SetLineWidthAsync(1);
                    await this._context.SetFontAsync("lighter 16px menu");
                    await this._context.StrokeTextAsync($"Image Anchor : {this.imageAnchorZoomed.ToString()}", 10, 25);
                    await this._context.StrokeTextAsync($"Canvas Anchor: {this.canvasAnchor.ToString()}", 10, 50);
                    await this._context.StrokeTextAsync($"Display Rect : {this.imageDisplayRect.ToString()}", 10, 75);
                    await this._context.StrokeTextAsync($"Zoom         : {this.zoom.ToString()}", 10, 100);

                    await this._context.EndBatchAsync();
                    }
                }
            }

        private async Task Next()
            {
            this.iCurFile++;
            if (this.iCurFile >= this.fileNames.Length)
                this.iCurFile = 0;
            StateHasChanged();
            }
        private async Task Previous()
            {
            this.iCurFile--;
            if (this.iCurFile < 0)
                this.iCurFile = this.fileNames.Length - 1;
            StateHasChanged();
            }

        private async Task OnKeyDown(KeyboardEventArgs args)
            {
            if (args.CtrlKey)
                {
                switch (args.Key)
                    {
                    default:
                        return;

                    case "1":
                        ZoomTo(100 / this.imageToCanvas);
                        RecomputeImageScale();
                        ComputeImageDisplayRect();
                        break;

                    case "0":
                        ZoomTo(100);
                        RecomputeImageScale();
                        CenterImage();
                        ComputeImageDisplayRect();
                        break;

                    case "+":
                    case "=":
                        ZoomPlus(25);
                        break;

                    case "-":
                        ZoomPlus(-25);
                        break;
                    }
                await DrawImage();
                }
            else if (args.ShiftKey)
                {
                }
            else if (args.AltKey)
                {
                }
            else
                {
                switch (args.Key)
                    {
                    default:
                        return;

                    case "d":
                        this.displayInfo = !this.displayInfo;
                        await DrawImage();
                        break;

                    case "a":
                        this.displayAnchor = !this.displayAnchor;
                        await DrawImage();
                        break;

                    case "f":
                        break;

                    case "ArrowLeft":
                        await Previous();
                        break;

                    case "ArrowRight":
                        await Next();
                        break;
                    }
                }
            }

        private void SetAnchor(int x, int y, bool setImageAnchor)
            {
            Point newAnchor = new Point(x, y);
            if (setImageAnchor)
                {
                // imageAnchor is relative to the zoomed location in canvas co-ordinates
                // newAnchor is relative to (0,0) in canvas co-ordinates.
                // So translate newAnchor to offset in image.
                this.imageAnchorZoomed = newAnchor - (Size) this.imageDisplayRect.Location;
                }

            this.canvasAnchor = newAnchor;
            ComputeImageDisplayRect();
            }

        private async Task OnMouseDown(MouseEventArgs args)
            {
            await this.containerDiv.FocusAsync();
            SetAnchor((int)args.OffsetX, (int)args.OffsetY, true);
            }

        private async Task OnMouseUp(MouseEventArgs args)
            {
            await DrawImage();
            }

        private async Task OnMouseMove(MouseEventArgs args)
            {
            if (args.Buttons != 0)
                {
                SetAnchor((int) args.OffsetX, (int) args.OffsetY, false);

                //TODO: keep image on canvas
                await DrawImage();
                }
            }

        private async Task OnMouseWheel(WheelEventArgs args)
            {
            ZoomPlus((int) -args.DeltaY);
            SetAnchor((int)args.OffsetX, (int)args.OffsetY, true);

            await DrawImage();
            }

        private void RecomputeImageScale()
            {
            if ((this.imageNativeSize.Width == 0) || (this.canvasSize.Width == 0))
                return;

            double imageRatio = this.imageNativeSize.Height / this.imageNativeSize.Width;
            double canvasRatio = this.canvasSize.Height / this.canvasSize.Width;

            if (imageRatio < canvasRatio)
                {
                // The whole image will fit on the canvas with the width filled
                this.imageToCanvas = new Fraction(this.canvasSize.Width, this.imageNativeSize.Width);
                }
            else
                {
                // The whole image will fit on the canvas with the height filled
                this.imageToCanvas = new Fraction(this.canvasSize.Height, this.imageNativeSize.Height);
                }

            this.imageSize = ImageToCanvas(this.imageNativeSize);
            this.imageSizeZoomed = this.imageSize * this.zoom;
            }
        private void CenterImage()
            {
            this.imageAnchorZoomed.X = this.imageSizeZoomed.Width / 2;
            this.imageAnchorZoomed.Y = this.imageSizeZoomed.Height / 2;
            this.canvasAnchor.X = this.canvasSize.Width / 2;
            this.canvasAnchor.Y = this.canvasSize.Height / 2;
            }

        private async Task OnImageLoaded()
            {
            this.imageNativeSize = await JSRuntime.InvokeAsync<Size>("GetNaturalSize", this.imageRef); 
            RecomputeImageScale();
            ZoomTo(100);
            CenterImage();
            ComputeImageDisplayRect();
            await DrawImage();
            }
        }
    public class BoundingClientRect
        {
        public double X { get; set; }
        public double Y { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }
        public double Top { get; set; }
        public double Right { get; set; }
        public double Bottom { get; set; }
        public double Left { get; set; }
        }

    public struct Fraction
        {
        public int numerator;
        public int denominator;

        public Fraction(int numerator, int denominator)
            {
            this.numerator = numerator;
            this.denominator = denominator;
            }

        public override string ToString()
            {
            return $"{this.numerator}/{this.denominator}";
            }

        public static Point operator * (Point left, Fraction right)
            {
            return new(left.X * right, left.Y * right);
            }

        public static Size operator * (Size left, Fraction right)
            {
            return new(left.Width * right, left.Height * right);
            }

        public static int operator * (Fraction left, int right)
            {
            return left.numerator * right / left.denominator;
            }

        public static int operator * (int right, Fraction left)
            {
            return left * right;
            }

        public static int operator / (int right, Fraction left)
            {
            return left.denominator * right / left.numerator;
            }

        public static explicit operator double (Fraction f)
            {
            return ((double)f.numerator) / ((double)f.denominator);
            }
        }
    
    
    }
