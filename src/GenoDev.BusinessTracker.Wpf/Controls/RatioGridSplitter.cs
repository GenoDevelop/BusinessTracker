using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace GenoDev.BusinessTracker.Wpf.Controls;

/// <summary>
/// Resizes two neighboring grid areas while keeping a consistent visible divider
/// and pointer hit box in both orientations.
/// </summary>
public class RatioGridSplitter : GridSplitter
{
    private DefinitionConstraint? _activeConstraint;

    public static readonly DependencyProperty OrientationProperty =
        DependencyProperty.Register(
            nameof(Orientation),
            typeof(Orientation),
            typeof(RatioGridSplitter),
            new FrameworkPropertyMetadata(Orientation.Horizontal));

    public RatioGridSplitter()
    {
        DragCompleted += (_, _) => RestoreDefinitionMaximum();
    }

    public Orientation Orientation
    {
        get => (Orientation)GetValue(OrientationProperty);
        set => SetValue(OrientationProperty, value);
    }

    protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        ApplyDefinitionMaximum();

        try
        {
            base.OnMouseLeftButtonDown(e);
        }
        catch
        {
            RestoreDefinitionMaximum();
            throw;
        }
    }

    protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
    {
        try
        {
            base.OnMouseLeftButtonUp(e);
        }
        finally
        {
            RestoreDefinitionMaximum();
        }
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        ApplyDefinitionMaximum();

        try
        {
            base.OnKeyDown(e);
        }
        finally
        {
            RestoreDefinitionMaximum();
        }
    }

    private void ApplyDefinitionMaximum()
    {
        RestoreDefinitionMaximum();

        if (Parent is not Grid grid)
        {
            return;
        }

        var behavior = ResolveResizeBehavior();

        if (Orientation == Orientation.Vertical)
        {
            var (firstIndex, secondIndex) = GetDefinitionIndices(
                Grid.GetColumn(this),
                behavior);

            ApplyColumnMaximum(grid, firstIndex, secondIndex);
            return;
        }

        var (firstRowIndex, secondRowIndex) = GetDefinitionIndices(
            Grid.GetRow(this),
            behavior);

        ApplyRowMaximum(grid, firstRowIndex, secondRowIndex);
    }

    private GridResizeBehavior ResolveResizeBehavior()
    {
        if (ResizeBehavior != GridResizeBehavior.BasedOnAlignment)
        {
            return ResizeBehavior;
        }

        if (Orientation == Orientation.Vertical)
        {
            return HorizontalAlignment switch
            {
                HorizontalAlignment.Left => GridResizeBehavior.PreviousAndCurrent,
                HorizontalAlignment.Right => GridResizeBehavior.CurrentAndNext,
                _ => GridResizeBehavior.PreviousAndNext
            };
        }

        return VerticalAlignment switch
        {
            VerticalAlignment.Top => GridResizeBehavior.PreviousAndCurrent,
            VerticalAlignment.Bottom => GridResizeBehavior.CurrentAndNext,
            _ => GridResizeBehavior.PreviousAndNext
        };
    }

    private static (int First, int Second) GetDefinitionIndices(
        int splitterIndex,
        GridResizeBehavior behavior) => behavior switch
        {
            GridResizeBehavior.PreviousAndCurrent =>
                (splitterIndex - 1, splitterIndex),
            GridResizeBehavior.CurrentAndNext =>
                (splitterIndex, splitterIndex + 1),
            _ => (splitterIndex - 1, splitterIndex + 1)
        };

    private void ApplyColumnMaximum(Grid grid, int firstIndex, int secondIndex)
    {
        if (firstIndex < 0 || secondIndex >= grid.ColumnDefinitions.Count)
        {
            return;
        }

        var first = grid.ColumnDefinitions[firstIndex];
        var second = grid.ColumnDefinitions[secondIndex];

        if (first.Width.IsStar && second.Width.IsStar)
        {
            return;
        }

        var resizedDefinition = !first.Width.IsStar ? first : second;
        var availableLength = first.Width.IsStar || second.Width.IsStar
            ? first.ActualWidth + second.ActualWidth
            : resizedDefinition.ActualWidth;
        var constrainedMaximum = Math.Max(
            resizedDefinition.MinWidth,
            Math.Min(resizedDefinition.MaxWidth, availableLength));

        _activeConstraint = new DefinitionConstraint(
            resizedDefinition,
            resizedDefinition.MaxWidth);
        resizedDefinition.MaxWidth = constrainedMaximum;
    }

    private void ApplyRowMaximum(Grid grid, int firstIndex, int secondIndex)
    {
        if (firstIndex < 0 || secondIndex >= grid.RowDefinitions.Count)
        {
            return;
        }

        var first = grid.RowDefinitions[firstIndex];
        var second = grid.RowDefinitions[secondIndex];

        if (first.Height.IsStar && second.Height.IsStar)
        {
            return;
        }

        var resizedDefinition = !first.Height.IsStar ? first : second;
        var availableLength = first.Height.IsStar || second.Height.IsStar
            ? first.ActualHeight + second.ActualHeight
            : resizedDefinition.ActualHeight;
        var constrainedMaximum = Math.Max(
            resizedDefinition.MinHeight,
            Math.Min(resizedDefinition.MaxHeight, availableLength));

        _activeConstraint = new DefinitionConstraint(
            resizedDefinition,
            resizedDefinition.MaxHeight);
        resizedDefinition.MaxHeight = constrainedMaximum;
    }

    private void RestoreDefinitionMaximum()
    {
        switch (_activeConstraint?.Definition)
        {
            case ColumnDefinition column:
                column.MaxWidth = _activeConstraint.OriginalMaximum;
                break;
            case RowDefinition row:
                row.MaxHeight = _activeConstraint.OriginalMaximum;
                break;
        }

        _activeConstraint = null;
    }

    private sealed record DefinitionConstraint(
        DependencyObject Definition,
        double OriginalMaximum);
}
