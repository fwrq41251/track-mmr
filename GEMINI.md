# GEMINI.md - TrackMmr Project Context

## Project Overview
TrackMmr is a Dota 2 MMR (Matchmaking Rating) history tracking tool. It uses [SteamKit2](https://github.com/SteamRE/SteamKit) to communicate with the Steam Game Coordinator and fetch recent ranked match data, which is then stored in a local SQLite database for history tracking and visualization.

The project is structured as a .NET 10 solution with three main projects:
- **TrackMmr.Core**: Shared library containing the Steam communication logic (`SteamService`), database management (`MmrDatabase`), and data models.
- **TrackMmr.Cli**: A command-line interface for fetching MMR and viewing history.
- **TrackMmr.Desktop**: A graphical user interface built with Avalonia for a more visual experience, including charts.

## Main Technologies
- **Runtime**: .NET 10.0
- **Steam Integration**: `SteamKit2` for Steam protocol and Dota 2 Game Coordinator communication.
- **Database**: `Microsoft.Data.Sqlite` for local data storage.
- **GUI Framework**: `Avalonia` (v11+) with MVVM pattern (`CommunityToolkit.Mvvm`).
- **Data Visualization**: `LiveChartsCore.SkiaSharpView.Avalonia` for rendering MMR history charts.

## Key Commands

### Building the Project
```bash
dotnet build
```

### Running the CLI
```bash
# Fetch latest MMR and update database
dotnet run --project TrackMmr.Cli

# View MMR history
dotnet run --project TrackMmr.Cli -- history

# View last 30 days of history
dotnet run --project TrackMmr.Cli -- history 30

# Force login/update credentials
dotnet run --project TrackMmr.Cli -- login
```

### Running the Desktop GUI
```bash
dotnet run --project TrackMmr.Desktop
```

## Development Conventions

### Project Structure
- **TrackMmr.Core/**: Business logic and data layer.
  - `SteamService.cs`: Manages Steam client connection, authentication (including Steam Guard), and Dota 2 GC messages.
  - `MmrDatabase.cs`: Handles SQLite schema creation and CRUD operations.
  - `AppConfig.cs`: Manages application settings (Username, RefreshToken) stored in `config.json`.
- **TrackMmr.Cli/**: Simple entry point that orchestrates `SteamService` and `MmrDatabase` for terminal use.
- **TrackMmr.Desktop/**: Follows standard Avalonia MVVM structure.
  - `ViewModels/`: UI logic and data binding.
  - `Views/`: XAML-based UI definitions.

### Authentication
The application uses Steam's modern authentication flow. On the first run, it prompts for a username and password. After a successful login (including Steam Guard if enabled), it saves a `RefreshToken` to `config.json` and clears the password. Subsequent runs use the token for seamless login.

### Database
A local SQLite database `mmr.db` is created in the application's execution directory. It stores timestamps, match IDs, MMR values, and basic match outcomes.

## TODOs / Future Improvements
- [ ] Implement automatic background tracking (service/daemon).
- [ ] Add more detailed match statistics in the Desktop view.
- [ ] Support multiple Steam accounts.
- [ ] Export data to CSV/JSON.
