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

    private void RangeCalendar_SelectedDatesChanged(object sender, SelectionChangedEventArgs e)
    {
        StartDate = RangeCalendar.SelectedDates.FirstOrDefault();
        EndDate = RangeCalendar.SelectedDates.LastOrDefault();
    }
}
