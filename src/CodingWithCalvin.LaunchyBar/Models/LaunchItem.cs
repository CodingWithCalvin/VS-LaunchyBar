using System;
using System.Text.Json.Serialization;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;

namespace CodingWithCalvin.LaunchyBar.Models;

/// <summary>
/// Represents a single item in the LaunchyBar.
/// </summary>
public sealed class LaunchItem
{
    /// <summary>
    /// Unique identifier for the launch item.
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Display name shown in tooltip.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Path to icon file, embedded resource, or VS ImageMoniker name.
    /// </summary>
    public string IconPath { get; set; } = string.Empty;

    /// <summary>
    /// The type of action this item performs.
    /// </summary>
    public LaunchItemType Type { get; set; } = LaunchItemType.VsCommand;

    /// <summary>
    /// The target of the action: executable path, command ID, or tool window GUID.
    /// </summary>
    public string Target { get; set; } = string.Empty;

    /// <summary>
    /// Optional command-line arguments for external programs.
    /// </summary>
    public string? Arguments { get; set; }

    /// <summary>
    /// Optional working directory for external programs.
    /// </summary>
    public string? WorkingDirectory { get; set; }

    /// <summary>
    /// Where this item appears in the bar.
    /// </summary>
    public LaunchItemPosition Position { get; set; } = LaunchItemPosition.Top;

    /// <summary>
    /// Sort order within the position group.
    /// </summary>
    public int Order { get; set; }

    /// <summary>
    /// Whether this item is enabled.
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Gets the ImageMoniker for this item's icon.
    /// </summary>
    [JsonIgnore]
    public ImageMoniker IconMoniker => GetIconMoniker();

    private ImageMoniker GetIconMoniker()
    {
        if (string.IsNullOrEmpty(IconPath))
        {
            return KnownMonikers.QuestionMark;
        }

        if (IconPath.StartsWith("KnownMonikers."))
        {
            var monikerName = IconPath.Substring("KnownMonikers.".Length);
            var property = typeof(KnownMonikers).GetProperty(monikerName);
            if (property != null)
            {
                return (ImageMoniker)property.GetValue(null)!;
            }
        }

        return KnownMonikers.QuestionMark;
    }
}
