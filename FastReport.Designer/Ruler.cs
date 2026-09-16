using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace FastReport.Designer
{
    public class Ruler : Control
    {
        // تعیین افقی یا عمودی بودن خط‌کش
        public Orientation Orientation { get; set; } = Orientation.Horizontal;

        protected override void OnRender(DrawingContext dc)
        {
            base.OnRender(dc);

            double width = ActualWidth;
            double height = ActualHeight;

            Pen pen = new Pen(Brushes.DarkGray, 1);
            Typeface typeface = new Typeface("Segoe UI");
            double dpi = VisualTreeHelper.GetDpi(this).PixelsPerDip;

            // رنگ پس‌زمینه خط‌کش
            dc.DrawRectangle(Brushes.WhiteSmoke, null, new Rect(0, 0, width, height));

            double max = Orientation == Orientation.Horizontal ? width : height;

            // رسم خطوط و اعداد (هر 10 پیکسل یک خط کوچک، هر 50 پیکسل متوسط، هر 100 پیکسل بزرگ + عدد)
            for (int i = 0; i < max; i += 10)
            {
                double tickLength = 5;

                if (i % 100 == 0)
                {
                    tickLength = 15;

                    FormattedText text = new FormattedText(
                        i.ToString(),
                        CultureInfo.InvariantCulture,
                        FlowDirection.LeftToRight,
                        typeface,
                        9,
                        Brushes.DimGray,
                        dpi);

                    if (Orientation == Orientation.Horizontal)
                        dc.DrawText(text, new Point(i + 3, 0));
                    else
                        dc.DrawText(text, new Point(2, i + 3)); // تنظیم محل متن در خط‌کش عمودی
                }
                else if (i % 50 == 0)
                {
                    tickLength = 10;
                }

                if (Orientation == Orientation.Horizontal)
                    dc.DrawLine(pen, new Point(i, height - tickLength), new Point(i, height));
                else
                    dc.DrawLine(pen, new Point(width - tickLength, i), new Point(width, i));
            }

            // رسم خط حاشیه دور خط‌کش
            if (Orientation == Orientation.Horizontal)
                dc.DrawLine(pen, new Point(0, height), new Point(width, height));
            else
                dc.DrawLine(pen, new Point(width, 0), new Point(width, height));
        }
    }
}
