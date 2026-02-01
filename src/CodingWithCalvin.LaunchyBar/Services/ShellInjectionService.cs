using System;
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
    private Grid? _injectedGrid;
    private FrameworkElement? _originalContent;
    private ContentPresenter? _targetPresenter;

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

        if (IsInjected)
            return true;

        try
        {
            var mainWindow = Application.Current.MainWindow;
            if (mainWindow == null)
                return false;

            // Find the main content area - we're looking for the area between toolbar and status bar
            // This requires walking VS's visual tree to find the right injection point
            var injectionTarget = FindInjectionTarget(mainWindow);
            if (injectionTarget == null)
                return false;

            _targetPresenter = injectionTarget;
            _originalContent = injectionTarget.Content as FrameworkElement;

            if (_originalContent == null)
                return false;

            // Create our bar control
            _barControl = new LaunchyBarControl(_configurationService, _launchService);
            _barControl.Width = BarWidth;
            _barControl.HorizontalAlignment = HorizontalAlignment.Left;
            _barControl.VerticalAlignment = VerticalAlignment.Stretch;

            // Create a new grid to hold both the bar and the original content
            _injectedGrid = new Grid();
            _injectedGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(BarWidth) });
            _injectedGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            // Remove original content from its parent
            injectionTarget.Content = null;

            // Add bar to column 0
            Grid.SetColumn(_barControl, 0);
            _injectedGrid.Children.Add(_barControl);

            // Add original content to column 1
            Grid.SetColumn(_originalContent, 1);
            _injectedGrid.Children.Add(_originalContent);

            // Set our grid as the new content
            injectionTarget.Content = _injectedGrid;

            return true;
        }
        catch (Exception)
        {
            Remove();
            return false;
        }
    }

    public void Remove()
    {
        ThreadHelper.ThrowIfNotOnUIThread();

        if (_targetPresenter != null && _originalContent != null && _injectedGrid != null)
        {
            try
            {
                _injectedGrid.Children.Clear();
                _targetPresenter.Content = _originalContent;
            }
            catch
            {
                // Best effort cleanup
            }
        }

        _barControl = null;
        _injectedGrid = null;
        _originalContent = null;
        _targetPresenter = null;
    }

    /// <summary>
    /// Finds the ContentPresenter that contains the main VS content area.
    /// This is the area between the toolbar and status bar.
    /// </summary>
    private ContentPresenter? FindInjectionTarget(Window mainWindow)
    {
        // Strategy: Walk the visual tree looking for a ContentPresenter
        // that contains the main dock/editor area.
        // This is fragile and may need adjustment for different VS versions.

        // Look for a ContentPresenter with a specific name or structure
        // VS typically has a structure like:
        // MainWindow > ... > DockPanel > [Toolbar, ContentPresenter (main area), StatusBar]

        return FindContentPresenterRecursive(mainWindow, 0);
    }

    private ContentPresenter? FindContentPresenterRecursive(DependencyObject parent, int depth)
    {
        if (depth > 20) // Prevent infinite recursion
            return null;

        var childCount = VisualTreeHelper.GetChildrenCount(parent);

        for (int i = 0; i < childCount; i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);

            // Look for ContentPresenter that might be our target
            if (child is ContentPresenter cp)
            {
                // Check if this ContentPresenter's content looks like the main content area
                if (IsMainContentArea(cp))
                {
                    return cp;
                }
            }

            // Also check for Grid with DockPanel children - common VS structure
            if (child is Grid grid)
            {
                // Look for the main content grid that has the editor/tool area
                var result = FindContentPresenterRecursive(grid, depth + 1);
                if (result != null)
                    return result;
            }

            if (child is DockPanel dockPanel)
            {
                var result = FindContentPresenterRecursive(dockPanel, depth + 1);
                if (result != null)
                    return result;
            }

            if (child is Border border)
            {
                var result = FindContentPresenterRecursive(border, depth + 1);
                if (result != null)
                    return result;
            }

            if (child is Decorator decorator)
            {
                var result = FindContentPresenterRecursive(decorator, depth + 1);
                if (result != null)
                    return result;
            }
        }

        return null;
    }

    private bool IsMainContentArea(ContentPresenter cp)
    {
        // Heuristics to identify the main content area:
        // 1. Should be reasonably large
        // 2. Should contain dock-related content

        if (cp.ActualWidth < 400 || cp.ActualHeight < 300)
            return false;

        // Check if the content's type name contains dock-related keywords
        var content = cp.Content;
        if (content == null)
            return false;

        var typeName = content.GetType().FullName ?? "";

        // VS's main content area typically has these in the type hierarchy
        if (typeName.Contains("Dock") ||
            typeName.Contains("ViewManager") ||
            typeName.Contains("MainWindow") ||
            typeName.Contains("Workspace"))
        {
            return true;
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
