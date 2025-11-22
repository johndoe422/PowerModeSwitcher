# Power Modes Switcher

A lightweight Windows system tray utility for quickly switching between power plans with real-time CPU frequency monitoring.

![.NET Framework 4.8](https://img.shields.io/badge/.NET%20Framework-4.8-blue)
![Windows](https://img.shields.io/badge/platform-Windows-lightgrey)
![License](https://img.shields.io/badge/license-MIT-green)

## 🎯 Why Power Modes Switcher?

Windows 11 makes it unnecessarily difficult to switch between power plans:
- No quick access from the system tray
- Requires opening Settings or Control Panel
- Multiple clicks through nested menus
- No at-a-glance view of active power plan

**Power Modes Switcher** solves this with a single click from your system tray, giving you instant access to all your power plans.

## ✨ Features

### 🔌 Instant Power Plan Switching
- **System Tray Access**: Right-click the tray icon to see all available power plans
- **Visual Feedback**: Active power plan is clearly marked with a checkmark
- **One-Click Switch**: Change power plans instantly from the context menu
- **Tooltip Display**: Hover over the tray icon to see the current active plan

### 📊 Real-Time CPU Monitoring
- **Live CPU Speed Display**: Always-visible overlay showing current CPU frequency
- **Running Average**: 1-minute rolling average for stable performance insights
- **Compact Design**: Semi-transparent, dark-themed overlay positioned near system tray
- **Auto-Update**: Refreshes every second with latest CPU metrics

### 🎨 User Experience
- **Minimal UI**: Runs silently in the system tray
- **Smart Cooldown**: 1-second delay between power plan changes to prevent accidental rapid switching
- **Persistent Overlay**: CPU speed display stays visible even when main window is hidden
- **Clean Design**: Modern, unobtrusive interface that blends with Windows 11

## 🚀 Getting Started

### Prerequisites
- Windows 10/11
- .NET Framework 4.8 or higher

### Installation
1. Download the latest release from [Releases](../../releases)
2. Extract the ZIP file to your preferred location
3. Run `PowerModes.exe`
4. The application will start minimized to the system tray

### Usage

#### Switching Power Plans
1. **Right-click** the system tray icon
2. Select your desired power plan from the menu
3. The change takes effect immediately

#### Viewing CPU Speed
- The CPU speed overlay appears automatically near your system tray
- Shows current frequency and 1-minute running average
- Updates every second

#### Opening the Main Window
- **Double-click** the system tray icon, or
- **Right-click** and select "Open"

#### Exiting the Application
- Right-click the system tray icon
- Select "Exit"

## 🔧 Building from Source

### Requirements
- Visual Studio 2017 or later
- .NET Framework 4.8 SDK

### Build Steps
```bash
# Clone the repository
git clone https://github.com/yourusername/PowerModes.git

# Open the solution
cd PowerModes
start PowerModes.sln

# Build using Visual Studio or command line
msbuild PowerModes.sln /p:Configuration=Release
```
## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

### Areas for Contribution
- Additional power plan metadata display
- Keyboard shortcuts for quick switching
- Custom power plan profiles
- Multi-monitor positioning options
- Localization/translations

## 📋 System Requirements

- **OS**: Windows 10 version 1809 or later, Windows 11
- **Runtime**: .NET Framework 4.8
- **Privileges**: Standard user (no admin required)
- **Architecture**: x86/x64

## 🐛 Known Issues

- CPU overlay may occasionally need repositioning after display configuration changes

## 📝 License

MIT License

Copyright (c) 2025 Sunil TG

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.

## 📧 Contact

For questions, suggestions, or issues, please open an issue on GitHub.