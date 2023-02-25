using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Blazor.Extensions;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.Web.Virtualization;
using Microsoft.JSInterop;
using Portfolio.Client.Shared;
using Portfolio.Shared;

namespace Portfolio.Client.Views
    {
    enum Aspect { none, square, rect_3x2 };
    record CoverLayout
        {
        public bool rowFirst;
        public string thumbnailsClass;
        public string[] groupClass;
        public Aspect [,] thumbnailAspects;
        public string[,] heights; 
        };
    public partial class CoverView : ComponentBase
        {
        [Inject] IJSRuntime JSRuntime { get; set; }
        [Inject] NavigationManager NavigationManager { get; set; }

        private ElementReference coverElement;

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

        /*
         * Up to nine images are displayed in the cover.
         * They are in three groups of three.
         * Not all slots are used.
         * coverImageIndexes(i, j) is the index into PortfolioInfo.Files for the given slot.
         * A negative number indicates the slot is not used.
         */


        private int[,] coverImageIndexes;

        // randomImageIndexes is a randomization of all the indexes into Portfolio.Files
        // It is used to pick the next random image to display from the sequence.
        // A new sequence is generated when all the images in the current sequence have been displayed.
        // curRandomImageIndex is the index into randomImageIndexes that will be used next.
        private int[] randomImageIndexes;
        private int curRandomImageIndex;
        private int[,] CoverImageIndexes
            {
            get
                {
                if (this.coverImageIndexes is null)
                    {
                    this.coverImageIndexes = new int[3, 3] { { -1, -1, -1 }, { -1, -1, -1 }, { -1, -1, -1 } };
                    for (int i = 0; i < 3; i++)
                        {
                        for (int j = 0; j < 3; j++)
                            {
                            RandomUpdateCoverImage(i, j);
                            }
                        }
                    }
                return this.coverImageIndexes;
                }
            }

        private void RandomUpdateCoverImage(int i, int j)
            {
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
                for (int index = 0; index < count; index++)
                    {
                    this.randomImageIndexes[index] = index;
                    }

                // Do a bunch of random swaps
                for (int index = 0; index < 2*count; index++)
                    {
                    Random random = new Random();

                    int i1 = random.Next(0, count);
                    int i2 = random.Next(0, count);

                    // Swap values at indexes i1 and i2
                    int t = this.randomImageIndexes[i1];
                    this.randomImageIndexes[i1] = this.randomImageIndexes[i2];
                    this.randomImageIndexes[i2] = t;
                    }
                }
            this.CoverImageIndexes[i,j] = this.randomImageIndexes[this.curRandomImageIndex++];
            }

        private Timer [] updateCoverImageTimers;
        protected override async Task OnAfterRenderAsync(bool firstRender)
            {
            if (firstRender)
                {
                await this.coverElement.FocusAsync();
                this.updateCoverImageTimers = new Timer[9];

                for (int index = 0; index < 9; index++)
                    {
                    this.updateCoverImageTimers[index] = new Timer(UpdateCoverImageTick, index, index*1000, 0);
                    }
                }
            }
        private void UpdateCoverImageTick(object obj)
            {
            int index = (int)obj;
            int row, column;
            row = index / 3;
            column = index % 3;

            RandomUpdateCoverImage(row, column);
            StateHasChanged();
            Random random = new Random();
            this.updateCoverImageTimers[index].Change(random.Next(5000,10000), 0);
            }

        private string SubFolder(Aspect aspect)
            {
            return aspect switch
                {
                    Aspect.square => "1x1",
                    Aspect.rect_3x2 => "3x2",
                    _ => ""
                    };
            }

        private String FileName(int row, int column)
            {
            int fileIndex = CoverImageIndexes[row, column];
            if ((fileIndex >= 0) && (fileIndex < PortfolioInfo?.FileNames?.Count))
                {
                String folder = SubFolder(CoverLayout.thumbnailAspects[row, column]);
                String fileName = PortfolioInfo.FileNames[fileIndex];

                return $"{PortfolioInfo.RootPath}/{folder}/{fileName}";
                }

            return "";
            }

        /*
         * CoverStyle == 1
         * 
         *     Column first - column x row
         *     Two squares in first column
         *     Three rectangles in second column
         *     
         *     1x1 3x2
         *     1x1 3x2
         *         3x2
         *     
         *     h1xh1 w2xh2
         *     h1xh1 w2xh2
         *           w2xh2
         *           
         * CoverStyle == 2
         * 
         *     Row first - row x column
         *     Two 3x2 rectangles (w1 x h1) on the first row
         *     Three squares (h2 x h2) on the second row.
         * 
         *     3x2 3x2
         *     1x1 1x1 1x1
         *     
         *     w1xh1 w1xh1
         *     h2xh2 h2xh2 h2xh2
         *     
         *     1) h = h1 + h2
         *     2) w1 = 1.5*h1         # because of 3x2 aspect ratio
         *     3) w = 2*w1
         *     4) w = 3*h2
         *     5) 2*w1 = 3*h2         # because 3 == 4
         *     6) w1 = 1.5*h2         # Simplify 5
         *     7) h2 = h1             # because 2 == 6
         *     So two rows have equal height
         *     w / h = 2*h2 / (h1 + h2) = 2*h2 / 2*h2 so square
         *     
         * CoverStyle == 3
         *    2 squares in top row.
         *    3 squares in bottom row
         *    Squares... so h1 = w1, h2 = w2
         *    
         *    h1xh1 h1xh1 h1xh1
         *    h2xh2 h2xh2
         *    
         *    2) w = 3 * h1                         # width of first row
         *    1) w = 2 * h2                         # width of second row
         *    3) 2*h2 = 3*h1                        # line 1 == line 2
         *    4) h2 = 1.5*h1
         *    So rows are 2/5*h and 3/5*h
         *    
         */

        CoverLayout? coverLayout = null;
        CoverLayout CoverLayout
            {
            get
                {
                if (coverLayout is null)
                    {
                    coverLayout = this.coverLayouts[PortfolioInfo.CoverStyle - 1];
                    }

                return coverLayout;
                }
            }
        CoverLayout[] coverLayouts = new CoverLayout[]
            {
                // Style 1
                new CoverLayout()
                    {
                    rowFirst = false,
                    thumbnailsClass = "columns-2auto",
                    groupClass = new string[3]
                        {
                        "rows-2equal",
                        "rows-3equal",
                        "hidden"
                        },
                    thumbnailAspects = new Aspect[3,3]
                        {
                            {Aspect.square, Aspect.square, Aspect.none  },   // First column
                            {Aspect.rect_3x2, Aspect.rect_3x2, Aspect.rect_3x2  }, // Second column
                            {Aspect.none, Aspect.none, Aspect.none },
                        },
                    heights = new string[3,3]
                        {
                            {"height-one-half", "height-one-half", "" },
                            {"height-one-third", "height-one-third", "height-one-third" },
                            {"", "", "" }
                        }
                    },

                // Style 2
                new CoverLayout()
                    {
                    rowFirst = true,
                    thumbnailsClass = "rows-2equal",
                    groupClass = new string[3]
                        {
                        "columns-3auto",
                        "columns-2auto",
                        "hidden"
                        },
                    thumbnailAspects = new Aspect[3,3]
                        {
                            {Aspect.square, Aspect.square, Aspect.square  },
                            {Aspect.rect_3x2, Aspect.rect_3x2, Aspect.none  },
                            {Aspect.none, Aspect.none, Aspect.none },
                        },
                    heights = new string[3,3]
                        {
                            {"height-one-half", "height-one-half", "height-one-half" },
                            {"height-one-half", "height-one-half", "" },
                            {"", "", "" }
                        }
                    },
                // Style 3
                new CoverLayout()
                    {
                    rowFirst = true,
                    thumbnailsClass = "rows-2-3",
                    groupClass = new string[3]
                        {
                        "columns-3auto",
                        "columns-2auto",
                        "hidden"
                        },
                    thumbnailAspects = new Aspect[3,3]
                        {
                            {Aspect.square, Aspect.square, Aspect.square  },
                            {Aspect.square, Aspect.square, Aspect.none  },
                            {Aspect.none, Aspect.none, Aspect.none },
                        },
                    heights = new string[3,3]
                        {
                            {"height-two-fifths", "height-two-fifths", "height-two-fifths" },
                            {"height-three-fifths", "height-three-fifths", "" },
                            {"", "", "" }
                        }
                    }
            };

        private string CoverClass
            {
            get
                {
                return CoverLayout.thumbnailsClass + " " + PortfolioInfo.CoverStyle switch
                    {
                    1 => "cover-style-1",
                    2 => "cover-style-2",
                    3 => "cover-style-3",
                    _ => ""
                    };
                }
            }
        private string ThumbnailsClass
            {
            get
                {
                return "";
                }
            }

        private string GroupClass(int group)
            {
            return CoverLayout.groupClass[group];
            }

        private string ThumbnailClass(int row, int column)
            {
            string aspect = CoverLayout.thumbnailAspects[row, column] switch
                {
                Aspect.square => "square",
                Aspect.rect_3x2 => "rect-3x2",
                _ => "hidden"
                };
            return $"thumbnail {aspect} {CoverLayout.heights[row, column]}";
            }

        private int imageIndex = 0;
        private bool ignoreClick = false;
        protected void OnClick00() { OnClick(0, 0); }
        protected void OnClick01() { OnClick(0, 1); }
        protected void OnClick02() { OnClick(0, 2); }
        protected void OnClick10() { OnClick(1, 0); }
        protected void OnClick11() { OnClick(1, 1); }
        protected void OnClick12() { OnClick(1, 2); }
        protected void OnClick20() { OnClick(2, 0); }
        protected void OnClick21() { OnClick(2, 1); }
        protected void OnClick22() { OnClick(2, 2); }
        protected void OnClick(int row, int column)
            {
            this.imageIndex = this.CoverImageIndexes[row, column];
            if (!this.ignoreClick && (this.imageIndex >= 0))
                {
                NavigationManager.NavigateTo($"ImageViewer\\{PortfolioInfo.Name}\\{this.imageIndex}");
                }
            }

        private async Task OnMouseDown(MouseEventArgs args)
            {
            await this.coverElement.FocusAsync();
            this.ignoreClick = true;
            if (args.AltKey)
                {
                this.coverLayout = null;
                PortfolioInfo.CoverStyle++;
                if (PortfolioInfo.CoverStyle > 3)
                    {
                    PortfolioInfo.CoverStyle = 1;
                    }
                StateHasChanged();
                }
            else
                {
                this.ignoreClick = false;
                }
            }
        }
    }
