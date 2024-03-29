﻿using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Drawing;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Runtime.CompilerServices;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Xml.Schema;
using Blazor.Extensions;
using Blazor.Extensions.Canvas.Canvas2D;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using Portfolio.Client.Shared;
using System.Threading;
using Portfolio.Shared;
using Portfolio.Client.Views;

namespace Portfolio.Client.Pages
    {
    public partial class ImageViewer : ComponentBase
        {
        [Inject] IJSRuntime JSRuntime { get; set; }
        [Inject] HttpClient HttpClient { get; set; }
        [Inject] AppData AppData { get; set; }

        [Parameter]
        public String Name { get; set; }

        [Parameter]
        public int? ImageIndex { get; set; }

        private PortfolioInfo portfolioInfo = null;

        private ElementReference containerDiv;
        private ElementReference imageRef;
        private Canvas canvas;
        private bool fullScreen = false;
        private string fullScreenClass { get; set; } = "oi oi-fullscreen-enter";

        private bool galleryVisible = false;
        private string galleryClass
            {
            get
                {
                return "gallery" + (this.galleryVisible ? "" : " display-none");
                }
            }

        private String HD
            {
            get
                {
                return AppData.HD ? "HD" : "SD";
                }
            }

        private void ToggleHD()
            {
            AppData.HD = !AppData.HD;
            StateHasChanged();
            }
        private String ImageNumber
            {
            get
                {
                if (this.portfolioInfo?.FileNames?.Count > 0)
                    {
                    return $"{this.currentImageIndex + 1} / {this.portfolioInfo.FileNames.Count}";
                    }

                return "";
                }
            }

        private String FrameInfo
            {
            get
                {
                if (this.keyFrameCount > 1)
                    {
                    int secs = this.currentKeyFrameIndex / fps;
                    int frame = (this.currentKeyFrameIndex % fps) + 1;
                    return $"{secs,3:D3}:{frame,2:D2}";
                    }

                return "000:01";
                }
            }
        private string ZoomText
            {
            get
                {
                return (this.zoom < 0.10) ? $"{this.zoom * 100,3:F1}%" : $"{this.zoom * 100,3:F0}%";
                }
            }

        private string CurFileName
            {
            get
                {
                if (afterFirstRender)
                    return FullImagePath(this.currentImageIndex);

                return String.Empty;
                }
            }

        private String FullImagePath(String subFolder, int index)
            {
            String path = this.portfolioInfo.RootPath;
            String fileName = this.portfolioInfo.FileNames[index];

            return $"{path}/{subFolder}/{fileName}";
            }

        private String FullImagePath(int index)
            {
                if (index < this.portfolioInfo?.FileNames?.Count)
                    {
                    return FullImagePath(AppData.HD ? "hd" : "sd", index);
                    }

                return "";
            }
        private String FullThumbnailPath(int index)
            {
            return FullImagePath("1x1", index);
            }

        private int currentImageIndex = 0;

        private KeyFrame[] keyFrames;
        private int keyFrameCount = 0;
        private int currentKeyFrameIndex = 0;
        private const int fps = 60;
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
        private bool afterFirstRender = false;
        protected override async Task OnAfterRenderAsync(bool firstRender)
            {
            if (firstRender)
                {
                afterFirstRender = true;
                AppData.HttpClient = HttpClient;
                this.portfolioInfo = await AppData.GetPortfolioInfo(Name);
                this.currentImageIndex = 0;
                if (ImageIndex.HasValue)
                    {
                    if ((ImageIndex.Value >= 0) && (ImageIndex.Value < this.portfolioInfo?.FileNames?.Count))
                        {
                        this.currentImageIndex = ImageIndex.Value;
                        }
                    }
                StateHasChanged();
                await JSRuntime.InvokeVoidAsync("RegisterWindowHandler", DotNetObjectReference.Create<ImageViewer>(this));
                await OnResize();
                await this.containerDiv.FocusAsync();
                this.startImageFetch = DateTime.Now;
                StartRandomRectangles();
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
        private Aspect displayAspect;
        private Rectangle? image1x1Crop = null;
        private Rectangle? image3x2Crop = null;

        private void ZoomPlus(int delta)
            {
            ZoomTo(this.zoom + ((double) delta / 100.0));
            }

        private async Task OnClickZoomIn(MouseEventArgs args)
            {
            await Zoom(true, args.ShiftKey, args.CtrlKey);
            }
        private async Task Zoom(bool bigger, bool shiftKey, bool ctrlKey)
            {
            double factor = (shiftKey, ctrlKey) switch
                {
                    (false, false) => 2.0,
                    (false, true) => 1.5,
                    (true, false) => 1.10,
                    (true, true) => 1.01
                    };
            double amount = bigger ? factor : 1.0/factor;
            ZoomBy(amount);
            await DrawCanvas();
            }
        private async Task OnClickZoomOut(MouseEventArgs args)
            {
            await Zoom(false, args.ShiftKey, args.CtrlKey);
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
            StateHasChanged();

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

            // if zoomed image is bigger than canvas, keep the image on the canvas
            if ((this.displayAspect == Aspect.none) && (this.imageSizeZoomed.Width >= this.canvasSize.Width))
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

        private Rectangle ImageRectFromDisplayRect(Rectangle displayRect)
            {
            Rectangle imageRect = new Rectangle();

            // Shift and scale displayRect to image co-ordinates
            imageRect.X = (int)((double)(displayRect.X - (this.canvasAnchor.X - this.imageAnchor.X))/this.zoom);
            imageRect.Y = (int)((double)(displayRect.Y - (this.canvasAnchor.Y - this.imageAnchor.Y))/this.zoom);
            imageRect.Width = (int)((double)displayRect.Width/this.zoom);
            imageRect.Height = (int)((double)displayRect.Height/this.zoom);

            return imageRect;
            }

        private Rectangle Canvas1x1CropRect
            {
            get
                {
                Rectangle rect = Util.BiggestCenteredSquare(this.canvasSize.Width, this.canvasSize.Height);
                rect.Inflate(new Size(-20, -20));
                return rect;
                }
            }

        private Rectangle Canvas3x2CropRect
            {
            get
                {
                Rectangle rect = Util.BiggestCenteredRect3x2(this.canvasSize.Width, this.canvasSize.Height);
                rect.Inflate(new Size(-20, -20));
                return rect;
                }
            }

        private async Task DrawCanvas()
            {
            if ((this.imageNativeSize.Width > 0) && (this.imageNativeSize.Height > 0))
                {

                if (this._context == null)
                    {
                    this._context = await this.canvas.CreateCanvas2DAsync();
                    }

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

                if (this.displayAspect != Aspect.none)
                    {
                    Rectangle rect = Rectangle.Empty;
                    string color = "";
                    
                    switch (this.displayAspect)
                        {
                        case Aspect.square:
                            {
                            rect = Canvas1x1CropRect;
                            color = "red";
                            }
                            break;
                        case Aspect.rect3x2:
                            {
                            rect = Canvas3x2CropRect;
                            color = "blue";
                            }
                            break;
                        }

                    if (!rect.IsEmpty)
                        {
                        await this._context.BeginPathAsync();
                        await this._context.SetStrokeStyleAsync(color);
                        await this._context.SetLineWidthAsync(3);
                        await this._context.RectAsync(rect.X, rect.Y, rect.Width, rect.Height);
                        await this._context.StrokeAsync();
                        await this._context.EndBatchAsync();
                        }
                    }
                    

                if (this.displayInfo)
                    {
                    await this._context.BeginBatchAsync();
                    await this._context.SetStrokeStyleAsync("white");
                    await this._context.SetLineWidthAsync(1);
                    await this._context.SetFontAsync("lighter 16px menu");
                    int x = 10;
                    int line = 1;
                    int lineHeight = 25;
                    await this._context.StrokeTextAsync($"Zoom         : {this.zoom.ToString()}", x, lineHeight*line++);
                    await this._context.StrokeTextAsync($"Canvas Size: {this.canvasSize.ToString()}", x, lineHeight*line++);
                    await this._context.StrokeTextAsync($"Canvas Anchor: {this.canvasAnchor.ToString()}", x, lineHeight*line++);
                    await this._context.StrokeTextAsync($"Image Native Size : {this.imageNativeSize.ToString()}", x, lineHeight*line++);
                    await this._context.StrokeTextAsync($"Image Size Zoomed : {this.imageSizeZoomed.ToString()}", x, lineHeight*line++);
                    await this._context.StrokeTextAsync($"Image Anchor : {this.imageAnchor.ToString()}", x, lineHeight*line++);
                    await this._context.StrokeTextAsync($"Display Rect : {this.imageDisplayRect.ToString()}", x, lineHeight*line++);
                    await this._context.StrokeTextAsync($"Image Load   : {this.imageLoadTime.ToString()}", x, lineHeight*line++);
                    await this._context.StrokeTextAsync($"Image Index   : {this.currentImageIndex.ToString()}", x, lineHeight*line++);
                    await this._context.StrokeTextAsync($"Frame Number   : {this.currentKeyFrameIndex.ToString()}", x, lineHeight*line++);
                    if (this.currentKeyFrameIndex < this.keyFrameCount)
                        {
                        await this._context.StrokeTextAsync($"Frame Rectangle   : {this.keyFrames[this.currentKeyFrameIndex].Rectangle.ToString()}", x, lineHeight*line++);
                        }

                    await this._context.EndBatchAsync();
                    }
                }
            }

        DateTime startImageFetch;
        TimeSpan imageLoadTime;
        private async Task Next()
            {
            if (this.portfolioInfo?.FileNames?.Count > 0)
                {
                this.currentImageIndex++;
                if (this.currentImageIndex >= this.portfolioInfo?.FileNames?.Count)
                    this.currentImageIndex = 0;
                StateHasChanged();
                this.startImageFetch = DateTime.Now;

                StartRandomRectangles();

                // Prefetch the next image while the current one is being viewed.
                String nextImage = FullImagePath(this.currentImageIndex + 1);
                if (!String.IsNullOrEmpty(nextImage))
                    {
                    await JSRuntime.InvokeVoidAsync("PrefetchImage", nextImage);
                    }
                }
            }
        private async Task Previous()
            {
            if (this.portfolioInfo?.FileNames?.Count > 0)
                {
                this.currentImageIndex--;
                if (this.currentImageIndex < 0)
                    this.currentImageIndex = this.portfolioInfo.FileNames.Count - 1;
                StateHasChanged();
                this.startImageFetch = DateTime.Now;

                StartRandomRectangles();
                }
            }

        private void HideCrop()
            {
            this.displayAspect = Aspect.none;
            FitImageToCanvas();
            }

        private async Task GetImageCropRects()
            {
            if ((this.image1x1Crop is null) || (this.image3x2Crop is null))
                {
                string imagePath = FullImagePath("thumbnailInfo", this.currentImageIndex) + ".json";
                ThumbnailInfo? info = null;
                try
                    {
                    info = await HttpClient.GetFromJsonAsync<ThumbnailInfo>(imagePath);
                    }
                catch (HttpRequestException ex)
                    {
                    string msg = ex.ToString();
                    }
                catch (Exception ex)
                    {
                    string msg = ex.ToString();
                    }
                if (info is not null)
                    {
                    this.image1x1Crop  = info.CropTo1x1ScaledToHeight(this.imageNativeSize.Height);
                    this.image3x2Crop  = info.CropTo3x2ScaledToHeight(this.imageNativeSize.Height);
                    }
                }
            if (this.image1x1Crop is null)
                {
                // Default square crop
                this.image1x1Crop = Util.BiggestCenteredSquare(this.imageNativeSize.Width, this.imageNativeSize.Height);
                }
            if (this.image3x2Crop is null)
                {
                this.image3x2Crop = Util.BiggestCenteredRect3x2(this.imageNativeSize.Width, this.imageNativeSize.Height);
                }
            }
        private async Task ShowSquareCrop()
            {
            this.displayAspect = Aspect.square;
            await GetImageCropRects();
            FitImageRectangleToCanvasRectangle((Rectangle)this.image1x1Crop, Canvas1x1CropRect);
            }
        private async Task Show3x2RectCrop()
            {
            this.displayAspect = Aspect.rect3x2;
            await GetImageCropRects();
            FitImageRectangleToCanvasRectangle((Rectangle)this.image3x2Crop, Canvas3x2CropRect);
            }

        Timer randomRectanglesTimer = null;
        private void StartRandomRectangles()
            {
            this.randomRectanglesTimer = new Timer(RandomRectangle, null, 250, 25);
            }

        private void StopRandomRectangles()
            {
            if (this.randomRectanglesTimer != null)
                {
                this.randomRectanglesTimer.Dispose();
                this.randomRectanglesTimer = null;
                }
            }

        private void RandomRectangle(Object obj)
            {
            InvokeAsync(RandomRectangleAsync);
            }

        private async void RandomRectangleAsync()
            {
            if (this._context == null)
                {
                this._context = await this.canvas.CreateCanvas2DAsync();
                }

            Random random = new Random();

            Size margin = new Size() { Width = 25, Height = 25 };
            int width = random.Next(15, 150);
            int height = random.Next(15, 150);
            int x = random.Next(margin.Width, this.canvasSize.Width - (margin.Width + width));
            int y= random.Next(margin.Height, this.canvasSize.Height - (margin.Height + height));

            int color = random.Next(0, 0xFFFFFF);
            await this._context.BeginBatchAsync();
            await this._context.SetFillStyleAsync($"#{color:X6}");
            await this._context.FillRectAsync(x, y, width, height);
            await this._context.EndBatchAsync();
            }

        private async Task Back()
            {
            await JSRuntime.InvokeVoidAsync("GoBack");
            }

        private void ShowImage(int index)
            {
            this.currentImageIndex = index;
            StateHasChanged();
            this.startImageFetch = DateTime.Now;
            }

        private bool onKeyDownHandled = false;
        private bool OnKeyDownPreventDefault { get { return this.onKeyDownHandled; }  }
        private bool OnKeyDownStopPropogation { get { return this.onKeyDownHandled; }  }
        private async Task OnKeyDown(KeyboardEventArgs args)
            {
            this.onKeyDownHandled = false;
            if (args.CtrlKey)
                {
                switch (args.Key)
                    {
                    case "1":
                        this.onKeyDownHandled = true;
                        ZoomTo(1.00); // 100%
                        await DrawCanvas();
                        break;

                    case "0":
                        this.onKeyDownHandled = true;
                        FitImageToCanvas();
                        await DrawCanvas();
                        break;

                    case "+":
                    case "=":
                        this.onKeyDownHandled = true;
                        await Zoom(true, false,false);
                        break;

                    case "-":
                        this.onKeyDownHandled = true;
                        await Zoom(false, false,false);
                        break;

                    case "c":
                        this.onKeyDownHandled = true;
                        await SerializePortfolioInfo();
                        break;

                    case "s":
                        this.onKeyDownHandled = true;
                        SaveCropRect();
                        break;
                    }
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
                        this.onKeyDownHandled = true;
                        this.displayInfo = !this.displayInfo;
                        await DrawCanvas();
                        break;

                    case "a":
                        this.onKeyDownHandled = true;
                        this.displayAnchor = !this.displayAnchor;
                        await DrawCanvas();
                        break;

                    case "f":
                        this.onKeyDownHandled = true;
                        break;

                    case "ArrowLeft":
                        this.onKeyDownHandled = true;
                        await Previous();
                        break;

                    case "ArrowRight":
                        this.onKeyDownHandled = true;
                        await Next();
                        break;

                    case "0":
                    case "1":
                    case "2":
                    case "3":
                    case "4":
                    case "5":
                    case "6":
                    case "7":
                    case "8":
                    case "9":
                        this.onKeyDownHandled = true;
                        AddKeyFrame(args.Key[0] - '0');
                        break;
                    case "[":
                        this.onKeyDownHandled = true;
                        await OnStepBackward();
                        break;
                    case "]":
                        this.onKeyDownHandled = true;
                        await OnStepForward();
                        break;

                    case " ":
                        this.onKeyDownHandled = true;
                        await OnPlayPause();
                        break;

                    case "t":
                        this.onKeyDownHandled = true;
                        // Move to the next aspect ratio
                        switch (this.displayAspect)
                            {
                            case Aspect.none:
                                await ShowSquareCrop();
                                break;
                            case Aspect.square:
                                SaveSquareCropRect();
                                await Show3x2RectCrop();
                                break;
                            case Aspect.rect3x2:
                                Save3x2CropRect();
                                HideCrop();
                                break;
                            }
                        await DrawCanvas();
                        break;
                    }
                }
            }

        private async Task OnSkipForward()
            {
            await ShowFrame(this.keyFrameCount - 1);
            }

        private async Task OnSkipBackward()
            {
            await ShowFrame(0);
            }

        private async Task OnStepForward()
            {
            await StepFrame(1);
            }

        private async Task OnStepBackward()
            {
            await StepFrame(-1);
            }

        private String PlayPauseClass
            {
            get
                {
                return (this.timer == null) ? "oi-media-play" : "oi-media-pause";
                }
            }
        Timer timer;
        private async Task OnPlayPause()
            {
            if (this.timer == null)
                {
                Play();
                }
            else
                {
                Pause();
                }
            }

        private async Task Play()
            {
            int millis = 1000 / fps;
            if (this.currentKeyFrameIndex >= (this.keyFrameCount - 1))
                {
                await ShowFrame(0);
                }
            this.timer = new Timer(TimerTick, null, millis, millis);
            }

        private void TimerTick(object state)
            {
            InvokeAsync(async () =>
                {
                if (await StepFrame(1))
                    {
                    // All done... turn off the timer
                    Pause();
                    }
                });
            }

        private void Pause()
            {
            if (this.timer != null)
                {
                this.timer.Dispose();
                this.timer = null;
                StateHasChanged();
                }
            }
        private void AddKeyFrame(int iFrame)
            {
            this.keyFrames = null; // Will need to do InfillKeyFrames() again.
            this.keyFrameCount = 0;

            Animation animation = this.portfolioInfo.GetAnimation(this.currentImageIndex, true);
            animation.KeyFrameCanvasSize = this.canvasSize;

            Rectangle rectangle = this.imageDisplayRect;

            KeyFrame keyFrame;

            if (animation.KeyFrames == null)
                {
                animation.KeyFrames = new ();
                }

            // All the frames between the current last frame and iFrame are filled with the current rectangle.
            while (iFrame >= animation.KeyFrames.Count)
                {
                int newIndex = animation.KeyFrames.Count;
                keyFrame = new KeyFrame()
                    {
                    Rectangle = rectangle,
                    FrameNumber = fps * newIndex
                    };
                animation.KeyFrames.Add(keyFrame);
                }
            keyFrame = animation.KeyFrames[iFrame];
            keyFrame.Rectangle = rectangle;
            keyFrame.FrameNumber = fps * iFrame;
            }
        private async Task<bool> StepFrame(int increment)
            {
            return await ShowFrame(this.currentKeyFrameIndex + increment);
            }

        private async Task<bool> ShowFrame(int frameIndex)
            {
            if (this.keyFrames == null)
                {
                GenerateKeyFrames();
                }

            // Ensure frameIndex is in range
            if (frameIndex >= this.keyFrameCount)
                {
                frameIndex = this.keyFrameCount - 1;
                }
            else if (frameIndex < 0)
                {
                frameIndex = 0;
                }

            if (frameIndex < this.keyFrameCount)
                {
                this.imageDisplayRect = this.keyFrames[frameIndex].Rectangle;
                this.currentKeyFrameIndex = frameIndex;
                this.zoom = ((double) this.imageDisplayRect.Width) / this.imageNativeSize.Width;
                StateHasChanged();
                await DrawCanvas();
                }

            return (frameIndex == 0) || (frameIndex == this.keyFrameCount - 1);
            }

        private String TransportControlsClass
            {
            get
                {
                return (this.keyFrameCount > 1) ? "" : "hidden";
                }
            }
        private void GenerateKeyFrames()
            {
            Animation animation = this.portfolioInfo.GetAnimation(this.currentImageIndex);
            this.keyFrameCount = 0;
            this.currentKeyFrameIndex = 0;
            this.keyFrames = null;
            double scale = 1.0;

            FitImageToCanvas();
            if (animation?.KeyFrames?.Count > 0)
                {
                if (this.canvasSize != animation.KeyFrameCanvasSize)
                    {
                    double widthScale = ((double)this.canvasSize.Width) / animation.KeyFrameCanvasSize.Width;
                    double heightScale = ((double) this.canvasSize.Height) / animation.KeyFrameCanvasSize.Height;

                    // Pick the scale that's closest to 1.0
                    double widthDistance = Math.Abs(1.0 - widthScale);
                    double heightDistance = Math.Abs(1.0 - heightScale);
                    scale = (widthDistance < heightDistance) ? widthScale : heightScale;
                    }

                int maxFrameNumber = 0;
                foreach (KeyFrame keyFrame in animation.KeyFrames)
                    {
                    if (keyFrame.FrameNumber > maxFrameNumber)
                        {
                        maxFrameNumber = keyFrame.FrameNumber;
                        }
                    }

                this.keyFrameCount = maxFrameNumber + 1;  // Start counting frames at zero
                this.keyFrames = new KeyFrame[this.keyFrameCount];
                KeyFrame previousKeyFrame = null;
                foreach (KeyFrame keyFrame in animation.KeyFrames)
                    {
                    KeyFrame scaledKeyFrame = Scale(keyFrame, scale);
                    if (previousKeyFrame != null)
                        {
                        InfillKeyFrames(previousKeyFrame, scaledKeyFrame);
                        }
                    previousKeyFrame = scaledKeyFrame;
                    }
                }
            else
                {
                this.keyFrameCount = 1;
                this.keyFrames = new KeyFrame[1];
                this.keyFrames[0] = new()
                    {
                    FrameNumber = 0,
                    Rectangle = new Rectangle()
                        {
                        X = this.imageDisplayRect.X,
                        Y = this.imageDisplayRect.Y,
                        Width = this.imageDisplayRect.Width,
                        Height = this.imageDisplayRect.Height
                        }
                    };
                }
            }
        private int Scale(int value, double scale)
            {
            if (scale == 1.0)
                return value;

            return (int)((double)value * scale);
            }
        private Rectangle Scale(Rectangle rect, double scale)
            {
            return new()
                {
                X = Scale(rect.X, scale),
                Y = Scale(rect.Y, scale),
                Width = Scale(rect.Width, scale),
                Height = Scale(rect.Height, scale)
                };
            }

        private KeyFrame Scale(KeyFrame keyFrame, double scale)
            {
            return new()
                {
                FrameNumber = keyFrame.FrameNumber,
                Rectangle = Scale(keyFrame.Rectangle, scale)
                };
            }
        private void InfillKeyFrames(KeyFrame fromKeyFrame, KeyFrame toKeyFrame)
            {
            int stepCount = (toKeyFrame.FrameNumber - fromKeyFrame.FrameNumber) - 1;

            this.keyFrames[fromKeyFrame.FrameNumber] = fromKeyFrame;
            this.keyFrames[toKeyFrame.FrameNumber] = toKeyFrame;
            double xStep = ((double) (toKeyFrame.Rectangle.X - fromKeyFrame.Rectangle.X)) / stepCount;
            double widthStep = ((double) (toKeyFrame.Rectangle.Width - fromKeyFrame.Rectangle.Width)) / stepCount;
            double yStep = ((double) (toKeyFrame.Rectangle.Y - fromKeyFrame.Rectangle.Y)) / stepCount;
            double heightStep = ((double) (toKeyFrame.Rectangle.Height - fromKeyFrame.Rectangle.Height)) / stepCount;

            Rectangle baseRectangle = fromKeyFrame.Rectangle;

            for (int step = 1; step <= stepCount; step++)
                {
                int frameNumber = fromKeyFrame.FrameNumber + step;
                this.keyFrames[frameNumber] = new()
                    {
                    FrameNumber = frameNumber,
                    Rectangle = new()
                        {
                        X = baseRectangle.X + (int) ((double) step * xStep),
                        Y = baseRectangle.Y + (int) ((double) step * yStep),
                        Width = baseRectangle.Width + (int) ((double) step * widthStep),
                        Height = baseRectangle.Height + (int) ((double) step * heightStep)
                        }
                    };
                }
            }

        private async Task SerializePortfolioInfo()
            {
            JsonSerializerOptions opts = new() { WriteIndented = true };
            String s = JsonSerializer.Serialize<PortfolioInfo>(this.portfolioInfo, opts);
            await JSRuntime.InvokeVoidAsync("navigator.clipboard.writeText", s);
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

                await DrawCanvas();
                }
            }

        private void OnMouseOverGallery(MouseEventArgs args)
            {
            this.galleryVisible = true;
            StateHasChanged();
            }
        private void OnMouseOutGallery(MouseEventArgs args)
            {
            this.galleryVisible = false;
            StateHasChanged();
            }

        DateTime lastWheelXAction = DateTime.Now;
        private async Task OnMouseWheel(WheelEventArgs args)
            {
            if (Math.Abs(args.DeltaY) > 4)
                {
                ZoomPlus((int) -args.DeltaY);
                SetAnchor((int)args.OffsetX, (int)args.OffsetY, true);

                await DrawCanvas();
                }
            else if (Math.Abs(args.DeltaX) > 100)
                {
                DateTime now = DateTime.Now;

                if (now > this.lastWheelXAction.AddMilliseconds(500))
                    {
                    this.lastWheelXAction = now;

                    if (args.DeltaX > 0)
                        {
                        await Next();
                        }
                    else
                        {
                        await Previous();
                        }
                    }
                }
            }
        private void FitImageToCanvas()
            {
            Rectangle rectangle = new()
                {
                X = 0,
                Y = 0,
                Width = this.imageNativeSize.Width,
                Height = this.imageNativeSize.Height
                };

            FitImageRectangleToCanvas(rectangle);
            }

        private void FitImageRectangleToCanvas(Rectangle rectangle)
            {
            Rectangle canvasRect = new(0, 0, this.canvasSize.Width, this.canvasSize.Height);
            FitImageRectangleToCanvasRectangle(rectangle, canvasRect);
            this.minZoom = 0.9*this.zoom;
            }
        private void FitImageRectangleToCanvasRectangle(Rectangle imageRectangle, Rectangle canvasRectangle)
            {
            if ((imageRectangle.Width == 0) || (imageRectangle.Width == 0))
                return;

            double imageRatio = ((double) imageRectangle.Height) / ((double) imageRectangle.Width);
            double canvasRatio = ((double) canvasRectangle.Height) / ((double) canvasRectangle.Width);

            if (imageRatio < canvasRatio)
                {
                // The whole image rectangle will fit in the canvas rectangle with the width filled
                this.zoom = ((double)canvasRectangle.Width) / ((double)imageRectangle.Width);
                }
            else
                {
                // The whole image rectangle will fit in the canvas rectangle with the height filled
                this.zoom = ((double)canvasRectangle.Height) / ((double)imageRectangle.Height);
                }

            this.imageSizeZoomed.Width = (int) ((double) this.imageNativeSize.Width * this.zoom);
            this.imageSizeZoomed.Height = (int) ((double) this.imageNativeSize.Height * this.zoom);

            Rectangle rectangleZoomed = new()
                {
                X = (int) ((double) imageRectangle.X * this.zoom),
                Y = (int) ((double) imageRectangle.Y * this.zoom),
                Width = (int) ((double) imageRectangle.Width * this.zoom),
                Height = (int) ((double) imageRectangle.Height * this.zoom)
                };

            CenterImageRectangleWithinCanvasRectangle(rectangleZoomed, canvasRectangle);
            }
        private void CenterImage()
            {
            Rectangle rectangle = new()
                {
                X = 0,
                Y = 0,
                Width = this.imageSizeZoomed.Width,
                Height = this.imageSizeZoomed.Height
                };

            CenterImageRectangleWithinCanvas(rectangle);
            }
        private void CenterImageRectangleWithinCanvas(Rectangle imageRectangle)
            {
            Rectangle canvasRectangle = new Rectangle(0, 0, this.canvasSize.Width, this.canvasSize.Height);
            CenterImageRectangleWithinCanvasRectangle(imageRectangle, canvasRectangle);
            }
        private void CenterImageRectangleWithinCanvasRectangle(Rectangle imageRectangle, Rectangle canvasRectangle)
            {
            this.imageAnchor.X = imageRectangle.X + imageRectangle.Width / 2;
            this.imageAnchor.Y = imageRectangle.Y + imageRectangle.Height / 2;
            this.canvasAnchor.X = canvasRectangle.X + canvasRectangle.Width / 2;
            this.canvasAnchor.Y = canvasRectangle.Y + canvasRectangle.Height / 2;
            ComputeImageDisplayRect();
            }

        private async Task OnImageLoaded()
            {
            DateTime finish = DateTime.Now;
            this.imageLoadTime = finish - this.startImageFetch;
            this.image3x2Crop = null;
            this.image1x1Crop = null;
            HideCrop();

            StopRandomRectangles();

            this.imageNativeSize = await JSRuntime.InvokeAsync<Size>("GetNaturalSize", this.imageRef);
            GenerateKeyFrames();
            if (this.keyFrameCount > 0)
                {
                await Play();
                }
            }

        [JSInvokable]
        public async Task OnResize()
            {
            // Make sure the canvas is the same size as its conainer
            Size newCanvasSize = await JSRuntime.InvokeAsync<Size>("ResizeCanvas", this.containerDiv, this.canvas.GetCanvasRef());
            if ((newCanvasSize.Width != this.canvasSize.Width) || (newCanvasSize.Height != this.canvasSize.Height))
                {
                this.canvasSize = newCanvasSize;
                if (this.zoom == this.minZoom)
                    {
                    FitImageToCanvas();
                    }
                ComputeImageDisplayRect();

                GenerateKeyFrames();

                this.fullScreen = await JSRuntime.InvokeAsync<bool>("IsFullScreen");
                await DrawCanvas();
                }
            }

        private void SaveSquareCropRect()
            {
            this.image1x1Crop = ImageRectFromDisplayRect(Canvas1x1CropRect);
            }
        private void Save3x2CropRect()
            {
            this.image3x2Crop = ImageRectFromDisplayRect(Canvas3x2CropRect);
            }
        private async Task SaveCropRect()
            {
            ThumbnailInfo info = new ThumbnailInfo();

            info.ImageHeight = this.imageNativeSize.Height;
            info.CropSquareRect = (Rectangle)this.image1x1Crop;
            info.Crop3x2Rect = (Rectangle)this.image3x2Crop;
            string imagePath = FullImagePath("thumbnailInfo", this.currentImageIndex) + ".json";
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Put, imagePath)
                {
                Content = System.Net.Http.Json.JsonContent.Create(info)
                };
            HttpResponseMessage response = await HttpClient.SendAsync(request);
            string s = await response.Content.ReadAsStringAsync();
            }
        }
    }
