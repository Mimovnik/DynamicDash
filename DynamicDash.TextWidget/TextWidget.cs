using System;
using System.ComponentModel.Composition;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using DynamicDash.Contracts;

namespace DynamicDash.TextWidget;

[Export(typeof(IWidget))]
[ExportMetadata("Name", "Text")]
public class TextWidget : IWidget
{
    public string Name => "Text";

    private TextBlock? _wordCountLabel;
    private TextBlock? _charCountLabel;
    private TextBlock? _lineCountLabel;
    private TextBlock? _receivedTextBlock;

    private readonly IEventAggregator _eventAggregator;

    [ImportingConstructor]
    public TextWidget(IEventAggregator eventAggregator)
    {
        _eventAggregator = eventAggregator;
        // Subscribe to DataSubmittedEvent
        _eventAggregator.Subscribe<DataSubmittedEvent>(OnDataReceived);
    }

    public object View => CreateView(); // Always create a fresh view

    private void OnDataReceived(DataSubmittedEvent dataEvent)
    {
        var text = dataEvent.Data;

        // Analyze the text
        var wordCount = text.Split(new[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries).Length;
        var charCount = text.Length;
        var lineCount = text.Split('\n').Length;

        // Update UI
        if (_wordCountLabel != null)
            _wordCountLabel.Text = $"Words: {wordCount}";
        if (_charCountLabel != null)
            _charCountLabel.Text = $"Characters: {charCount}";
        if (_lineCountLabel != null)
            _lineCountLabel.Text = $"Lines: {lineCount}";
        if (_receivedTextBlock != null)
            _receivedTextBlock.Text = text.Length > 100 ? text[..100] + "..." : text;
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
            Text = "Text widget",
            FontSize = 18,
            FontWeight = FontWeight.Bold,
            Foreground = new SolidColorBrush(Color.FromRgb(21, 101, 192)),
            HorizontalAlignment = HorizontalAlignment.Center
        };

        // Statistics section
        var statsPanel = new Border
        {
            Background = new SolidColorBrush(Color.FromRgb(227, 242, 253)),
            Padding = new Thickness(10),
            CornerRadius = new CornerRadius(5),
            Child = new StackPanel
            {
                Orientation = Orientation.Vertical,
                Spacing = 5
            }
        };

        var statsStackPanel = (StackPanel)statsPanel.Child!;

        _wordCountLabel = new TextBlock { Text = "Words: 0", FontWeight = FontWeight.SemiBold, Foreground = new SolidColorBrush(Color.FromRgb(25, 118, 210)) };
        _charCountLabel = new TextBlock { Text = "Characters: 0", FontWeight = FontWeight.SemiBold, Foreground = new SolidColorBrush(Color.FromRgb(25, 118, 210)) };
        _lineCountLabel = new TextBlock { Text = "Lines: 0", FontWeight = FontWeight.SemiBold, Foreground = new SolidColorBrush(Color.FromRgb(25, 118, 210)) };

        statsStackPanel.Children.Add(_wordCountLabel);
        statsStackPanel.Children.Add(_charCountLabel);
        statsStackPanel.Children.Add(_lineCountLabel);

        // Received text preview
        var previewLabel = new TextBlock
        {
            Text = "Latest text:",
            FontWeight = FontWeight.SemiBold,
            Margin = new Thickness(0, 10, 0, 5),
            Foreground = new SolidColorBrush(Color.FromRgb(21, 101, 192))
        };

        _receivedTextBlock = new TextBlock
        {
            Text = "No data received yet...",
            Background = new SolidColorBrush(Color.FromRgb(232, 245, 253)),
            Foreground = new SolidColorBrush(Color.FromRgb(25, 118, 210)),
            Padding = new Thickness(10),
            TextWrapping = TextWrapping.Wrap,
            MinHeight = 60
        };

        mainPanel.Children.Add(title);
        mainPanel.Children.Add(statsPanel);
        mainPanel.Children.Add(previewLabel);
        mainPanel.Children.Add(_receivedTextBlock);

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
