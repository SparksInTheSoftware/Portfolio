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

        private Size imageSizeZoomed = new() { Width = 0, Height = 0 };

        // imageAnchor is the point in image rect that corresponds to canvasAnchor
        private Point imageAnchor;

        private double zoom = 1.0; // 100%
        private double minZoom;
        private bool displayInfo = false;
        private bool displayAnchor = false;


        private void ZoomPlus(int delta)
            {
            ZoomTo(this.zoom + ((double) delta / 100.0));
            }

        private void ZoomBy(double scale)
            {
            ZoomTo(this.zoom * scale);

            }
        private void ZoomTo(double percent)
            {
            double prevZoom = this.zoom;
            if (percent < this.minZoom)
                percent = this.minZoom;

            this.zoom = percent;

            this.imageAnchor.X = (int) (((double) this.imageAnchor.X * this.zoom) /  prevZoom);
            this.imageAnchor.Y = (int) (((double) this.imageAnchor.Y * this.zoom) / prevZoom);
            ComputeImageDisplayRect();
            }

        private void ComputeImageDisplayRect()
            {
            // Figure out where the image should be in canvas co-ordinates
            //     1. Scale the image rect
            //     2. Line up the anchor points
            this.imageSizeZoomed.Width = (int) ((double) this.imageNativeSize.Width * this.zoom);
            this.imageSizeZoomed.Height = (int) ((double) this.imageNativeSize.Height * this.zoom);
            this.imageDisplayRect = new(origin, this.imageSizeZoomed);

            // Shift the image so that its anchor point lines up
            // with the anchor point on the canvas.
            // The anchor points are initially the centers of the image and the canvas,
            // but the anchor point changes to the mouse position when dragging and zooming.
            Point offset = new()
                {
                X = this.canvasAnchor.X - this.imageAnchor.X,
                Y = this.canvasAnchor.Y - this.imageAnchor.Y
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
                    await this._context.StrokeTextAsync($"Image Anchor : {this.imageAnchor.ToString()}", 10, 25);
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
                        ZoomTo(1.00); // 100%
                        ComputeImageDisplayRect();
                        break;

                    case "0":
                        FitImageToCanvas();
                        ComputeImageDisplayRect();
                        break;

                    case "+":
                    case "=":
                        ZoomBy(2.0);
                        break;

                    case "-":
                        ZoomBy(0.5);
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
                this.imageAnchor = newAnchor - (Size) this.imageDisplayRect.Location;
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

        private void FitImageToCanvas()
            {
            if ((this.imageNativeSize.Width == 0) || (this.canvasSize.Width == 0))
                return;

            double imageRatio = ((double) this.imageNativeSize.Height) / ((double) this.imageNativeSize.Width);
            double canvasRatio = ((double) this.canvasSize.Height) / ((double) this.canvasSize.Width);

            if (imageRatio < canvasRatio)
                {
                // The whole image will fit on the canvas with the width filled
                this.zoom = ((double)this.canvasSize.Width) / ((double)this.imageNativeSize.Width);
                }
            else
                {
                // The whole image will fit on the canvas with the height filled
                this.zoom = ((double)this.canvasSize.Height) / ((double)this.imageNativeSize.Height);
                }

            this.minZoom = this.zoom + 0; //TODO: need copy operator
            this.imageSizeZoomed.Width = (int) ((double) this.imageNativeSize.Width * this.zoom);
            this.imageSizeZoomed.Height = (int) ((double) this.imageNativeSize.Height * this.zoom);
            CenterImage();
            }
        private void CenterImage()
            {
            this.imageAnchor.X = this.imageSizeZoomed.Width / 2;
            this.imageAnchor.Y = this.imageSizeZoomed.Height / 2;
            this.canvasAnchor.X = this.canvasSize.Width / 2;
            this.canvasAnchor.Y = this.canvasSize.Height / 2;
            }

        private async Task OnImageLoaded()
            {
            DateTime finish = DateTime.Now;
            this.imageLoadTime = finish - this.startImageFetch;

            this.imageNativeSize = await JSRuntime.InvokeAsync<Size>("GetNaturalSize", this.imageRef);
            FitImageToCanvas();
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
                if (this.zoom == this.minZoom)
                    {
                    FitImageToCanvas();
                    }
                ComputeImageDisplayRect();

                this.fullScreen = await JSRuntime.InvokeAsync<bool>("IsFullScreen");
                await DrawCanvas();
                }
            }
        }
    }
