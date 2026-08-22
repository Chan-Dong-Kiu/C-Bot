using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;

namespace FPTEnglishRAG.Wpf.Behaviors;

public static class ScrollToBottomBehavior
{
    public static readonly DependencyProperty AutoScrollProperty =
        DependencyProperty.RegisterAttached(
            "AutoScroll",
            typeof(bool),
            typeof(ScrollToBottomBehavior),
            new PropertyMetadata(false, OnAutoScrollChanged));

    public static bool GetAutoScroll(DependencyObject obj) =>
        (bool)obj.GetValue(AutoScrollProperty);

    public static void SetAutoScroll(DependencyObject obj, bool value) =>
        obj.SetValue(AutoScrollProperty, value);

    private static void OnAutoScrollChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is ScrollViewer scrollViewer && e.NewValue is true)
        {
            scrollViewer.Loaded += OnScrollViewerLoaded;
        }
    }

    private static void OnScrollViewerLoaded(object sender, RoutedEventArgs e)
    {
        if (sender is ScrollViewer scrollViewer)
        {
            scrollViewer.ScrollToEnd();
            if (scrollViewer.Content is ItemsControl itemsControl)
            {
                if (itemsControl.ItemsSource is INotifyCollectionChanged notifyCollection)
                {
                    notifyCollection.CollectionChanged += (_, _) =>
                    {
                        scrollViewer.Dispatcher.InvokeAsync(() =>
                        {
                            scrollViewer.ScrollToEnd();
                        });
                    };
                }
            }
        }
    }
}
