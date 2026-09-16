using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FastReport.Domain.Models
{
    public class ChartComponent : ReportComponent
    {
        // نوع نمودار: Bar (میله‌ای)، Line (خطی)، Pie (دایره‌ای)
        public string ChartType { get; set; } = "Bar";

        // مسیر آرایه داده‌ها در JSON (مثلاً "SalesData")
        public string DataBindingPath { get; set; }

        // نام فیلد محور افقی (مثلاً "Month")
        public string XAxisField { get; set; }

        // نام فیلد مقادیر محور عمودی (مثلاً "Amount")
        public string YAxisField { get; set; }
    }
}
