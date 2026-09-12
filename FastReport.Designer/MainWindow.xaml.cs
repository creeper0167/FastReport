using FastReport.Domain.Models;
using Microsoft.Win32;
using PdfSharp.Drawing;
using PdfSharp.Fonts;
using PdfSharp.Pdf;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace FastReport.Designer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private List<ReportComponent> _reportComponents = new List<ReportComponent>();
        // متغیرهای کنترل جابجایی المان‌ها روی بوم
        private bool _isDragging = false;
        private Point _clickPosition;
        private UIElement _selectedElement = null;
        private Border _activeElement = null; // المانی که در حال حاضر انتخاب شده است
        private bool _isUpdatingUI = false; // جلوگیری از تداخل رویدادها
        public MainWindow()
        {
            InitializeComponent();

            // فعال‌سازی انکودینگ برای دات‌نت کور
            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
            // اعلام لایسنس نسخه کامیونیتی (رایگان) برای QuestPDF
            QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;

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
                        Cursor = Cursors.SizeAll, // تغییر شکل موس برای نشان دادن قابلیت جابجایی
                        Tag = textModel
                    };

                    TextBlock textBlock = new TextBlock
                    {
                        Text = textModel.Text,
                        HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                        VerticalAlignment = System.Windows.VerticalAlignment.Center,
                        FontFamily = new FontFamily(textModel.FontFamily),
                        FontSize = textModel.FontSize
                    };

                    visualElement.Child = textBlock;

                    // قرار دادن عنصر گرافیکی در مختصاتی که موس رها شده است
                    Canvas.SetLeft(visualElement, textModel.X);
                    Canvas.SetTop(visualElement, textModel.Y);

                    // اضافه کردن رویدادهای جابجایی به المان
                    visualElement.MouseLeftButtonDown += Element_MouseLeftButtonDown;
                    visualElement.MouseMove += Element_MouseMove;
                    visualElement.MouseLeftButtonUp += Element_MouseLeftButtonUp;

                    DesignCanvas.Children.Add(visualElement);
                }
            }
        }

        // ۱. زمانی که کاربر روی المان کلیک می‌کند تا جابجایی را شروع کند
        private void Element_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _selectedElement = sender as UIElement;
            _isDragging = true;

            if (_selectedElement != null)
            {
                _clickPosition = e.GetPosition(_selectedElement);
                _selectedElement.CaptureMouse();

                // بررسی می‌کنیم که آیا المان کلیک شده یک Border است و آیا Tag آن از نوع TextComponent است
                if (_selectedElement is Border border && border.Tag is TextComponent model)
                {
                    _activeElement = border;

                    // مخفی کردن متن راهنما و نمایش پنل تنظیمات
                    txtNoSelection.Visibility = Visibility.Collapsed;
                    pnlProperties.Visibility = Visibility.Visible;

                    // روشن کردن قفل برای جلوگیری از اجرای حلقه TextChanged
                    _isUpdatingUI = true;

                    // پر کردن فیلدها با اطلاعات مدل
                    txtBoxContent.Text = model.Text;
                    txtBoxFontSize.Text = model.FontSize.ToString();
                    txtBoxColor.Text = model.HexColor;
                    txtBoxDataBinding.Text = model.DataBindingPath;

                    // باز کردن قفل
                    _isUpdatingUI = false;
                }
                else
                {
                    // اگر این پیام را دیدید، یعنی ارتباط بین گرافیک و کلاس Model قطع شده است
                    MessageBox.Show("Element selected, but it is not a valid TextComponent!");
                }
            }

            // جلوگیری از انتقال کلیک به پس‌زمینه
            e.Handled = true;
        }
        // ۲. زمانی که کاربر موس را حرکت می‌دهد
        private void Element_MouseMove(object sender, MouseEventArgs e)
        {
            if (_isDragging && _selectedElement != null)
            {
                // گرفتن موقعیت فعلی موس نسبت به بوم طراحی
                Point mousePos = e.GetPosition(DesignCanvas);

                // محاسبه مختصات جدید (موقعیت موس منهای فاصله‌ای که از گوشه المان کلیک شده بود)
                double newLeft = mousePos.X - _clickPosition.X;
                double newTop = mousePos.Y - _clickPosition.Y;

                // اعمال مختصات جدید به المان گرافیکی
                Canvas.SetLeft(_selectedElement, newLeft);
                Canvas.SetTop(_selectedElement, newTop);

                // بروزرسانی مختصات در مدل داده‌ای (Domain Model)
                if (_selectedElement is FrameworkElement fe && fe.Tag is ReportComponent model)
                {
                    model.X = newLeft;
                    model.Y = newTop;
                }
            }
        }

        // ۳. زمانی که کاربر کلیک را رها می‌کند
        private void Element_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (_isDragging)
            {
                _isDragging = false;
                _selectedElement.ReleaseMouseCapture(); // آزاد کردن قفل موس
                _selectedElement = null;
            }
        }

        // ۱. تغییر متن المان
        private void txtBoxContent_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isUpdatingUI || _activeElement == null) return;

            if (_activeElement.Tag is TextComponent model)
            {
                model.Text = txtBoxContent.Text;
                if (_activeElement.Child is TextBlock tb)
                {
                    tb.Text = model.Text;
                }
            }
        }

        // ۲. تغییر سایز فونت
        private void txtBoxFontSize_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isUpdatingUI || _activeElement == null) return;

            if (_activeElement.Tag is TextComponent model)
            {
                if (double.TryParse(txtBoxFontSize.Text, out double size))
                {
                    model.FontSize = (int)size;
                    if (_activeElement.Child is TextBlock tb)
                    {
                        tb.FontSize = size;
                    }
                }
            }
        }

        // ۳. تغییر رنگ متن
        private void txtBoxColor_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isUpdatingUI || _activeElement == null) return;

            if (_activeElement.Tag is TextComponent model)
            {
                string colorHex = txtBoxColor.Text;
                model.HexColor = colorHex;

                try
                {
                    var brush = (Brush)new BrushConverter().ConvertFromString(colorHex);
                    if (_activeElement.Child is TextBlock tb)
                    {
                        tb.Foreground = brush;
                    }
                }
                catch
                {
                    // در صورتی که کاربر در حال تایپ کد رنگ است و هنوز کامل نشده، کرش نمی‌کند
                }
            }
        }

        // متد ذخیره فایل (Save)
        private void MenuItem_Save_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "Fast Report Template (*.frpt)|*.frpt";
            saveFileDialog.DefaultExt = ".frpt";
            saveFileDialog.Title = "Save Report Template";

            if (saveFileDialog.ShowDialog() == true)
            {
                try
                {
                    var options = new JsonSerializerOptions { WriteIndented = true };
                    string jsonString = JsonSerializer.Serialize(_reportComponents, options);
                    File.WriteAllText(saveFileDialog.FileName, jsonString);

                    MessageBox.Show("Report saved successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error saving file: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        // متد باز کردن فایل (Open)
        private void MenuItem_Open_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Fast Report Template (*.frpt)|*.frpt";
            openFileDialog.Title = "Open Report Template";

            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    string jsonString = File.ReadAllText(openFileDialog.FileName);
                    var loadedComponents = JsonSerializer.Deserialize<List<ReportComponent>>(jsonString);

                    if (loadedComponents != null)
                    {
                        // ۱. پاک کردن بوم فعلی و حافظه سیستم
                        _reportComponents.Clear();
                        DesignCanvas.Children.Clear();

                        // ۲. مخفی کردن پنل تنظیمات
                        pnlProperties.Visibility = Visibility.Collapsed;
                        txtNoSelection.Visibility = Visibility.Visible;
                        _activeElement = null;

                        // ۳. خواندن اطلاعات از فایل و ساخت مجدد المان‌های گرافیکی روی بوم
                        foreach (var component in loadedComponents)
                        {
                            _reportComponents.Add(component);

                            if (component is TextComponent textModel)
                            {
                                // بازسازی مستطیل دور متن
                                Border visualElement = new Border
                                {
                                    Width = textModel.Width,
                                    Height = textModel.Height,
                                    Background = Brushes.White,
                                    BorderBrush = Brushes.Gray,
                                    BorderThickness = new Thickness(1),
                                    Cursor = Cursors.SizeAll,
                                    Tag = textModel // اتصال مجدد مدل داده‌ای
                                };

                                // بازسازی متن
                                TextBlock textBlock = new TextBlock
                                {
                                    Text = textModel.Text,
                                    HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                                    VerticalAlignment = System.Windows.VerticalAlignment.Center,
                                    FontFamily = new FontFamily(textModel.FontFamily),
                                    FontSize = textModel.FontSize
                                };

                                // تلاش برای اعمال رنگ قبلی
                                try
                                {
                                    textBlock.Foreground = (Brush)new BrushConverter().ConvertFromString(textModel.HexColor);
                                }
                                catch { }

                                visualElement.Child = textBlock;

                                // تنظیم مختصات ذخیره شده
                                Canvas.SetLeft(visualElement, textModel.X);
                                Canvas.SetTop(visualElement, textModel.Y);

                                // متصل کردن مجدد رویدادهای موس تا دوباره قابل جابجایی و کلیک باشند
                                visualElement.MouseLeftButtonDown += Element_MouseLeftButtonDown;
                                visualElement.MouseMove += Element_MouseMove;
                                visualElement.MouseLeftButtonUp += Element_MouseLeftButtonUp;

                                DesignCanvas.Children.Add(visualElement);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error opening file: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
        private void DesignCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // اگر کاربر مستقیماً روی فضای خالی بوم کلیک کرد
            if (e.OriginalSource == DesignCanvas)
            {
                _activeElement = null;
                pnlProperties.Visibility = Visibility.Collapsed;
                txtNoSelection.Visibility = Visibility.Visible;
            }
        }
        private void MenuItem_ExportPdf_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "PDF Document (*.pdf)|*.pdf";
            saveFileDialog.DefaultExt = ".pdf";
            saveFileDialog.Title = "Export Report to PDF";

            if (saveFileDialog.ShowDialog() == true)
            {
                try
                {
                    // تولید سند PDF با استفاده از QuestPDF
                    Document.Create(container =>
                    {
                        container.Page(page =>
                        {
                            // تنظیمات صفحه (A4 استاندارد)
                            page.Size(PageSizes.A4);
                            page.Margin(0);
                            page.PageColor(QuestPDF.Helpers.Colors.White);

                            // استفاده از Layers برای پیاده‌سازی سیستم مختصات مطلق (Absolute Positioning)
                            page.Content().Layers(layers =>
                            {
                                // حل خطا: ایجاد یک لایه اصلی و نامرئی به ابعاد صفحه A4 تا بوم ما شکل بگیرد
                                layers.PrimaryLayer().Width(PageSizes.A4.Width).Height(PageSizes.A4.Height);

                                // رسم المان‌های گزارش روی لایه‌های رویی
                                foreach (var component in _reportComponents)
                                {
                                    if (component is TextComponent textModel)
                                    {
                                        layers.Layer()
                                            .TranslateX((float)textModel.X)
                                            .TranslateY((float)textModel.Y)
                                            .Width((float)textModel.Width)
                                            .Height((float)textModel.Height)
                                            .Text(text =>
                                            {
                                                text.AlignRight();

                                                text.Span(textModel.Text)
                                                    .FontFamily(textModel.FontFamily)
                                                    .FontSize((float)textModel.FontSize)
                                                    .FontColor(textModel.HexColor)
                                                    .DirectionFromRightToLeft();
                                            });
                                    }
                                }
                            });
                        });
                    })
                    .GeneratePdf(saveFileDialog.FileName); // خروجی گرفتن و ذخیره فایل

                    MessageBox.Show("PDF exported successfully with QuestPDF!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error exporting PDF: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
}