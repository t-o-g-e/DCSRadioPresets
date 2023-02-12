using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace DCS_Radio_Presets;

public static class UiHelpers
{
    private static T? TryFindParent<T>(DependencyObject child)
        where T : DependencyObject
    {
        var parentObject = GetParentObject(child);

        if (parentObject == null) return null;

        if (parentObject is T parent)
            return parent;
        return TryFindParent<T>(parentObject);
    }

    private static DependencyObject? GetParentObject(DependencyObject? child)
    {
        if (child == null) return null;

        if (child is ContentElement contentElement)
        {
            var parent = ContentOperations.GetParent(contentElement);
            if (parent != null) return parent;

            return contentElement is FrameworkContentElement fce ? fce.Parent : null;
        }

        return VisualTreeHelper.GetParent(child);
    }

    public static T? TryFindFromPoint<T>(UIElement reference, Point point) where T : DependencyObject
    {
        if (reference.InputHitTest(point) is not DependencyObject element) return null;
        if (element is T) return (T)element;
        return TryFindParent<T>(element);
    }
}

public class BetterDataGrid : DataGrid
{
    private static readonly FieldInfo? IsDraggingSelectionField = 
        typeof(DataGrid).GetField("_isDraggingSelection", BindingFlags.Instance | BindingFlags.NonPublic);

    private static readonly MethodInfo? EndDraggingMethod =
        typeof(DataGrid).GetMethod("EndDragging", BindingFlags.Instance | BindingFlags.NonPublic);

    protected override void OnMouseMove(MouseEventArgs e)
    {
        IsDraggingSelectionField?.SetValue(this, false);
    }
}