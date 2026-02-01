using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using CodingWithCalvin.LaunchyBar.UI;
using Microsoft.VisualStudio.Shell;

namespace CodingWithCalvin.LaunchyBar.Services;

/// <summary>
/// Service for injecting the LaunchyBar into the Visual Studio shell's visual tree.
/// </summary>
public sealed class ShellInjectionService : IShellInjectionService
{
    private readonly IConfigurationService _configurationService;
    private readonly ILaunchService _launchService;

    private LaunchyBarControl? _barControl;
    private Grid? _targetGrid;
    private ColumnDefinition? _injectedColumn;

    private const double BarWidth = 48;

    public ShellInjectionService(IConfigurationService configurationService, ILaunchService launchService)
    {
        _configurationService = configurationService;
        _launchService = launchService;
    }

    public bool IsInjected => _barControl != null;

    public bool Inject()
    {
        ThreadHelper.ThrowIfNotOnUIThread();

        Debug.WriteLine("LaunchyBar: Inject() called");

        if (IsInjected)
        {
            Debug.WriteLine("LaunchyBar: Already injected");
            return true;
        }

        try
        {
            var mainWindow = Application.Current.MainWindow;
            if (mainWindow == null)
            {
                Debug.WriteLine("LaunchyBar: MainWindow is null");
                return false;
            }

            Debug.WriteLine($"LaunchyBar: MainWindow found - {mainWindow.GetType().FullName}, Size: {mainWindow.ActualWidth}x{mainWindow.ActualHeight}");

            // Find the main layout grid
            _targetGrid = FindMainLayoutGrid(mainWindow);
            if (_targetGrid == null)
            {
                Debug.WriteLine("LaunchyBar: Could not find main layout grid");
                return false;
            }

            Debug.WriteLine($"LaunchyBar: Found target grid with {_targetGrid.RowDefinitions.Count} rows, {_targetGrid.ColumnDefinitions.Count} columns");
            Debug.WriteLine($"LaunchyBar: Grid size: {_targetGrid.ActualWidth}x{_targetGrid.ActualHeight}");

            // Log existing children
            foreach (UIElement child in _targetGrid.Children)
            {
                var childRow = Grid.GetRow(child);
                var childCol = Grid.GetColumn(child);
                var childRowSpan = Grid.GetRowSpan(child);
                var childColSpan = Grid.GetColumnSpan(child);
                Debug.WriteLine($"LaunchyBar:   Child: {child.GetType().Name} at Row={childRow}, Col={childCol}, RowSpan={childRowSpan}, ColSpan={childColSpan}");
            }

            // Create our bar control
            _barControl = new LaunchyBarControl(_configurationService, _launchService);
            _barControl.Width = BarWidth;
            _barControl.HorizontalAlignment = HorizontalAlignment.Left;
            _barControl.VerticalAlignment = VerticalAlignment.Stretch;

            // Check if grid originally had column definitions
            var hadColumns = _targetGrid.ColumnDefinitions.Count > 0;
            Debug.WriteLine($"LaunchyBar: Grid originally had {_targetGrid.ColumnDefinitions.Count} columns");

            // If the grid had no columns, we need to add one for the existing content
            if (!hadColumns)
            {
                // Add a Star column for existing content first
                _targetGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                Debug.WriteLine("LaunchyBar: Added Star column for existing content");
            }

            // Insert our bar column at the beginning
            _injectedColumn = new ColumnDefinition { Width = new GridLength(BarWidth) };
            _targetGrid.ColumnDefinitions.Insert(0, _injectedColumn);

            // Shift all existing children to the right by incrementing their column
            foreach (UIElement child in _targetGrid.Children)
            {
                var currentCol = Grid.GetColumn(child);
                Grid.SetColumn(child, currentCol + 1);
                Debug.WriteLine($"LaunchyBar:   Shifted {child.GetType().Name} from col {currentCol} to {currentCol + 1}");
            }

            // Add our bar at column 0, spanning all rows (top to bottom)
            Grid.SetColumn(_barControl, 0);
            Grid.SetRow(_barControl, 0);
            Grid.SetRowSpan(_barControl, Math.Max(1, _targetGrid.RowDefinitions.Count));
            _targetGrid.Children.Add(_barControl);

            Debug.WriteLine("LaunchyBar: Injection successful!");
            Debug.WriteLine($"LaunchyBar: Bar at column 0, spanning {Grid.GetRowSpan(_barControl)} rows");

            return true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"LaunchyBar: Exception during injection - {ex.Message}");
            Debug.WriteLine($"LaunchyBar: Stack trace: {ex.StackTrace}");
            Remove();
            return false;
        }
    }

    public void Remove()
    {
        ThreadHelper.ThrowIfNotOnUIThread();

        if (_targetGrid != null && _barControl != null && _injectedColumn != null)
        {
            try
            {
                // Remove our bar
                _targetGrid.Children.Remove(_barControl);

                // Shift all children back
                foreach (UIElement child in _targetGrid.Children)
                {
                    var currentCol = Grid.GetColumn(child);
                    if (currentCol > 0)
                    {
                        Grid.SetColumn(child, currentCol - 1);
                    }
                }

                // Remove our column
                _targetGrid.ColumnDefinitions.Remove(_injectedColumn);
            }
            catch
            {
                // Best effort cleanup
            }
        }

        _barControl = null;
        _targetGrid = null;
        _injectedColumn = null;
    }

    /// <summary>
    /// Finds the main layout Grid in VS's visual tree.
    /// This should be the grid that contains the toolbar and main content area.
    /// </summary>
    private Grid? FindMainLayoutGrid(Window mainWindow)
    {
        // Walk the visual tree to find the main layout grid
        // We're looking for a large grid that contains the main VS content
        return FindMainGridRecursive(mainWindow, 0);
    }

    private Grid? FindMainGridRecursive(DependencyObject parent, int depth)
    {
        if (depth > 15)
            return null;

        var childCount = VisualTreeHelper.GetChildrenCount(parent);
        var indent = new string(' ', depth * 2);

        for (int i = 0; i < childCount; i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);

            if (depth < 5)
            {
                Debug.WriteLine($"LaunchyBar: {indent}[{depth}] {child.GetType().Name}");
            }

            if (child is Grid grid)
            {
                // Look for a grid that:
                // 1. Is large enough
                // 2. Has row definitions (VS uses rows for title/toolbar/content/statusbar)
                // 3. Contains relevant VS controls
                if (grid.ActualWidth > 400 && grid.ActualHeight > 300)
                {
                    Debug.WriteLine($"LaunchyBar: {indent}  Grid candidate: {grid.ActualWidth}x{grid.ActualHeight}, Rows={grid.RowDefinitions.Count}, Cols={grid.ColumnDefinitions.Count}");

                    // Check if this grid contains VsToolBarHostControl or similar
                    if (ContainsVsContent(grid))
                    {
                        Debug.WriteLine($"LaunchyBar: {indent}  ** MATCHED - contains VS content **");
                        return grid;
                    }
                }
            }

            // Continue searching
            var result = FindMainGridRecursive(child, depth + 1);
            if (result != null)
                return result;
        }

        return null;
    }

    private bool ContainsVsContent(Grid grid)
    {
        foreach (UIElement child in grid.Children)
        {
            var typeName = child.GetType().Name;
            // Look for VS-specific controls that indicate this is the main layout grid
            if (typeName.Contains("ToolBar") ||
                typeName.Contains("DockPanel") ||
                typeName.Contains("MainWindowTitleBar"))
            {
                return true;
            }
        }
        return false;
    }

    public void Dispose()
    {
        try
        {
            ThreadHelper.JoinableTaskFactory.Run(async () =>
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                Remove();
            });
        }
        catch
        {
            // Best effort
        }
    }
}
