using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using TitleBarButton = iNKORE.UI.WPF.Modern.Controls.Primitives.TitleBarButton;
using TitleBarControl = iNKORE.UI.WPF.Modern.Controls.Primitives.TitleBarControl;

namespace PortableHub.App.Helpers;

/// <summary>
/// Attached helper for client-area <see cref="TitleBarControl"/> instances.
/// When ExtendViewIntoTitleBar is enabled on a ModernWindow, the window template's TitleBarControl
/// handles system caption buttons (Minimize, Maximize, Close). A client-area TitleBarControl used
/// to host the icon/title must have its duplicate caption buttons collapsed to eliminate ghosting
/// and overlapping caption controls.
/// </summary>
public static class TitleBarHelper
{
    public static readonly DependencyProperty RemoveCaptionButtonsProperty =
        DependencyProperty.RegisterAttached(
            "RemoveCaptionButtons",
            typeof(bool),
            typeof(TitleBarHelper),
            new PropertyMetadata(false, OnRemoveCaptionButtonsChanged));

    public static bool GetRemoveCaptionButtons(DependencyObject obj) => (bool)obj.GetValue(RemoveCaptionButtonsProperty);
    public static void SetRemoveCaptionButtons(DependencyObject obj, bool value) => obj.SetValue(RemoveCaptionButtonsProperty, value);

    private static void OnRemoveCaptionButtonsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is FrameworkElement fe && (bool)e.NewValue)
        {
            Apply(fe);
            fe.Loaded += Fe_Loaded;
            fe.LayoutUpdated += Fe_LayoutUpdated;
            fe.IsVisibleChanged += Fe_IsVisibleChanged;
            fe.SizeChanged += Fe_SizeChanged;
        }
    }

    private static void Fe_Loaded(object? sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement fe)
        {
            Apply(fe);
        }
    }

    private static void Fe_LayoutUpdated(object? sender, EventArgs e)
    {
        if (sender is FrameworkElement fe)
        {
            Apply(fe);
        }
    }

    private static void Fe_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (sender is FrameworkElement fe)
        {
            Apply(fe);
        }
    }

    private static void Fe_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (sender is FrameworkElement fe)
        {
            Apply(fe);
        }
    }

    public static void Apply(FrameworkElement fe)
    {
        // Only strip caption buttons from client-area content title bars.
        // The Window template's TitleBarControl (where TemplatedParent != null) must preserve its system caption buttons!
        if (fe.TemplatedParent != null)
        {
            return;
        }

        fe.ApplyTemplate();

        if (fe is System.Windows.Controls.Control ctrl)
        {
            if (ctrl.ReadLocalValue(System.Windows.Controls.Control.ForegroundProperty) == DependencyProperty.UnsetValue)
            {
                ctrl.SetResourceReference(System.Windows.Controls.Control.ForegroundProperty, "TextFillColorPrimaryBrush");
            }

            if (ctrl.Template?.FindName("Title", fe) is TextBlock titleTb)
            {
                titleTb.SetResourceReference(TextBlock.ForegroundProperty, "TextFillColorPrimaryBrush");
            }

            if (ctrl.Template?.FindName("PART_RightSystemOverlay", fe) is FrameworkElement rightOverlay)
            {
                CollapseElement(rightOverlay);
                CollapsePanelButtons(rightOverlay);
            }

            if (ctrl.Template?.FindName("MinimizeButton", fe) is FrameworkElement minBtn)
            {
                CollapseElement(minBtn);
            }

            if (ctrl.Template?.FindName("MaximizeRestoreButton", fe) is FrameworkElement maxBtn)
            {
                CollapseElement(maxBtn);
            }

            if (ctrl.Template?.FindName("CloseButton", fe) is FrameworkElement clsBtn)
            {
                CollapseElement(clsBtn);
            }
        }

        var visualTitle = FindVisualChildByName<TextBlock>(fe, "Title");
        if (visualTitle != null)
        {
            visualTitle.SetResourceReference(TextBlock.ForegroundProperty, "TextFillColorPrimaryBrush");
        }

        // Also inspect visual tree to ensure no TitleBarButton escaped
        CollapseVisualButtons(fe);
    }

    private static T? FindVisualChildByName<T>(DependencyObject parent, string name) where T : FrameworkElement
    {
        int count = VisualTreeHelper.GetChildrenCount(parent);
        for (int i = 0; i < count; i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            if (child is T typed && typed.Name == name)
            {
                return typed;
            }
            var sub = FindVisualChildByName<T>(child, name);
            if (sub != null)
            {
                return sub;
            }
        }
        return null;
    }

    private static void CollapseElement(FrameworkElement elem)
    {
        elem.Visibility = Visibility.Collapsed;
        elem.Width = 0;
        elem.MaxWidth = 0;
        elem.Opacity = 0;
        elem.IsEnabled = false;
        elem.IsHitTestVisible = false;
    }

    private static void CollapsePanelButtons(DependencyObject parent)
    {
        int count = VisualTreeHelper.GetChildrenCount(parent);
        for (int i = 0; i < count; i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            if (child is FrameworkElement fe)
            {
                if (child is TitleBarButton)
                {
                    CollapseElement(fe);
                }
            }
            CollapsePanelButtons(child);
        }
    }

    private static void CollapseVisualButtons(DependencyObject parent)
    {
        int count = VisualTreeHelper.GetChildrenCount(parent);
        for (int i = 0; i < count; i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            if (child is TitleBarButton btn)
            {
                CollapseElement(btn);
            }
            CollapseVisualButtons(child);
        }
    }
}
