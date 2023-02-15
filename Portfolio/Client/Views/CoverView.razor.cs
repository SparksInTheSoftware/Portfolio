using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Blazor.Extensions;
using Blazor.Extensions.Canvas.Canvas2D;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.Web.Virtualization;
using Microsoft.JSInterop;
using Portfolio.Client.Shared;
using Portfolio.Shared;

namespace Portfolio.Client.Views
    {
    public partial class CoverView : ComponentBase
        {
        [Inject] IJSRuntime JSRuntime { get; set; }
        [Inject] NavigationManager NavigationManager { get; set; }

        private ElementReference containerDiv;

        [Parameter]
        public PortfolioInfo PortfolioInfo { get; set; }

        private List<CoverView> coverViews;
        [Parameter]
        public List<CoverView> CoverViews
            {
            get
                {
                return this.coverViews;
                }
            set
                {
                this.coverViews = value;
                this.coverViews.Add(this);
                }
            }
        private Canvas canvas;
        private Size marginSize = new() { Width = 10, Height = 10 };
#if false
        private Canvas canvas0;
        private Canvas canvas1;
        private Canvas canvas2;
        private Canvas canvas3;
        private Canvas canvas4;
#endif
        private ElementReference imageRef0;
        private ElementReference imageRef1;
        private ElementReference imageRef2;
        private ElementReference imageRef3;
        private ElementReference imageRef4;
        private ElementReference [] imageRefs;
        private Size[] imageNativeSizes;


        private int[] coverImageIndexes;
        private int[] randomImageIndexes;
        private int curRandomImageIndex;
        private int[] CoverImageIndexes
            {
            get
                {
                if (this.coverImageIndexes is null)
                    {
                    this.coverImageIndexes = new int[5];
                    RandomUpdateCoverImage(0);
                    RandomUpdateCoverImage(1);
                    RandomUpdateCoverImage(2);
                    RandomUpdateCoverImage(3);
                    RandomUpdateCoverImage(4);
                    }
                return this.coverImageIndexes;
                }
            }

        private void RandomUpdateCoverImage(int coverImageIndex = -1)
            {
            Random random = new Random();
            if (coverImageIndex == -1)
                {
                coverImageIndex = random.Next(0, 5);
                }

            int count = PortfolioInfo.FileNames.Count;

            if (this.curRandomImageIndex >= count)
                {
                this.randomImageIndexes= null;
                }

            if (this.randomImageIndexes is null)
                {
                this.randomImageIndexes = new int[count];
                this.curRandomImageIndex = 0;

                // Initialize the randomImageIndexes to a non-random state
                // that includes all the possible indexes
                for (int i = 0; i < count; i++)
                    {
                    this.randomImageIndexes[i] = i;
                    }

                // Do a bunch of random swaps
                for (int i = 0; i < 2*count; i++)
                    {
                    int i1 = random.Next(0, count);
                    int i2 = random.Next(0, count);

                    // Swap values at indexes i1 and i2
                    int t = this.randomImageIndexes[i1];
                    this.randomImageIndexes[i1] = this.randomImageIndexes[i2];
                    this.randomImageIndexes[i2] = t;
                    }
                }
            this.CoverImageIndexes[coverImageIndex] = this.randomImageIndexes[this.curRandomImageIndex++];
            }

        private Size CoverSize
            {
            get
                {
                int maxBottom = 0;
                int maxRight = 0;
                if (this.rectangles == null)
                    {
                    InitRectangles(this.canvasSize.Height);
                    }

                for (int i = 0; i < this.rectangles.Length; i++)
                    {
                    Rectangle rect = this.rectangles[i];
                    if (rect.Bottom > maxBottom)
                        {
                        maxBottom = rect.Bottom;
                        }
                    if (rect.Right > maxRight)
                        {
                        maxRight = rect.Right;
                        }
                    }

                return new Size(maxRight + this.marginSize.Width, maxBottom + this.marginSize.Height);
                }
            }

        private const int gap = 4;
        private Rectangle[] rectangles;
        enum Aspect { _1x1, _2x3 };
        private Aspect [] rectangleAspects;
        private String[] aspectFolderNames = { "1x1", "2x3" };

        private void InitRectangles(int h)
            {
            h = h - 2 * this.marginSize.Height;
            if (PortfolioInfo == null)
                {
                InitRectangles1(h);
                return;
                }

            switch (PortfolioInfo.CoverStyle)
                {
                case 1:
                    InitRectangles1(h);
                    break;

                case 2:
                    InitRectangles2(h);
                    break;

                case 3:
                    InitRectangles3(h);
                    break;

                default:
                    InitRectangles1(h);
                    break;
                }
            }

        private void InitRectangles1(int h)
            {
            /*
             * Two squares in first column
             * Three rectangles in second column
             */
            this.rectangleAspects = new Aspect[5];
            this.rectangleAspects[0] = Aspect._1x1;
            this.rectangleAspects[1] = Aspect._1x1;
            this.rectangleAspects[2] = Aspect._2x3;
            this.rectangleAspects[3] = Aspect._2x3;
            this.rectangleAspects[4] = Aspect._2x3;

            double ratio = 3.0 / 2.0;
            this.rectangles = new Rectangle[5];
            int x = this.marginSize.Width;
            int y = this.marginSize.Height;
            int h1 = (h - gap) / 2;
            int w1 = h1;
            int h2 = ((2 * h1 + gap) - 2 * gap) / 3;
            int w2 = (int)((double)h2 * ratio);

            int index = 0;

            // Two squares
            this.rectangles[index++] = new Rectangle(x, y, w1, h1);
            y += h1 + gap;
            this.rectangles[index++] = new Rectangle(x, y, w1, h1);

            // Three rectangles
            x = this.marginSize.Width + w1 + gap;
            y = this.marginSize.Height;

            // Three rectangles
            this.rectangles[index++] = new Rectangle(x, y, w2, h2);
            y += h2 + gap;
            this.rectangles[index++] = new Rectangle(x, y, w2, h2);
            y += h2 + gap;
            this.rectangles[index++] = new Rectangle(x, y, w2, h2);
            }

        private void InitRectangles2(int h)
            {
            /*
             * Two rectangles (w1 x h1) on the first row
             * Three squares (h2 x h2) on the bottom row.
             * 
             *  1) w = w1 + gap + w1 = 2*w1 + gap
             *  2) w = w2 + gap + w2 + gap + w2 = 3*w2 + 2*gap
             *  3) h = h1 + gap + h2
             *  4) w1 = h1 * ratio
             * 
             *  5) 2*w1 + gap = 3*w2 + 2*gap (line 1 == line 2)
             *  6) 2*w1 = 3*w2 + gap
             *  7) w1 = (3*w2 + gap)/2
             *  8) h1*ratio = (3*h2 + gap)/2 (line 4 == line 7)
             *  9) h1 = (3*h2 + gap)/(2*ratio)
             * 10) h2 = h - h1 - gap (from line 3)
             * 11) h1 = (3*(h - h1 - gap) + gap)/(2*ratio) (substitute line 10 in line 9)
             * 12) 2*ratio*h1 = 3*(h - h1 - gap) + gap
             * 13) 2*ratio*h1 = 3*h - 3*h1 - 3*gap + gap
             * 14) 2*ratio*h1 = 3*h - 3*h1 - 2*gap
             * 15) 2*ratio*h1 + 3*h1 = 3*h - 2*gap
             * 16) (2*ratio + 3)*h1 = 3*h - 2*gap
             * 17) h1 = (3*h - 2*gap)/(2*ratio + 3)
             *
             */
            this.rectangleAspects = new Aspect[5];
            this.rectangleAspects[0] = Aspect._2x3;
            this.rectangleAspects[1] = Aspect._2x3;
            this.rectangleAspects[2] = Aspect._1x1;
            this.rectangleAspects[3] = Aspect._1x1;
            this.rectangleAspects[4] = Aspect._1x1;
            double ratio = 3.0 / 2.0;
            int h1 = (int)((3.0 * h - 2.0 * gap) / (2.0 * ratio + 3.0));
            int h2 = h - h1 - gap;
            int w2 = h2;
            int w1 = (int)((double) h1 * ratio);  //(int)((3.0*(w2 + gap))/2.0 );

            this.rectangles = new Rectangle[5];
            int x = this.marginSize.Width;
            int y = this.marginSize.Height;


            int index = 0;

            // Two rectangles on first row
            this.rectangles[index++] = new Rectangle(x, y, w1, h1);
            x += w1 + gap;
            this.rectangles[index++] = new Rectangle(x, y, w1, h1);

            x = this.marginSize.Width;
            y = this.marginSize.Height + h1 + gap;

            // Three squares on bottom row
            this.rectangles[index++] = new Rectangle(x, y, w2, h2);
            x += w2 + gap;
            this.rectangles[index++] = new Rectangle(x, y, w2, h2);
            x += w2 + gap;
            this.rectangles[index++] = new Rectangle(x, y, w2, h2);

            if (this.rectangles[1].Right < this.rectangles[4].Right)
                {
                this.rectangles[1].X = this.rectangles[4].Right - w1;
                }
            else if (this.rectangles[1].Right < this.rectangles[4].Right)
                {
                this.rectangles[4].X = this.rectangles[1].Right - w2;
                }
            }

        private void InitRectangles3(int h)
            {
            /*
            2 squares in top row.
            3 squares in bottom row
            Squares... so h = w, h1 = w1, h2 = w2

             1) h = 2 * h1 + gap
             2) h = 3 * h2 + 2*gap
             3) 2*h1 + gap = 3*h2 + 2*gap  (line 1 == line 2)
             4) h1 = (3*h2 + gap) / 2

             5) h = h1 + h2 + gap (one square of height h1 and one square of height h2 with gap between)
             6) h = (3*h2 + gap) / 2 + h2 + gap (substitute line 4 into line 5)
             7) h = 2*h2/2 + (3*h2 + gap)/2 + gap
             8) h = (2*h2 + (3*h2 +gap))/2 + gap
             9) h = (5*h2 + gap)/2 + gap
            10) h - gap = (5*h2 + gap)/2
            11) 2*(h - gap) = 5*h2 + gap
            12) h2 = (2*(h -gap) – gap)/5
             */
            this.rectangleAspects = new Aspect[5];
            this.rectangleAspects[0] = Aspect._1x1;
            this.rectangleAspects[1] = Aspect._1x1;
            this.rectangleAspects[2] = Aspect._1x1;
            this.rectangleAspects[3] = Aspect._1x1;
            this.rectangleAspects[4] = Aspect._1x1;

            this.rectangles = new Rectangle[5];
            int h2 = (2 * (h - gap) - gap) / 5;
            int h1 = (3 * h2 + gap) / 2;

            int index = 0;
            int x = this.marginSize.Width;
            int y = this.marginSize.Height;

            // Two large squares on first row
            this.rectangles[index++] = new Rectangle(x, y, h1, h1);
            x += h1 + gap;
            this.rectangles[index++] = new Rectangle(x, y, h1, h1);

            x = this.marginSize.Width;
            y = this.marginSize.Width + h1 + gap;

            // Three small squares on bottom row
            this.rectangles[index++] = new Rectangle(x, y, h2, h2);
            x += h2 + gap;
            this.rectangles[index++] = new Rectangle(x, y, h2, h2);
            x += h2 + gap;
            this.rectangles[index++] = new Rectangle(x, y, h2, h2);
            }

        private Timer delayDrawTimer;
        private Timer updateCoverImageTimer;
        protected override async Task OnAfterRenderAsync(bool firstRender)
            {
            if (firstRender)
                {
                this.imageRefs = new ElementReference[5];
                this.imageRefs[0] = this.imageRef0;
                this.imageRefs[1] = this.imageRef1;
                this.imageRefs[2] = this.imageRef2;
                this.imageRefs[3] = this.imageRef3;
                this.imageRefs[4] = this.imageRef4;
                this.imageNativeSizes = new Size[5];

                await this.containerDiv.FocusAsync();
                await OnResize();
                this.delayDrawTimer = new Timer(DelayDraw, null, 10, 0);
                this.updateCoverImageTimer = new Timer(UpdateCoverImageTick, null, 5000, 0);
                }
            else
                {
                await OnResize();
                await DrawCanvas();
                }
            }

        private void DelayDraw(object obj)
            {
            InvokeAsync(async () =>
                {
                await DrawCanvas();
                });
            }

        private void UpdateCoverImageTick(object obj)
            {
            RandomUpdateCoverImage();
            StateHasChanged();
            Random random = new Random();
            this.updateCoverImageTimer.Change(random.Next(1000, 4000), 0);
            }

        private String FileName(int index)
            {
            if ((this.rectangleAspects != null) && (index < PortfolioInfo?.FileNames?.Count))
                {
                int imageIndex = CoverImageIndexes[index];
                String folder = this.aspectFolderNames[(int) this.rectangleAspects[index]];
                String fileName = PortfolioInfo.FileNames[imageIndex];

                return $"{PortfolioInfo.RootPath}\\{folder}\\{fileName}";
                }

            return "";
            }

        private async Task OnImageLoaded0()
            {
            await OnImageLoaded(0);
            }
        private async Task OnImageLoaded1()
            {
            await OnImageLoaded(1);
            }
        private async Task OnImageLoaded2()
            {
            await OnImageLoaded(2);
            }
        private async Task OnImageLoaded3()
            {
            await OnImageLoaded(3);
            }
        private async Task OnImageLoaded4()
            {
            await OnImageLoaded(4);
            }
        private async Task OnImageLoaded(int index)
            {
            this.imageNativeSizes[index] = await JSRuntime.InvokeAsync<Size>("GetNaturalSize", this.imageRefs[index]);
            await DrawImage(index);
            }

        private int imageIndex = 0;
        private bool ignoreClick = false;
        protected void OnClick()
            {
            if (!this.ignoreClick)
                {
                NavigationManager.NavigateTo($"ImageViewer\\{PortfolioInfo.Name}\\{this.imageIndex}");
                }
            }

        private async Task OnMouseDown(MouseEventArgs args)
            {
            await this.containerDiv.FocusAsync();
            this.ignoreClick = true;
            if (args.AltKey)
                {
                PortfolioInfo.CoverStyle++;
                if (PortfolioInfo.CoverStyle > 3)
                    {
                    PortfolioInfo.CoverStyle = 1;
                    }
                InitRectangles(this.canvasSize.Height);
                StateHasChanged();
                }
            else if (args.CtrlKey)
                {
                int index = RectangleIndexAt((int) args.OffsetX, (int) args.OffsetY);
                if (index != -1)
                    {
                    if (args.ShiftKey)
                        {
                        CoverImageIndexes[index]--;
                        if (CoverImageIndexes[index] < 0)
                            {
                            CoverImageIndexes[index] = PortfolioInfo.FileNames.Count - 1;
                            }
                        }
                    else
                        {
                        CoverImageIndexes[index]++;
                        if (CoverImageIndexes[index] >= PortfolioInfo.FileNames.Count)
                            {
                            CoverImageIndexes[index] = 0;
                            }
                        }
                    StateHasChanged();
                    }
                }
            else
                {
                this.ignoreClick = false;
                this.imageIndex = 0;
                int rectIndex = RectangleIndexAt((int) args.OffsetX, (int) args.OffsetY);
                if (rectIndex != -1)
                    {
                    this.imageIndex = CoverImageIndexes[rectIndex];
                    }
                }
            }

        private int RectangleIndexAt(int x, int y)
            {
            for (int i = 0; i < 5; i++)
                {
                Rectangle rect = this.rectangles[i];
                if ((x >= rect.X && x <= rect.Right) && (y >= rect.Y && y <= rect.Bottom))
                    return i;
                }
            return -1;
            }

        private Size canvasSize = new () { Width = 0, Height = 0 };
        private Canvas2DContext canvas2Dcontext;
        private async Task DrawCanvas()
            {
            if ((this.canvasSize.Width > 0) && (this.canvasSize.Height > 0))
                {

                if (this.canvas2Dcontext == null)
                    {
                    this.canvas2Dcontext = await this.canvas.CreateCanvas2DAsync();
                    }

                for (int i = 0; i < this.rectangles.Length; i++)
                    {
                    await DrawImage(i);
                    }
                }
            }

        private bool displayInfo = false;
        private async Task DrawImage(int index)
            {
            Rectangle rectangle = this.rectangles[index];
            Size imageSize = this.imageNativeSizes[index];
            ElementReference imageRef = this.imageRefs[index];

            await this.canvas2Dcontext.BeginBatchAsync();

            await this.canvas2Dcontext.DrawImageAsync(imageRef,
                0, 0, imageSize.Width, imageSize.Height,
                rectangle.X, rectangle.Y, rectangle.Width, rectangle.Height);

            await this.canvas2Dcontext.BeginPathAsync();
            await this.canvas2Dcontext.SetStrokeStyleAsync("#2C3539");
            await this.canvas2Dcontext.SetLineWidthAsync(2);
            await this.canvas2Dcontext.RectAsync(rectangle.Left, rectangle.Top, rectangle.Width, rectangle.Height);
            await this.canvas2Dcontext.StrokeAsync();
            await this.canvas2Dcontext.EndBatchAsync();

            if (this.displayInfo)
                {
                await this.canvas2Dcontext.BeginBatchAsync();
                await this.canvas2Dcontext.SetStrokeStyleAsync("black");
                await this.canvas2Dcontext.SetLineWidthAsync(1);
                await this.canvas2Dcontext.SetFontAsync("lighter 16px menu");
                int x = rectangle.Left + 10;
                int y = rectangle.Top + (rectangle.Height/2);
                await this.canvas2Dcontext.StrokeTextAsync($"{rectangle.Width} x {rectangle.Height}", x, y);

                await this.canvas2Dcontext.EndBatchAsync();
                }
            }

        public async Task OnResize()
            {
            Size newCanvasSize = await JSRuntime.InvokeAsync<Size>("ResizeCanvas", this.containerDiv, this.canvas.GetCanvasRef());
            if ((newCanvasSize.Width != this.canvasSize.Width) || (newCanvasSize.Height != this.canvasSize.Height))
                {
                // Console.WriteLine($"OnResize() - {newCanvasSize.Width} x {newCanvasSize.Height}");
                this.canvasSize = newCanvasSize;
                InitRectangles(this.canvasSize.Height);
                StateHasChanged();
                }
            }
        }
    }
