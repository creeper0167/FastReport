using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FastReport.Domain.Models
{
    public class TextComponent : ReportComponent
    {
        public string Text { get; set; } = "Text Element";
        public string FontFamily { get; set; } = "Arial";
        public int FontSize { get; set; } = 12;
        public string HexColor { get; set; } = "#000000";
    }
}
