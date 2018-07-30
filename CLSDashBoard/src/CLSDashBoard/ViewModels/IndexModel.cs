using LPT.CLS.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CLSDashBoard.ViewModels
{
    public class AreaSortingRecords
    {
        public int UserNo { get; set; }
        public DateTime Time { get; set; }
        public string GarbageTypeName { get; set; }
        public float Weight { get; set; }
        public int BP { get; set; }
    }
}
