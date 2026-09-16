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
                                            // اندازه خانه‌های شبکه (10 پیکسل)
        private const double GridSize = 10.0;
        public MainWindow()
        {
            InitializeComponent();

            // فعال‌سازی انکودینگ برای دات‌نت کور
            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
            // اعلام لایسنس نسخه کامیونیتی (رایگان) برای QuestPDF
            QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;

        }

        private double SnapToGrid(double value)
        {
            return Math.Round(value / GridSize) * GridSize;
        }
        private void Element_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (_isDragging && _selectedElement != null)
            {
                Point currentPosition = e.GetPosition(DesignCanvas);

                // محاسبه مختصات جدید و اعمال Snap
                double newX = SnapToGrid(currentPosition.X - _clickPosition.X);
                double newY = SnapToGrid(currentPosition.Y - _clickPosition.Y);

                // جلوگیری از خروج المان از سمت چپ و بالای بوم
                newX = Math.Max(0, newX);
                newY = Math.Max(0, newY);

                Canvas.SetLeft(_selectedElement, newX);
                Canvas.SetTop(_selectedElement, newY);

                // ذخیره مختصات جدید در مدل داده‌ها
                if (_selectedElement is FrameworkElement element && element.Tag is ReportComponent model)
                {
                    model.X = newX;
                    model.Y = newY;
                }
            }
        }
        private void RemoveSelectionHandles()
        {
            if (_activeElement != null)
            {
                var layer = AdornerLayer.GetAdornerLayer(_activeElement);
                if (layer != null)
                {
                    var adorners = layer.GetAdorners(_activeElement);
                    if (adorners != null)
                    {
                        foreach (var adorner in adorners)
                        {
                            layer.Remove(adorner);
                        }
                    }
                }
            }
        }

        // 1. شروع عملیات Drag از Toolbox
        private void btnDragText_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // یک سیگنال متنی به نام TextComponent ارسال می‌کنیم تا Canvas بفهمد چه چیزی در حال کشیده شدن است
            DragDrop.DoDragDrop((DependencyObject)sender, "Text Box", DragDropEffects.Copy);
        }
        // 2. عملیات Drop روی بوم طراحی
        private void DesignCanvas_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.StringFormat))
            {
                string toolName = (string)e.Data.GetData(DataFormats.StringFormat);
                Point dropPosition = e.GetPosition(DesignCanvas);

                if (toolName == "Text Box")
                {
                    // ۱. ساخت مدل داده‌ای متن در نقطه‌ی رها شدن موس
                    var textModel = new TextComponent
                    {
                        X = SnapToGrid(dropPosition.X), // اعمال Snap
                        Y = SnapToGrid(dropPosition.Y), // اعمال Snap
                        Width = 200,
                        Height = 40,
                        Text = "متن نمونه",
                        FontSize = 14,
                        HexColor = "#000000",
                        FontFamily = "Arial"
                    };

                    _reportComponents.Add(textModel);

                    // ۲. ساخت ظاهر گرافیکی متن روی بوم
                    Border textVisual = new Border
                    {
                        Width = textModel.Width,
                        Height = textModel.Height,
                        Background = Brushes.White,
                        BorderBrush = Brushes.Gray,
                        BorderThickness = new Thickness(1),
                        Cursor = Cursors.SizeAll,
                        Tag = textModel // اتصال مدل داده‌ای به ظاهر گرافیکی
                    };

                    TextBlock textBlock = new TextBlock
                    {
                        Text = textModel.Text,
                        HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                        VerticalAlignment = System.Windows.VerticalAlignment.Center,
                        FontFamily = new FontFamily(textModel.FontFamily),
                        FontSize = textModel.FontSize
                    };

                    textVisual.Child = textBlock;

                    // ۳. قرار دادن در مختصات موس
                    Canvas.SetLeft(textVisual, textModel.X);
                    Canvas.SetTop(textVisual, textModel.Y);

                    // ۴. اتصال رویدادها (انتخاب، تغییر سایز، جابجایی)
                    textVisual.MouseLeftButtonDown += Element_MouseLeftButtonDown;
                    textVisual.MouseMove += Element_MouseMove;
                    textVisual.MouseLeftButtonUp += Element_MouseLeftButtonUp;

                    // ۵. اضافه کردن به بوم
                    DesignCanvas.Children.Add(textVisual);
                }
                else if (toolName == "Chart")
                {
                    var chartModel = new ChartComponent
                    {
                        X = SnapToGrid(dropPosition.X), // اعمال Snap
                        Y = SnapToGrid(dropPosition.Y), // اعمال Snap
                        Width = 300,
                        Height = 200,
                        ChartType = "Bar"
                    };

                    _reportComponents.Add(chartModel);

                    Border chartVisual = new Border
                    {
                        Width = chartModel.Width,
                        Height = chartModel.Height,
                        Background = Brushes.AliceBlue,
                        BorderBrush = Brushes.CornflowerBlue,
                        BorderThickness = new Thickness(2),
                        Cursor = Cursors.SizeAll,
                        Tag = chartModel
                    };

                    TextBlock label = new TextBlock
                    {
                        Text = "📊 Chart Area",
                        HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                        VerticalAlignment = System.Windows.VerticalAlignment.Center,
                        Foreground = Brushes.CornflowerBlue,
                        FontWeight = FontWeights.Bold
                    };

                    chartVisual.Child = label;

                    Canvas.SetLeft(chartVisual, chartModel.X);
                    Canvas.SetTop(chartVisual, chartModel.Y);

                    chartVisual.MouseLeftButtonDown += Element_MouseLeftButtonDown;
                    chartVisual.MouseMove += Element_MouseMove;
                    chartVisual.MouseLeftButtonUp += Element_MouseLeftButtonUp;

                    DesignCanvas.Children.Add(chartVisual);
                }
            }
        }

        // ۱. زمانی که کاربر روی المان کلیک می‌کند تا جابجایی را شروع کند
        private void Element_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            RemoveSelectionHandles();

            _selectedElement = sender as UIElement;
            _isDragging = true;

            if (_selectedElement != null)
            {
                _clickPosition = e.GetPosition(_selectedElement);
                _selectedElement.CaptureMouse();

                if (_selectedElement is Border border)
                {
                    _activeElement = border;

                    var layer = AdornerLayer.GetAdornerLayer(_activeElement);
                    if (layer != null)
                    {
                        layer.Add(new ResizingAdorner(_activeElement));
                    }

                    // ابتدا همه پنل‌ها را مخفی می‌کنیم
                    txtNoSelection.Visibility = Visibility.Collapsed;
                    pnlTextProperties.Visibility = Visibility.Collapsed;
                    pnlChartProperties.Visibility = Visibility.Collapsed;

                    _isUpdatingUI = true;

                    // اگر المان متنی بود:
                    if (border.Tag is TextComponent textModel)
                    {
                        pnlTextProperties.Visibility = Visibility.Visible;

                        txtBoxContent.Text = textModel.Text;
                        txtBoxFontSize.Text = textModel.FontSize.ToString();
                        txtBoxColor.Text = textModel.HexColor;
                        txtBoxDataBinding.Text = textModel.DataBindingPath;
                    }
                    // اگر المان نمودار بود:
                    else if (border.Tag is ChartComponent chartModel)
                    {
                        pnlChartProperties.Visibility = Visibility.Visible;

                        cmbChartType.Text = chartModel.ChartType;
                        txtChartDataPath.Text = chartModel.DataBindingPath;
                        txtChartXAxis.Text = chartModel.XAxisField;
                        txtChartYAxis.Text = chartModel.YAxisField;
                    }

                    _isUpdatingUI = false;
                }
            }

            e.Handled = true;
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

                        // ۲. مخفی کردن پنل‌های تنظیمات جدید
                        pnlTextProperties.Visibility = Visibility.Collapsed;
                        pnlChartProperties.Visibility = Visibility.Collapsed;
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

                                TextBlock textBlock = new TextBlock
                                {
                                    Text = textModel.Text,
                                    HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                                    VerticalAlignment = System.Windows.VerticalAlignment.Center,
                                    FontFamily = new FontFamily(textModel.FontFamily),
                                    FontSize = textModel.FontSize
                                };

                                try
                                {
                                    textBlock.Foreground = (Brush)new BrushConverter().ConvertFromString(textModel.HexColor);
                                }
                                catch { }

                                visualElement.Child = textBlock;

                                Canvas.SetLeft(visualElement, textModel.X);
                                Canvas.SetTop(visualElement, textModel.Y);

                                visualElement.MouseLeftButtonDown += Element_MouseLeftButtonDown;
                                visualElement.MouseMove += Element_MouseMove;
                                visualElement.MouseLeftButtonUp += Element_MouseLeftButtonUp;

                                DesignCanvas.Children.Add(visualElement);
                            }
                            else if (component is ChartComponent chartModel)
                            {
                                // بازسازی بخش نمودار روی بوم
                                Border chartVisual = new Border
                                {
                                    Width = chartModel.Width,
                                    Height = chartModel.Height,
                                    Background = Brushes.AliceBlue,
                                    BorderBrush = Brushes.CornflowerBlue,
                                    BorderThickness = new Thickness(2),
                                    Cursor = Cursors.SizeAll,
                                    Tag = chartModel
                                };

                                TextBlock label = new TextBlock
                                {
                                    Text = "📊 Chart Area",
                                    HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                                    VerticalAlignment = System.Windows.VerticalAlignment.Center,
                                    Foreground = Brushes.CornflowerBlue,
                                    FontWeight = FontWeights.Bold
                                };

                                chartVisual.Child = label;

                                Canvas.SetLeft(chartVisual, chartModel.X);
                                Canvas.SetTop(chartVisual, chartModel.Y);

                                chartVisual.MouseLeftButtonDown += Element_MouseLeftButtonDown;
                                chartVisual.MouseMove += Element_MouseMove;
                                chartVisual.MouseLeftButtonUp += Element_MouseLeftButtonUp;

                                DesignCanvas.Children.Add(chartVisual);
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
                RemoveSelectionHandles();

                _activeElement = null;
                pnlTextProperties.Visibility = Visibility.Collapsed;
                pnlChartProperties.Visibility = Visibility.Collapsed;
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
                    // ضریب تبدیل پیکسل‌های WPF (96 DPI) به پوینت‌های PDF (72 DPI)
                    const float scale = 0.75f;

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
                                // ایجاد یک لایه اصلی و نامرئی به ابعاد صفحه A4 تا بوم ما شکل بگیرد
                                layers.PrimaryLayer().Width(PageSizes.A4.Width).Height(PageSizes.A4.Height);

                                // رسم المان‌های گزارش روی لایه‌های رویی
                                foreach (var component in _reportComponents)
                                {
                                    if (component is TextComponent textModel)
                                    {
                                        layers.Layer()
                                            .TranslateX((float)(textModel.X * scale))     // اعمال ضریب
                                            .TranslateY((float)(textModel.Y * scale))     // اعمال ضریب
                                            .Width((float)(textModel.Width * scale))      // اعمال ضریب
                                            .Height((float)(textModel.Height * scale))    // اعمال ضریب
                                            .Text(text =>
                                            {
                                                // اعمال تراز متن بر اساس انتخاب کاربر (اگر در مرحله قبل اضافه کردید)
                                                if (textModel.TextAlignment == "Left") text.AlignLeft();
                                                else if (textModel.TextAlignment == "Right") text.AlignRight();
                                                else text.AlignCenter();

                                                text.Span(textModel.Text)
                                                    .FontFamily(textModel.FontFamily)
                                                    .FontSize((float)(textModel.FontSize * scale)) // اعمال ضریب به سایز فونت
                                                    .FontColor(textModel.HexColor)
                                                    .DirectionFromRightToLeft();
                                            });
                                    }
                                    // بخش مربوط به Chart را هم اگر در این خروجی دارید، باید مقادیرش را ضرب در scale کنید
                                }
                            });
                        });
                    })
                    .GeneratePdf(saveFileDialog.FileName); // خروجی گرفتن و ذخیره فایل

                    MessageBox.Show("PDF exported successfully with exact coordinates!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error exporting PDF: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
        private void RibbonButton_AddChart_Click(object sender, RoutedEventArgs e)
        {
            var chartModel = new ChartComponent
            {
                X = 50,
                Y = 50,
                Width = 300,
                Height = 200,
                ChartType = "Bar"
            };
            _reportComponents.Add(chartModel);

            // ساخت یک ظاهر بصری (پیش‌نمایش) برای نمودار روی بوم طراح
            Border chartVisual = new Border
            {
                Width = chartModel.Width,
                Height = chartModel.Height,
                Background = Brushes.AliceBlue,
                BorderBrush = Brushes.CornflowerBlue,
                BorderThickness = new Thickness(2),
                Cursor = Cursors.SizeAll,
                Tag = chartModel // اتصال به مدل
            };

            TextBlock label = new TextBlock
            {
                Text = "📊 Chart Area",
                HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                VerticalAlignment = System.Windows.VerticalAlignment.Center,
                Foreground = Brushes.CornflowerBlue,
                FontWeight = FontWeights.Bold
            };

            chartVisual.Child = label;

            // اتصال رویداد کلیک برای انتخاب المان (مثل تکست‌باکس‌ها)
            chartVisual.MouseLeftButtonDown += Element_MouseLeftButtonDown;

            Canvas.SetLeft(chartVisual, chartModel.X);
            Canvas.SetTop(chartVisual, chartModel.Y);
            DesignCanvas.Children.Add(chartVisual);
        }
        private void cmbChartType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isUpdatingUI || _activeElement == null) return;

            if (_activeElement.Tag is ChartComponent chartModel && cmbChartType.SelectedItem is ComboBoxItem item)
            {
                chartModel.ChartType = item.Content.ToString();
            }
        }

        private void txtChartDataPath_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isUpdatingUI || _activeElement == null) return;

            if (_activeElement.Tag is ChartComponent chartModel)
            {
                chartModel.DataBindingPath = txtChartDataPath.Text;
            }
        }

        private void txtChartXAxis_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isUpdatingUI || _activeElement == null) return;

            if (_activeElement.Tag is ChartComponent chartModel)
            {
                chartModel.XAxisField = txtChartXAxis.Text;
            }
        }

        private void txtChartYAxis_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isUpdatingUI || _activeElement == null) return;

            if (_activeElement.Tag is ChartComponent chartModel)
            {
                chartModel.YAxisField = txtChartYAxis.Text;
            }
        }
        private void btnDragChart_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // ارسال رشته "Chart" هنگام کشیدن موس برای ساخته شدن نمودار روی بوم
            DragDrop.DoDragDrop((DependencyObject)sender, "Chart", DragDropEffects.Copy);
        }
        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            // بررسی فشرده شدن کلید Delete
            if (e.Key == Key.Delete)
            {
                // جلوگیری از تداخل: اگر کاربر در حال تایپ یا پاک کردن متن در پنل تنظیمات است، عملیات متوقف شود
                if (e.OriginalSource is TextBox) return;

                // اگر المانی روی بوم انتخاب شده است
                if (_activeElement != null)
                {
                    // ۱. پاک کردن دستگیره‌های تغییر سایز
                    RemoveSelectionHandles();

                    // ۲. پیدا کردن مدل مربوطه و حذف آن از لیست حافظه (تا در خروجی PDF و Save نیاید)
                    if (_activeElement.Tag is ReportComponent model)
                    {
                        _reportComponents.Remove(model);
                    }

                    // ۳. حذف ظاهر گرافیکی المان از روی بوم
                    DesignCanvas.Children.Remove(_activeElement);

                    // ۴. پاک‌سازی وضعیت انتخاب و مخفی کردن پنل‌های تنظیمات
                    _activeElement = null;
                    pnlTextProperties.Visibility = Visibility.Collapsed;
                    pnlChartProperties.Visibility = Visibility.Collapsed;
                    txtNoSelection.Visibility = Visibility.Visible;
                }
            }
        }
    }
}