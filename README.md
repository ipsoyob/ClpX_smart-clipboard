# ClipX Ultimate (ClpX)

A modern, high-performance, and deeply customized clipboard manager built on Windows Forms (.NET). Inspired by the classic ClipX, this version features a completely redesigned dark user interface, advanced search mechanics, and robust asynchronous multi-threaded data handling.

## ✨ Features

* **Advanced Search History**: Instant fuzzy search across your clipboard history with dynamic result filtering.
* **Smart UI Threading**: Fully custom-built UI controls designed programmatically (no heavy visual designers) with built-in thread safety (`InvokeRequired`) to prevent deadlocks and window freezing.
* **Content Categorization**: Separate tabs for filtering all items, pure text, or images/thumbnails.
* **Performance-Oriented**: Thread-safe database operations utilizing `lock` mechanisms to handle rapid clipboard updates without overhead.
* **Localization Support**: Built-in multi-language architecture managed via a centralized `LanguageManager`.
* **Custom UI Modals**: Fully custom-tailored dark dialog boxes matching the application's minimalist aesthetic.

## 🛠️ Tech Stack

* **Language**: C#
* **Framework**: .NET Framework 4.8+ / .NET 6.0+ (Windows Forms)
* **Architecture**: Multi-threaded Win32 API clipboard hooking, asynchronous Task-based filtering.

## 🚀 Getting Started

### Prerequisites
* Visual Studio 2022 or newer
* .NET SDK installed

### Installation & Build
1. Clone the repository:
   ```bash
   git clone https://github.com
   ```
2. Open `ClpX.sln` in Visual Studio.
3. Restore NuGet packages (if any).
4. Press `Ctrl + Shift + B` to build the project.
5. Run the application.

## 📝 License

This project is open-source. Feel free to fork, modify, and improve it!
