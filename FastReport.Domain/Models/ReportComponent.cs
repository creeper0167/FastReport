using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FastReport.Domain.Models
{
    public abstract class ReportComponent
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = string.Empty;

        // مختصات و ابعاد
        public double X { get; set; }
        public double Y { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }

        // مسیری در فایل JSON ورودی که این المان باید از آن دیتا بخواند
        // مثال: "Invoice.Customer.Name"
        public string DataBindingPath { get; set; } = string.Empty;
    }
}
