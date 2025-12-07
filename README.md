# Screenshot Tool

A professional Windows Forms application for taking screenshots and screen recordings.

## Features

- **Screenshots:** Capture full monitors or specific regions.
- **Recordings:** Record screen video with audio support.
- **Management:** View, filter, and manage your media library.
- **Live Preview:** See what you are capturing in real-time.
- **Compact Mode:** Minimalistic interface for unobtrusive operation.

## Requirements

- .NET 10.0 (Windows)
- Windows OS (due to WinForms and COM dependencies)

## Getting Started

1. Open `ScreenshotTool.sln` in Visual Studio.
2. Build the solution.
3. Run the application.

## Project Structure

- **ScreenshotTool**: Main Windows Forms application.
- **ScreenshotTool.Tests**: Unit tests for the application services.

## Architecture

The application follows a structured service-oriented approach:
- **MainForm**: Handles UI interaction and events.
- **Services**:
  - `ScreenshotService`: Image capture logic.
  - `RecordingService`: Video recording logic.
  - `FileService`: File management and cleanup.
  - `SettingsService`: Application configuration.
