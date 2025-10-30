# DynamicDash

DynamicDash is a modular dashboard written with Avalonia UI and MEF (Managed Extensibility Framework). The solution demonstrates how to load external widgets at runtime and let them react to data submitted from the main application.

## Project structure
- `DynamicDash.App` – Avalonia desktop application hosting the dashboard, loading widget plug-ins, and exposing the UI.
- `DynamicDash.Contracts` – shared interfaces and simple event aggregator used both by the host and widgets (`IWidget`, `DataSubmittedEvent`, `EventAggregator`).
- `DynamicDash.TextWidget` – text widget that analyses submitted text (word/character/line count) and displays the latest snippet.
- `DynamicDash.ChartWidget` – chart widget that parses numbers from the submitted input and renders a simple bar chart with basic statistics.
- `Widgets` – directory where compiled widget DLLs are copied so that the application can discover them dynamically.

Each widget project contains an MSBuild target that copies its `*.dll` output to the `Widgets` folder after every build. The main app watches this folder and reloads widgets automatically when files change.

## Prerequisites
- .NET SDK 9.0 (the repo provides a Nix flake – `nix develop` – which sets up the required SDK, runtime, and native libraries for Avalonia/Skia).

## Build & run
```bash
# optional: enter the devshell if you are using Nix
nix develop .

# restore packages (first run only)
dotnet restore DynamicDash.sln

# build the whole solution including widgets
dotnet build DynamicDash.sln

# run the Avalonia desktop app
dotnet run --project DynamicDash.App
```

When the application starts you can:
1. Type text or numbers into the input field at the bottom and press **Send data**.
2. The `Text` widget updates counters and shows the latest snippet.
3. The `Chart` widget extracts numeric values, draws a bar chart, and displays summary statistics.
4. You can rebuild a widget project (`dotnet build DynamicDash.TextWidget` / `DynamicDash.ChartWidget`) and the app will reload the DLL from `Widgets` automatically.

## Creating additional widgets
1. Create a new class library project targeting `net9.0`.
2. Reference `DynamicDash.Contracts` and import MEF attributes (`System.ComponentModel.Composition`).
3. Implement `IWidget`, export it using `[Export(typeof(IWidget))]`, and inject the event aggregator via `[ImportingConstructor]` if you need to respond to `DataSubmittedEvent`.
4. Add a post-build copy target similar to the existing widgets so that your DLL lands in the `Widgets` directory.
5. Build the new project – it will appear in the dashboard without restarting the main app.
