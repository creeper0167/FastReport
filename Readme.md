# FastReport 📄

![NuGet Version](https://img.shields.io/nuget/v/FastReportEngine)
![License](https://img.shields.io/badge/license-MIT-blue.svg)
![.NET](https://img.shields.io/badge/.NET-8.0-purple.svg)

**FastReport** is a modern, decoupled PDF report generation tool for the .NET ecosystem. It separates the visual design process from the rendering logic, allowing developers to create report templates visually and render them dynamically in any .NET application using JSON data.

Powered by [QuestPDF](https://www.questpdf.com/), FastReport ensures high-performance, vector-based PDF generation with native support for Complex Text Layouts (CTL).

## ✨ Features

- **Visual Report Designer:** A standalone WPF desktop application with drag-and-drop capabilities to visually design reports and save them as `.frpt` (JSON-based) templates.
- **Dynamic JSON Data Binding:** Inject dynamic data into your reports using double-brace template syntax (e.g., `{{Customer.FullName}}`).
- **Cross-Platform Rendering Engine:** The core API is a pure .NET library, ready to be integrated into ASP.NET Core Web APIs, Worker Services, or Console applications on Windows, Linux, and macOS.
- **Native RTL & Persian/Arabic Support:** Flawless rendering of right-to-left languages with proper text shaping and connected characters out of the box.
- **No HTML-to-PDF Conversion:** Directly generates optimized, selectable, and print-ready PDF files.

## 🚀 Installation

Install the rendering engine into your project via the NuGet Package Manager:

```bash
dotnet add package FastReportEngine
