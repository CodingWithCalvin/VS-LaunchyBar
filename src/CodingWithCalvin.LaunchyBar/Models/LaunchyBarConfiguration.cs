using System.Collections.Generic;

namespace CodingWithCalvin.LaunchyBar.Models;

/// <summary>
/// Configuration for the LaunchyBar extension.
/// </summary>
public sealed class LaunchyBarConfiguration
{
    /// <summary>
    /// Version of the configuration schema for migration purposes.
    /// </summary>
    public int Version { get; set; } = 1;

    /// <summary>
    /// The configured launch items.
    /// </summary>
    public List<LaunchItem> Items { get; set; } = new();

    /// <summary>
    /// Width of the bar in pixels.
    /// </summary>
    public int BarWidth { get; set; } = 48;

    /// <summary>
    /// Creates a default configuration with common VS items.
    /// </summary>
    public static LaunchyBarConfiguration CreateDefault()
    {
        return new LaunchyBarConfiguration
        {
            Items = new List<LaunchItem>
            {
                new()
                {
                    Id = "solution-explorer",
                    Name = "Solution Explorer",
                    IconPath = "KnownMonikers.Solution",
                    Type = LaunchItemType.ToolWindow,
                    Target = "View.SolutionExplorer",
                    Position = LaunchItemPosition.Top,
                    Order = 0
                },
                new()
                {
                    Id = "search",
                    Name = "Find in Files",
                    IconPath = "KnownMonikers.SearchFiles",
                    Type = LaunchItemType.VsCommand,
                    Target = "Edit.FindinFiles",
                    Position = LaunchItemPosition.Top,
                    Order = 1
                },
                new()
                {
                    Id = "terminal",
                    Name = "Terminal",
                    IconPath = "KnownMonikers.Console",
                    Type = LaunchItemType.VsCommand,
                    Target = "View.Terminal",
                    Position = LaunchItemPosition.Top,
                    Order = 2
                },
                new()
                {
                    Id = "git-changes",
                    Name = "Git Changes",
                    IconPath = "KnownMonikers.GitLogo",
                    Type = LaunchItemType.VsCommand,
                    Target = "View.GitWindow",
                    Position = LaunchItemPosition.Top,
                    Order = 3
                },
                new()
                {
                    Id = "debug",
                    Name = "Start Debugging",
                    IconPath = "KnownMonikers.Run",
                    Type = LaunchItemType.VsCommand,
                    Target = "Debug.Start",
                    Position = LaunchItemPosition.Top,
                    Order = 4
                },
                new()
                {
                    Id = "settings",
                    Name = "Options",
                    IconPath = "KnownMonikers.Settings",
                    Type = LaunchItemType.VsCommand,
                    Target = "Tools.Options",
                    Position = LaunchItemPosition.Bottom,
                    Order = 0
                }
            }
        };
    }
}
