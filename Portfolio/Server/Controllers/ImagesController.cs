using Microsoft.AspNetCore.Mvc;
using System;
using System.Drawing;
using System.IO;
using System.Net.Http;
using Portfolio.Shared;
using System.Text.Json;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace Portfolio.Server.Controllers
    {
    [Route("[controller]")]
    [ApiController]
    public class ImagesController : ControllerBase
        {
        private string dirName = @"E:\Users\lee\Pictures\Portfolio\Web";

        private string FullPathName(string name)
            {
            return $"{dirName}\\{name}";
            }
        private string FullPathName(string format, string name)
            {
            return $"{dirName}\\{format}\\{name}";
            }
        [HttpGet]
        [Route("{name}")]
        public IActionResult Get(string name)
            {
            switch(name) 
                {
                case "Portfolios":
                        return new JsonResult(GetPortfolioInfos());
                }
            return NotFound($"{name}");
            }

        [HttpGet]
        [Route("{format}/{name}")]
        public IActionResult Get(string format, string name)
            {
            string fileName = FullPathName(format, name);

            if (!System.IO.File.Exists(fileName))
                {
                switch (format)
                    {
                    case "full":
                        break;
                    case "low":
                        Create(format, name,
                            (Image sourceImage) => { return ScaleToFit(sourceImage, 1536, 1024); });
                        break;
                    case "1x1":
                        Create(format, name,
                            (Image sourceImage) => { return CropScaleToFit(sourceImage, 512, 512); });
                        break;
                    case "2x3":
                        Create(format, name,
                            (Image sourceImage) => { return CropScaleToFit(sourceImage, 768, 512); });
                        break;

                    case "thumbnailInfo":
                        // The client will assume biggest centered for thumbnail info.
                        return NotFound($"{format}/{name}");
                    }
                }

            if (!System.IO.File.Exists(fileName))
                return NotFound($"{format}/{name}");

            var image = System.IO.File.OpenRead(fileName);
            string contentType = (format == "thumbnailInfo") ? "text/json" : "image/jpeg";
            return File(image, contentType);
            }

        [HttpPut]
        [Route("thumbnailInfo/{name}")]
        public void Put(string name, [FromBody] ThumbnailInfo thumbnailInfo)
            {
            string fileName = FullPathName("thumbnailInfo", name);
            JsonSerializerOptions options = new() { IgnoreReadOnlyFields = true, IgnoreReadOnlyProperties = true };
            string jsonString = JsonSerializer.Serialize(thumbnailInfo, options);
            System.IO.File.WriteAllText(fileName, jsonString);
            }

        // DELETE api/<ValuesController>/5
        [HttpDelete("{name}")]
        public void Delete(string name)
            {
            }

        private void EnsureSubDirectoryExists(string dirName)
            {
            string subDirName = FullPathName(dirName);
            if (!System.IO.Directory.Exists(subDirName))
                {
                DirectoryInfo dirInfo = System.IO.Directory.CreateDirectory(subDirName);
                if (dirInfo is not null)
                    {
                    string s = dirInfo.CreationTime.ToString();
                    }
                }
            }

        private delegate Bitmap ImageCreator(Image sourceImage);
        private void Create(string format, string name, ImageCreator imageCreator)
            {
            string sourceFileName = FullPathName("full", name);
            string destinationFileName = FullPathName(format, name);
            EnsureSubDirectoryExists(format);
            if (System.IO.File.Exists(sourceFileName))
                {
                Image image = Image.FromFile(sourceFileName);
                Bitmap bitmap = imageCreator(image);
                if (bitmap is not null)
                    {
                    bitmap.Save(destinationFileName, System.Drawing.Imaging.ImageFormat.Jpeg);
                    }
                }
            }

        /*
         * Scale the width and height proportionally so that both fit within the maximums
        */
        private static Size ScaleToFit(int width, int height, int maxWidth, int maxHeight)
            {
            // First try scaling to maxHeight
            int targetWidth = (width * maxHeight) / height;
            if (targetWidth < maxWidth)
                return new Size(targetWidth, maxHeight);

            // Didn't fit, so scale to maxWidth
            return new Size(maxWidth, (height * maxWidth) / width);
            }

        private static Bitmap ScaleToFit(Image sourceImage, int maxWidth, int maxHeight)
            {
            int width = sourceImage.Width;
            int height = sourceImage.Height;

            return new Bitmap(sourceImage, ScaleToFit(width, height, maxWidth, maxHeight));
            }
        private static Bitmap CropScaleToFit(Image sourceImage, int width, int height)
            {
            // Copy as much of the sourceImage as will fill width x height when scaled to fit.

            int x = 0;
            int y = 0;
            int sourceWidth = sourceImage.Width;
            int sourceHeight = sourceImage.Height;

            // Ratio greater than one is wider than it is tall
            // Ratio equal to one is square.
            // Ratio less than one is taller than it is wide.
            // So, larger ratio is a fatter aspect ratio
            float sourceRatio = ((float)sourceWidth)/sourceHeight;
            float destinationRatio = ((float)width)/height;

            if (sourceRatio > destinationRatio)
                {
                // source is wider, crop left and right
                sourceWidth = (int)(sourceHeight/destinationRatio);
                x = (sourceImage.Width - sourceWidth)/2;
                }
            else
                {
                // sourceImage is taller, crop top and bottom
                sourceHeight = (int)(sourceWidth/destinationRatio);
                y = (sourceImage.Height - sourceHeight)/2;
                }

            Rectangle sourceRect = new Rectangle(x, y, sourceWidth, sourceHeight);
            Rectangle destinationRect = new Rectangle(0, 0, width, height);

            return CropImage(sourceImage, sourceRect, destinationRect);
            }
        static Bitmap CropImage(Image sourceImage, Rectangle sourceRect, Rectangle destinationRect)
            {
            var cropImage = new Bitmap(destinationRect.Width, destinationRect.Height);
            using (var graphics = Graphics.FromImage(cropImage))
                {
                graphics.DrawImage(sourceImage, destinationRect, sourceRect, GraphicsUnit.Pixel);
                }
            return cropImage;
            }

        private ImageMetadata[] allMetadata;
        private ImageMetadata[] GetAllMetadata()
        {
            if (this.allMetadata is null)
                {
                string json = System.IO.File.ReadAllText(this.dirName + @"\Metadata.json");
                this.allMetadata = JsonSerializer.Deserialize<ImageMetadata[]>(json);
                }
            return this.allMetadata;
        }

        private PortfolioInfo[] portfolioInfos;
        public PortfolioInfo [] GetPortfolioInfos()
            {
            if (this.portfolioInfos == null)
                {
                Dictionary<string, List<ImageMetadata>> dict = new();
                GetAllMetadata();
                if (this.allMetadata is not null)
                    {
                    foreach (ImageMetadata metadata in this.allMetadata)
                        {
                        foreach (string keyword in metadata.Keywords)
                            {
                            if ((keyword == "2x3") || (keyword == "1x1"))
                                continue;
                                   
                            List<ImageMetadata> list;
                            if (!dict.TryGetValue(keyword, out list))
                                {
                                list = new List<ImageMetadata>();
                                dict[keyword] = list;
                                }
                            list.Add(metadata);
                            }
                        }

                    SortedList<String,PortfolioInfo> portfolioInfoList = new();
                    foreach ((string key, List<ImageMetadata> metadatas) in dict)
                        {
                        PortfolioInfo portfolioInfo = new();
                        portfolioInfo.RootPath = "images";
                        portfolioInfo.Name = key;
                        portfolioInfo.FileNames = new();
                        portfolioInfo.CoverStyle = (metadatas.Count % 3) + 1;
                        foreach (ImageMetadata metadata in metadatas)
                            {
                            portfolioInfo.FileNames.Add(metadata.FileName);
                            }
                        
                        portfolioInfoList.Add(portfolioInfo.Name, portfolioInfo);
                        }
                    this.portfolioInfos = new PortfolioInfo[portfolioInfoList.Count];
                    int i = 0;
                    foreach ((string key, PortfolioInfo portfolioInfo) in portfolioInfoList)
                        {
                        this.portfolioInfos[i++] = portfolioInfo;
                        }
                    }
                }

            return this.portfolioInfos;
            }
        }
    }
