using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Portfolio.Client
    {
    public class PortfolioInfo
        {
        public String Name { get; set; }
        public String RootPath { get; set; }
        public int CoverStyle { get; set; }
        public List<String> FileNames { get; set; }
        }
    }
