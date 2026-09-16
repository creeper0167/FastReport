using FastReport.Domain.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace FastReport.Domain
{
    public class ReportEngine
    {
        public ReportEngine()
        {
            QuestPDF.Settings.License = LicenseType.Community;
        }
        // این همان API (متد عمومی) است که بقیه برنامه‌های دات‌نت آن را صدا می‌زنند
        public void RenderPdf(string templateFilePath, string jsonData, string outputPdfPath)
        {
            // ۱. خواندن فایل قالب (.frpt) که قبلا در محیط گرافیکی ساخته شده
            string templateJson = File.ReadAllText(templateFilePath);
            var components = JsonSerializer.Deserialize<List<ReportComponent>>(templateJson);

            if (components == null) throw new Exception("Invalid report template.");

            // ۲. پارس کردن دیتای JSON ورودی (ارسال شده توسط برنامه‌نویس دیگر)
            using JsonDocument document = JsonDocument.Parse(jsonData);
            JsonElement jsonRoot = document.RootElement;

            // ۳. تولید PDF
            Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(0);
                    page.PageColor(Colors.White);

                    page.Content().Layers(layers =>
                    {
                        layers.PrimaryLayer().Width(PageSizes.A4.Width).Height(PageSizes.A4.Height);

                        foreach (var component in components)
                        {
                            if (component is TextComponent textModel)
                            {
                                // جایگذاری متغیرها با دیتای JSON
                                string finalDisplayText = ProcessTemplateText(textModel.Text, jsonRoot);

                                layers.Layer()
                                    .TranslateX((float)textModel.X)
                                    .TranslateY((float)textModel.Y)
                                    .Width((float)textModel.Width)
                                    .Height((float)textModel.Height)
                                    .Text(text =>
                                    {
                                        text.AlignRight();
                                        text.Span(finalDisplayText)
                                            .FontFamily(textModel.FontFamily)
                                            .FontSize((float)textModel.FontSize)
                                            .FontColor(textModel.HexColor)
                                            .DirectionFromRightToLeft();
                                    });
                            }
                            else if (component is ChartComponent chartModel)
                            {
                                // ۱. استخراج داده‌های آرایه‌ای از JSON (برای رسم نمودار به یک لیست نیاز داریم)
                                List<double> values = new List<double>();
                                List<string> labels = new List<string>();

                                try
                                {
                                    // پیمایش مسیر متغیر در JSON تا رسیدن به آرایه هدف
                                    JsonElement arrayElement = jsonRoot;
                                    if (!string.IsNullOrEmpty(chartModel.DataBindingPath))
                                    {
                                        foreach (var part in chartModel.DataBindingPath.Split('.'))
                                        {
                                            arrayElement = arrayElement.GetProperty(part);
                                        }
                                    }

                                    // اگر یک آرایه معتبر در JSON یافت شد، مقادیر آن را می‌خوانیم
                                    if (arrayElement.ValueKind == JsonValueKind.Array)
                                    {
                                        foreach (var item in arrayElement.EnumerateArray())
                                        {
                                            // خواندن لیبل‌ها (مثلاً نام ماه‌ها)
                                            if (item.TryGetProperty(chartModel.XAxisField ?? "Label", out var xProp))
                                                labels.Add(xProp.ToString());

                                            // خواندن مقادیر عددی
                                            if (item.TryGetProperty(chartModel.YAxisField ?? "Value", out var yProp) &&
                                                double.TryParse(yProp.ToString(), out double val))
                                                values.Add(val);
                                        }
                                    }
                                }
                                catch
                                {
                                    // در صورت عدم وجود دیتا، یک نمودار نمونه و خالی کشیده می‌شود
                                    values = new List<double> { 10, 20, 15 };
                                    labels = new List<string> { "No", "Data", "Found" };
                                }

                                // ۲. تولید عکس نمودار در حافظه با استفاده از ScottPlot
                                var plot = new ScottPlot.Plot();

                                if (chartModel.ChartType == "Bar")
                                {
                                    var barPlot = plot.Add.Bars(values.ToArray());

                                    // ۱. تولید یک آرایه عددی برای موقعیت قرارگیری لیبل‌ها (0, 1, 2, ...)
                                    double[] tickPositions = new double[labels.Count];
                                    for (int i = 0; i < labels.Count; i++)
                                    {
                                        tickPositions[i] = i;
                                    }

                                    // ۲. ارسال هر دو پارامتر (موقعیت‌ها و متن‌ها) به محور افقی
                                    plot.Axes.Bottom.SetTicks(tickPositions, labels.ToArray());
                                }
                                else if (chartModel.ChartType == "Pie")
                                {
                                    plot.Add.Pie(values.ToArray());
                                }

                                // پنهان کردن خطوط گرید برای زیبایی بیشتر در گزارش
                                plot.HideGrid();
                                plot.FigureBackground.Color = ScottPlot.Color.FromHex("#FFFFFF");

                                // گرفتن خروجی عکس از نمودار با ابعاد مشخص شده در طراحی
                                byte[] chartImage = plot.GetImageBytes((int)chartModel.Width, (int)chartModel.Height, ScottPlot.ImageFormat.Png);

                                // ۳. قرار دادن عکس نمودار در لایه QuestPDF
                                layers.Layer()
                                    .TranslateX((float)chartModel.X)
                                    .TranslateY((float)chartModel.Y)
                                    .Width((float)chartModel.Width)
                                    .Height((float)chartModel.Height)
                                    .Image(chartImage); // تزریق بایت‌های عکس
                            }
                        }
                    });
                });
            })
            .GeneratePdf(outputPdfPath);
        }

        // متدهای کمکی برای پردازش متن (همان‌هایی که در مرحله قبل نوشتیم)
        private string ProcessTemplateText(string templateText, JsonElement jsonRoot)
        {
            if (string.IsNullOrWhiteSpace(templateText)) return "";
            return Regex.Replace(templateText, @"\{\{(.+?)\}\}", match =>
            {
                return GetValueFromJson(jsonRoot, match.Groups[1].Value.Trim());
            });
        }

        private string GetValueFromJson(JsonElement element, string path)
        {
            try
            {
                foreach (var part in path.Split('.'))
                {
                    element = element.GetProperty(part);
                }
                return element.ToString();
            }
            catch { return $"[{path} Not Found]"; }
        }
    }
}
