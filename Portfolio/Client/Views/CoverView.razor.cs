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

namespace Portfolio.Client.Views
    {
    public partial class CoverView : ComponentBase
        {
        [Inject] IJSRuntime JSRuntime { get; set; }
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
        protected override async Task OnAfterRenderAsync(bool firstRender)
            {
            if (firstRender)
                {
                await this.containerDiv.FocusAsync();
                await OnResize();
                this.delayDrawTimer = new Timer(DelayDraw, null, 10, 0);
                }
            else
                {
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

        private String FileName(int index)
            {
            if (index < PortfolioInfo?.CoverImageFileNames?.Count)
                {
                return PortfolioInfo.CoverImageFileNames[index];
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
        private async Task OnImageLoaded(int imageIndex)
            {
#if false
            this.imageNativeSize = await JSRuntime.InvokeAsync<Size>("GetNaturalSize", this.imageRef);
            GenerateKeyFrames();
            if (this.keyFrameCount > 0)
                {
                await Play();
                }
#endif
            }

        protected async Task OnClick()
            {
            await DrawCanvas();
            }

        private Size imageNativeSize = new () { Width = 1, Height = 1};
        private Size canvasSize = new () { Width = 0, Height = 0};
        private Canvas2DContext _context;
        private async Task DrawCanvas()
            {

            if ((this.imageNativeSize.Width > 0) && (this.imageNativeSize.Height > 0))
                {

                this._context = await this.canvas.CreateCanvas2DAsync();

                for (int i = 0; i < this.rectangles.Length; i++)
                    {
                    await DrawImage(i);
                    }
                }
            }

        private bool displayInfo = true;
        private async Task DrawImage(int imageIndex)
            {
            Rectangle rectangle = this.rectangles[imageIndex];

            await this._context.BeginBatchAsync();

#if false
            await this._context.DrawImageAsync(this.imageRef,
                0, 0, this.imageNativeSize.Width, this.imageNativeSize.Height,
                this.imageDisplayRect.X, this.imageDisplayRect.Y, this.imageDisplayRect.Width, this.imageDisplayRect.Height);
#endif

            await this._context.BeginPathAsync();
            await this._context.SetStrokeStyleAsync("gray");
            await this._context.SetLineWidthAsync(2);
            await this._context.RectAsync(rectangle.Left, rectangle.Top, rectangle.Width, rectangle.Height);
            await this._context.StrokeAsync();
            await this._context.EndBatchAsync();

            if (this.displayInfo)
                {
                await this._context.BeginBatchAsync();
                await this._context.SetStrokeStyleAsync("black");
                await this._context.SetLineWidthAsync(1);
                await this._context.SetFontAsync("lighter 16px menu");
                int x = rectangle.Left + 10;
                int y = rectangle.Top + (rectangle.Height/2);
                await this._context.StrokeTextAsync($"{rectangle.Width} x {rectangle.Height}", x, y);

                await this._context.EndBatchAsync();
                }
            }

        public async Task OnResize()
            {
            Size newCanvasSize = await JSRuntime.InvokeAsync<Size>("ResizeCanvas", this.containerDiv, this.canvas.GetCanvasRef());
            if ((newCanvasSize.Width != this.canvasSize.Width) || (newCanvasSize.Height != this.canvasSize.Height))
                {
                this.canvasSize = newCanvasSize;
                InitRectangles(this.canvasSize.Height);
                StateHasChanged();
                }
            }
        }
    }
