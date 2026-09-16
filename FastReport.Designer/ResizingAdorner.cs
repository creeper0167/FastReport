using FastReport.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;

namespace FastReport.Designer
{
    public class ResizingAdorner : Adorner
    {
        private VisualCollection _visualChildren;
        private Thumb _bottomRight;

        public ResizingAdorner(UIElement adornedElement) : base(adornedElement)
        {
            _visualChildren = new VisualCollection(this);

            // ساخت یک دستگیره (Thumb) برای گوشه پایین سمت راست
            _bottomRight = new Thumb
            {
                Cursor = Cursors.SizeNWSE, // شکل موس به حالت تغییر سایز مورب در می‌آید
                Width = 10,
                Height = 10
            };

            // استایل‌دهی به دستگیره تا شکل مربع سفید با حاشیه آبی داشته باشد (ظاهر حرفه‌ای)
            ControlTemplate template = new ControlTemplate(typeof(Thumb));
            FrameworkElementFactory border = new FrameworkElementFactory(typeof(Border));
            border.SetValue(Border.BackgroundProperty, Brushes.White);
            border.SetValue(Border.BorderBrushProperty, Brushes.DodgerBlue);
            border.SetValue(Border.BorderThicknessProperty, new Thickness(1.5));
            template.VisualTree = border;
            _bottomRight.Template = template;

            // متصل کردن رویداد کشیدن موس روی دستگیره
            _bottomRight.DragDelta += BottomRight_DragDelta;
            _visualChildren.Add(_bottomRight);
        }

        private void BottomRight_DragDelta(object sender, DragDeltaEventArgs e)
        {
            if (AdornedElement is FrameworkElement adornedElement)
            {
                // اعمال Snap هنگام تغییر عرض و ارتفاع
                double newWidth = Math.Round(Math.Max(adornedElement.Width + e.HorizontalChange, 20) / 10.0) * 10.0;
                double newHeight = Math.Round(Math.Max(adornedElement.Height + e.VerticalChange, 20) / 10.0) * 10.0;

                adornedElement.Width = newWidth;
                adornedElement.Height = newHeight;

                if (adornedElement.Tag is ReportComponent model)
                {
                    model.Width = newWidth;
                    model.Height = newHeight;
                }
            }
        }

        // --- تنظیمات داخلی WPF برای رسم دستگیره ---
        protected override int VisualChildrenCount => _visualChildren.Count;
        protected override Visual GetVisualChild(int index) => _visualChildren[index];

        protected override Size ArrangeOverride(Size finalSize)
        {
            // قرار دادن دستگیره دقیقاً در گوشه پایین سمت راست المان
            _bottomRight.Arrange(new Rect(AdornedElement.DesiredSize.Width - 5, AdornedElement.DesiredSize.Height - 5, 10, 10));
            return finalSize;
        }
    }
}
