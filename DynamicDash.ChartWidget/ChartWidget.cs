using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Text.RegularExpressions;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Layout;
using Avalonia.Media;
using DynamicDash.Contracts;

namespace DynamicDash.ChartWidget;

[Export(typeof(IWidget))]
[ExportMetadata("Name", "Chart")]
public class ChartWidget : IWidget
{
    public string Name => "Chart";

    private Canvas? _chartCanvas;
    private TextBlock? _statsLabel;
    private readonly List<double> _numbers = new();
    private readonly IEventAggregator _eventAggregator;

    [ImportingConstructor]
    public ChartWidget(IEventAggregator eventAggregator)
    {
        _eventAggregator = eventAggregator;
        // Subscribe to DataSubmittedEvent
        _eventAggregator.Subscribe<DataSubmittedEvent>(OnDataReceived);
    }

    public object View => CreateView(); // Always create a fresh view

    private void OnDataReceived(DataSubmittedEvent dataEvent)
    {
        var text = dataEvent.Data;

        // Try to parse numbers from the text
        var numberMatches = Regex.Matches(text, @"-?\d+(?:\.\d+)?");
        var newNumbers = new List<double>();

        foreach (Match match in numberMatches)
        {
            if (double.TryParse(match.Value, out var number))
            {
                newNumbers.Add(number);
            }
        }

        if (newNumbers.Count > 0)
        {
            _numbers.Clear();
            _numbers.AddRange(newNumbers);
            RedrawChart();
            UpdateStats();
        }
        else
        {
            // No numbers found
            if (_statsLabel != null)
                _statsLabel.Text = "No numeric data found in: " + (text.Length > 30 ? text[..30] + "..." : text);
        }
    }

    private void RedrawChart()
    {
        if (_chartCanvas == null || _numbers.Count == 0) return;

        _chartCanvas.Children.Clear();

        var canvasWidth = 300.0;
        var canvasHeight = 200.0;
        var maxValue = _numbers.Max();
        var minValue = _numbers.Min();
        var range = Math.Max(maxValue - minValue, 1); // Avoid division by zero

        var barWidth = canvasWidth / _numbers.Count - 5;

        for (var i = 0; i < _numbers.Count; i++)
        {
            var value = _numbers[i];
            var normalizedValue = Math.Abs(value - minValue) / range;
            var barHeight = normalizedValue * (canvasHeight - 30) + 10; // Min height of 10

            var bar = new Rectangle
            {
                Width = barWidth,
                Height = barHeight,
                Fill = new SolidColorBrush(Color.FromRgb((byte)(50 + i * 30 % 200), 100, 255)),
                Stroke = new SolidColorBrush(Color.FromRgb(0, 0, 0)),
                StrokeThickness = 1
            };

            Canvas.SetLeft(bar, i * (barWidth + 5));
            Canvas.SetTop(bar, canvasHeight - barHeight - 10);

            _chartCanvas.Children.Add(bar);

            // Add value label
            var label = new TextBlock
            {
                Text = value.ToString("0.##"),
                FontSize = 10,
                Foreground = new SolidColorBrush(Color.FromRgb(0, 0, 0))
            };

            Canvas.SetLeft(label, i * (barWidth + 5) + barWidth / 2 - 10);
            Canvas.SetTop(label, canvasHeight - barHeight - 25);

            _chartCanvas.Children.Add(label);
        }
    }

    private void UpdateStats()
    {
        if (_statsLabel == null || _numbers.Count == 0) return;

        var count = _numbers.Count;
        var sum = _numbers.Sum();
        var avg = sum / count;
        var min = _numbers.Min();
        var max = _numbers.Max();

        _statsLabel.Text = $"Stats: Count={count}, Sum={sum:0.##}, Avg={avg:0.##}, Min={min:0.##}, Max={max:0.##}";
    }

    private Control CreateView()
    {
        var mainPanel = new StackPanel
        {
            Orientation = Orientation.Vertical,
            Margin = new Thickness(10),
            Spacing = 10
        };

        // Title
        var title = new TextBlock
        {
            Text = "Chart widget",
            FontSize = 18,
            FontWeight = FontWeight.Bold,
            Foreground = new SolidColorBrush(Color.FromRgb(21, 101, 192)),
            HorizontalAlignment = HorizontalAlignment.Center
        };

        // Chart canvas
        _chartCanvas = new Canvas
        {
            Width = 300,
            Height = 200,
            Background = new SolidColorBrush(Color.FromRgb(227, 242, 253))
        };

        var chartBorder = new Border
        {
            BorderBrush = new SolidColorBrush(Color.FromRgb(21, 101, 192)),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(4),
            Child = _chartCanvas
        };

        // Stats section
        _statsLabel = new TextBlock
        {
            Text = "Waiting for numeric data...",
            FontWeight = FontWeight.SemiBold,
            Background = new SolidColorBrush(Color.FromRgb(232, 245, 253)),
            Foreground = new SolidColorBrush(Color.FromRgb(25, 118, 210)),
            Padding = new Thickness(10),
            TextWrapping = TextWrapping.Wrap
        };

        // Clear button
        var clearButton = new Button
        {
            Content = "Clear chart",
            HorizontalAlignment = HorizontalAlignment.Center,
            Padding = new Thickness(15, 5),
            Background = new SolidColorBrush(Color.FromRgb(25, 118, 210)),
            Foreground = new SolidColorBrush(Color.FromRgb(255, 255, 255))
        };

        clearButton.Click += (_, _) =>
        {
            _numbers.Clear();
            _chartCanvas?.Children.Clear();
            if (_statsLabel != null)
                _statsLabel.Text = "Chart cleared. Waiting for numeric data...";
        };

        mainPanel.Children.Add(title);
        mainPanel.Children.Add(chartBorder);
        mainPanel.Children.Add(_statsLabel);
        mainPanel.Children.Add(clearButton);

        return new Border
        {
            BorderBrush = new SolidColorBrush(Color.FromRgb(21, 101, 192)),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(5),
            Padding = new Thickness(8),
            Child = mainPanel,
            Background = new SolidColorBrush(Color.FromRgb(240, 248, 255))
        };
    }
}
