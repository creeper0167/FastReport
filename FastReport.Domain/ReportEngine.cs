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
