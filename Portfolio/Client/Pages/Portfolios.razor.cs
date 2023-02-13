using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using Portfolio.Client.Views;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Portfolio.Shared;

namespace Portfolio.Client.Pages
    {
    public partial class Portfolios : ComponentBase
        {
        [Inject] IJSRuntime JSRuntime { get; set; }
        [Inject] HttpClient HttpClient { get; set; }
        [Inject] AppData AppData { get; set; }

        private ElementReference containerDiv;

        private PortfolioInfo[] portfolioInfos = null;
        private PortfolioInfo[] PortfolioInfos
            {
            get
                {
                if (this.portfolioInfos != null)
                    return this.portfolioInfos;

                return new PortfolioInfo[0];
                }
            }

        private List<CoverView> coverViews = new List<CoverView>();
        protected override async Task OnAfterRenderAsync(bool firstRender)
            {
            if (firstRender)
                {
                await JSRuntime.InvokeVoidAsync("RegisterWindowHandler", DotNetObjectReference.Create<Portfolios>(this));
                AppData.HttpClient = HttpClient;
                this.portfolioInfos = await AppData.GetPortfolioInfos();
                StateHasChanged();
                await this.containerDiv.FocusAsync();
                }
            }

        [JSInvokable]
        public async Task OnResize()
            {
            foreach (var coverView in this.coverViews)
                {
                await coverView.OnResize();
                }
            }

        private bool onKeyDownHandled = true;
        private bool OnKeyDownPreventDefault { get { return this.onKeyDownHandled; }  }
        private bool OnKeyDownStopPropogation { get { return this.onKeyDownHandled; }  }
        private async Task OnKeyDown(KeyboardEventArgs args)
            {
            this.onKeyDownHandled = false;
            if (args.CtrlKey)
                {
                switch (args.Key)
                    {
                    case "c":
                        this.onKeyDownHandled = true;
                        await SerializePortfolioInfos();
                        break;
                    }
                }
            else if (args.ShiftKey)
                {
                }
            else if (args.AltKey)
                {
                switch (args.Key)
                    {
                    }
                }
            else
                {
                switch (args.Key)
                    {
                    }
                }
            }

        private async Task SerializePortfolioInfos()
            {
            JsonSerializerOptions opts = new() { WriteIndented = true };
            String s = JsonSerializer.Serialize<PortfolioInfo[]>(PortfolioInfos, opts);

            await JSRuntime.InvokeVoidAsync("navigator.clipboard.writeText", s);
            }
        }
    }
