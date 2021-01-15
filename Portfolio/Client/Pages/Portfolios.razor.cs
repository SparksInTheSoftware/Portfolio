using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using Portfolio.Client.Views;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace Portfolio.Client.Pages
    {
    public partial class Portfolios : ComponentBase
        {
        [Inject] IJSRuntime JSRuntime { get; set; }
        [Inject] HttpClient HttpClient { get; set; }

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
                this.portfolioInfos = await HttpClient.GetFromJsonAsync<PortfolioInfo[]>("portfolios.json");
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
        }
    }
