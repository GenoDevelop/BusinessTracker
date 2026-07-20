using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace GenoDev.BusinessTracker.Wpf.Infrastructure.Controls;

public partial class DateRangePicker : UserControl
{
    public static readonly DependencyProperty StartDateProperty = DependencyProperty.Register(
        nameof(StartDate), typeof(DateTime?), typeof(DateRangePicker), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

    public DateTime? StartDate
    {
        get => (DateTime?)GetValue(StartDateProperty);
        set => SetValue(StartDateProperty, value);
    }

    public static readonly DependencyProperty EndDateProperty = DependencyProperty.Register(
        nameof(EndDate), typeof(DateTime?), typeof(DateRangePicker), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

    public DateTime? EndDate
    {
        get => (DateTime?)GetValue(EndDateProperty);
        set => SetValue(EndDateProperty, value);
    }

    public DateRangePicker()
    {
        InitializeComponent();
    }

    private bool _isClearing;

    private void RangeCalendar_SelectedDatesChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isClearing) return;

        StartDate = RangeCalendar.SelectedDates.FirstOrDefault();
        EndDate = RangeCalendar.SelectedDates.LastOrDefault();
    }

    private void ClearButton_Click(object sender, RoutedEventArgs e)
    {
        _isClearing = true;
        try
        {
            StartDate = null;
            EndDate = null;
            RangeCalendar.SelectedDates.Clear();
        }
        finally
        {
            _isClearing = false;
        }
        e.Handled = true;
    }
}
