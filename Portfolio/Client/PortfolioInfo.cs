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
        private int[] imageIndexes = null;
        public int [] ImageIndexes
            {
            get
                {
                if (this.imageIndexes == null)
                    {
                    this.imageIndexes = new int [] { 0, 1, 2, 3, 4 };
                    }
                return this.imageIndexes;
                }
            set
                {
                this.imageIndexes = value;
                }
            }
        public List<String> FileNames { get; set; }
        }
    }
