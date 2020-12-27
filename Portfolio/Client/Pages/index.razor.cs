using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Drawing;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
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
        [Inject] HttpClient HttpClient { get; set; }

        private ElementReference containerDiv;
        private ElementReference imageRef;
        private Canvas canvas;
        private bool fullScreen = false;
        private string fullScreenClass { get; set; } = "oi oi-fullscreen-enter";

        private string CurFileName
            {
            get
                {
                if ((this.fileNames != null) && (this.iCurFile < this.fileNames.Length))
                    {
                    return this.fileNames[this.iCurFile];
                    }
                return "";
                }
            }

        private int iCurFile = 0;
        private string[] fileNames;
        private void DoFullScreen()
            {
            string func = this.fullScreen ? "exitFullScreen" : "enterFullScreen";

            this.fullScreen = !this.fullScreen;
            fullScreenClass = this.fullScreen ? "oi oi-fullscreen-exit" : "oi oi-fullscreen-enter";

            IJSInProcessRuntime js = JSRuntime as IJSInProcessRuntime;
            string outValuesBase64 = js.Invoke<string>(func);
            CenterImage();
            }

        private Canvas2DContext _context;

        protected override async Task OnAfterRenderAsync(bool firstRender)
            {
            if (firstRender)
                {
                await JSRuntime.InvokeVoidAsync("RegisterWindowHandler", DotNetObjectReference.Create<Index>(this));
                this.fileNames = await HttpClient.GetFromJsonAsync<string[]>("https://www.sparksinthesoftware.com/portfolio/portfolio.json");
                await this.containerDiv.FocusAsync();
                await OnResize();
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

        // imageAnchorZoomed is the point in image rect that corresponds to canvasAnchor
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

            // if zoomed image is bigger than canvas, don't keep the image on the canvas
            if (this.imageSizeZoomed.Width >= this.canvasSize.Width)
                {
                if (this.imageDisplayRect.X > 0)
                    {
                    this.imageDisplayRect.X = 0;
                    }
                else if (this.imageDisplayRect.Right <= this.canvasSize.Width)
                    {
                    this.imageDisplayRect.X = -(this.imageDisplayRect.Width - this.canvasSize.Width);
                    }
                }
            if (this.imageSizeZoomed.Height >= this.canvasSize.Height)
                {
                if (this.imageDisplayRect.Y > 0)
                    {
                    this.imageDisplayRect.Y = 0;
                    }
                else if (this.imageDisplayRect.Bottom <= this.canvasSize.Height)
                    {
                    this.imageDisplayRect.Y = -(this.imageDisplayRect.Height - this.canvasSize.Height);
                    }
                }
            }

        private async Task DrawCanvas()
            {

            if ((this.imageNativeSize.Width > 0) && (this.imageNativeSize.Height > 0))
                {

                this._context = await this.canvas.CreateCanvas2DAsync();

                await this._context.BeginBatchAsync();
                await this._context.ClearRectAsync(0, 0, this.canvasSize.Width, this.canvasSize.Height);

                await this._context.DrawImageAsync(this.imageRef,
                    0, 0, this.imageNativeSize.Width, this.imageNativeSize.Height,
                    this.imageDisplayRect.X, this.imageDisplayRect.Y, this.imageDisplayRect.Width, this.imageDisplayRect.Height);

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
                    await this._context.StrokeTextAsync($"Image Load   : {this.imageLoadTime.ToString()}", 10, 125);

                    await this._context.EndBatchAsync();
                    }
                }
            }

        DateTime startImageFetch;
        TimeSpan imageLoadTime;
        private async Task Next()
            {
            if (this.fileNames?.Length > 0)
                {
                this.iCurFile++;
                if (this.iCurFile >= this.fileNames.Length)
                    this.iCurFile = 0;
                StateHasChanged();
                this.startImageFetch = DateTime.Now;

                // Prefetch the next image while the current one is being viewed.
                if (this.iCurFile + 1 < this.fileNames.Length)
                    await JSRuntime.InvokeVoidAsync("PrefetchImage", this.fileNames[this.iCurFile + 1]);
                }
            }
        private async Task Previous()
            {
            if (this.fileNames?.Length > 0)
                {
                this.iCurFile--;
                if (this.iCurFile < 0)
                    this.iCurFile = this.fileNames.Length - 1;
                StateHasChanged();
                this.startImageFetch = DateTime.Now;
                }
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
                await DrawCanvas();
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
                        await DrawCanvas();
                        break;

                    case "a":
                        this.displayAnchor = !this.displayAnchor;
                        await DrawCanvas();
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
            await DrawCanvas();
            }

        private async Task OnMouseMove(MouseEventArgs args)
            {
            if (args.Buttons != 0)
                {
                SetAnchor((int) args.OffsetX, (int) args.OffsetY, false);

                //TODO: keep image on canvas
                await DrawCanvas();
                }
            }

        private async Task OnMouseWheel(WheelEventArgs args)
            {
            ZoomPlus((int) -args.DeltaY);
            SetAnchor((int)args.OffsetX, (int)args.OffsetY, true);

            await DrawCanvas();
            }

        private void RecomputeImageScale()
            {
            if ((this.imageNativeSize.Width == 0) || (this.canvasSize.Width == 0))
                return;

            double imageRatio = ((float) this.imageNativeSize.Height) / ((float) this.imageNativeSize.Width);
            double canvasRatio = ((float) this.canvasSize.Height) / ((float) this.canvasSize.Width);

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
            DateTime finish = DateTime.Now;
            this.imageLoadTime = finish - this.startImageFetch;

            this.imageNativeSize = await JSRuntime.InvokeAsync<Size>("GetNaturalSize", this.imageRef); 
            RecomputeImageScale();
            ZoomTo(100);
            CenterImage();
            ComputeImageDisplayRect();
            await DrawCanvas();
            }

        [JSInvokable]
        public async Task OnResize()
            {
            // Make sure the canvas is the same size as its conainer
            Size newCanvasSize = await JSRuntime.InvokeAsync<Size>("ResizeCanvas", this.containerDiv, this.canvas.GetCanvasRef());
            if ((newCanvasSize.Width != this.canvasSize.Width) || (newCanvasSize.Width != this.canvasSize.Width))
                {
                this.canvasSize = newCanvasSize;
                RecomputeImageScale();
                if (this.zoom.numerator == 100)
                    {
                    CenterImage();
                    }
                ComputeImageDisplayRect();

                this.fullScreen = await JSRuntime.InvokeAsync<bool>("IsFullScreen");
                await DrawCanvas();
                }
            }
        }
    }
