using System;
using System.Threading.Tasks;
using CodingWithCalvin.LaunchyBar.Models;

namespace CodingWithCalvin.LaunchyBar.Services;

/// <summary>
/// Service for managing LaunchyBar configuration (in-memory only for now).
/// </summary>
public sealed class ConfigurationService : IConfigurationService
{
    private LaunchyBarConfiguration _configuration = LaunchyBarConfiguration.CreateDefault();

    /// <inheritdoc/>
    public LaunchyBarConfiguration Configuration => _configuration;

    /// <inheritdoc/>
    public event EventHandler? ConfigurationChanged;

    /// <inheritdoc/>
    public Task LoadAsync()
    {
        _configuration = LaunchyBarConfiguration.CreateDefault();
        ConfigurationChanged?.Invoke(this, EventArgs.Empty);
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task SaveAsync()
    {
        // In-memory only - no persistence for now
        ConfigurationChanged?.Invoke(this, EventArgs.Empty);
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task ResetToDefaultsAsync()
    {
        _configuration = LaunchyBarConfiguration.CreateDefault();
        ConfigurationChanged?.Invoke(this, EventArgs.Empty);
        return Task.CompletedTask;
    }
}
