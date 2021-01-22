using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Web;

namespace Portfolio.Client
    {
    public class AppData
        {
        public HttpClient HttpClient { get; set; }

        public bool HD { get; set; } = false;

        private PortfolioInfo[] portfolioInfos;
        public async Task<PortfolioInfo []> GetPortfolioInfos()
            {
            if (this.portfolioInfos == null)
                {
                this.portfolioInfos = await HttpClient.GetFromJsonAsync<PortfolioInfo[]>("portfolios.json");
                }

            return this.portfolioInfos;
            }
        public async Task<PortfolioInfo> GetPortfolioInfo(String name)
            {
            name = HttpUtility.UrlDecode(name);
            PortfolioInfo[] portfolioInfos = await GetPortfolioInfos();

            foreach (PortfolioInfo portfolioInfo in portfolioInfos)
                {
                if (portfolioInfo.Name == name)
                    {
                    return portfolioInfo;
                    }
                }

            return null;
            }
        }
    }
