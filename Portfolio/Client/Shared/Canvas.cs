using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Blazor.Extensions.Canvas;
using Microsoft.AspNetCore.Components;

namespace Portfolio.Client.Shared
    {
    public class Canvas : BECanvas
        {
        public ElementReference GetCanvasRef() { return this._canvasRef; }
        }
    }
