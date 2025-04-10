# Windows Optimizer

A utility application for optimizing Windows performance.

## Features

- System resource optimization
- Temporary file cleanup
- Startup program management
- System information dashboard

## Prerequisites

- Windows 10 or 11
- .NET 7.0 or later (for development)

## Building from Source

1. Clone this repository
2. Open the solution in Visual Studio or VS Code
3. Build the solution

```
dotnet build
```

## Creating Release Builds

To create a self-contained release that doesn't require .NET runtime installation:

```
dotnet publish -c Release -r win-x64 --self-contained true
```

## Repository Structure

- `/WindowsOptimizerApp` - Main project folder containing all source code
  - `*.cs` and `*.xaml` files - Source code and UI definitions
  - `WindowsOptimizerApp.csproj` - Project file
  - `app.manifest` - Application manifest
  - `/Assets` - Contains application images and icons
  - `/Resources` - Contains resource files

## Distribution

The compiled application is available as a ZIP file in the Releases section. For source control (GitHub), only the source code files are included - build artifacts, binaries, and temporary files are excluded via the .gitignore file.

To distribute the application to end users, use the self-contained deployment package which does not require .NET installation.

## Download

You can download the latest release from the Releases page.

## License

This project is licensed under the MIT License
