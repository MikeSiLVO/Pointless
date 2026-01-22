# Pointless

A Windows 11 system tray app that auto-hides the mouse pointer when idle.

## Features

- Hide pointer after configurable idle timeout (1-60 seconds)
- Global hotkey toggle (default: Ctrl+Alt+P)
- Option to start with Windows
- System tray icon with quick access to settings
- Single instance enforcement

## Requirements

- Windows 10/11
- .NET 10 Runtime

## Building

```
dotnet build
```

## Usage

Run the app and it will minimize to the system tray. The cursor will automatically hide after the configured idle time and reappear when you move the mouse.

Right-click the tray icon to access settings or exit the application.
