using FastReport.Domain.Models;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace FastReport.Designer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private List<ReportComponent> _reportComponents = new List<ReportComponent>();
        public MainWindow()
        {
            InitializeComponent();
        }
        // 1. شروع عملیات Drag از Toolbox
        private void btnDragText_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // یک سیگنال متنی به نام TextComponent ارسال می‌کنیم تا Canvas بفهمد چه چیزی در حال کشیده شدن است
            DragDrop.DoDragDrop(btnDragText, "TextComponent", DragDropEffects.Copy);
        }
        // 2. عملیات Drop روی بوم طراحی
        private void DesignCanvas_Drop(object sender, DragEventArgs e)
        {
            // بررسی می‌کنیم که آیا چیزی که رها شده، همان سیگنال متنی ماست؟
            if (e.Data.GetDataPresent(DataFormats.StringFormat))
            {
                string componentType = (string)e.Data.GetData(DataFormats.StringFormat);

                if (componentType == "TextComponent")
                {
                    // گرفتن مختصات دقیق موس روی Canvas
                    Point dropPosition = e.GetPosition(DesignCanvas);

                    // بخش اول: ساخت مدل داده‌ای (Domain)
                    var textModel = new TextComponent
                    {
                        X = dropPosition.X,
                        Y = dropPosition.Y,
                        Width = 150,
                        Height = 40,
                        Text = "Sample Text"
                    };
                    _reportComponents.Add(textModel); // اضافه کردن به حافظه برای ذخیره نهایی

                    // بخش دوم: ساخت عنصر گرافیکی (UI) برای نمایش به کاربر
                    Border visualElement = new Border
                    {
                        Width = textModel.Width,
                        Height = textModel.Height,
                        Background = Brushes.White,
                        BorderBrush = Brushes.Gray,
                        BorderThickness = new Thickness(1, 1, 1, 1),
                        Cursor = Cursors.SizeAll // تغییر شکل موس برای نشان دادن قابلیت جابجایی
                    };

                    TextBlock textBlock = new TextBlock
                    {
                        Text = textModel.Text,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center,
                        FontFamily = new FontFamily(textModel.FontFamily),
                        FontSize = textModel.FontSize
                    };

                    visualElement.Child = textBlock;

                    // قرار دادن عنصر گرافیکی در مختصاتی که موس رها شده است
                    Canvas.SetLeft(visualElement, textModel.X);
                    Canvas.SetTop(visualElement, textModel.Y);

                    // اضافه کردن عنصر به بوم
                    DesignCanvas.Children.Add(visualElement);
                }
            }
        }
    }
}