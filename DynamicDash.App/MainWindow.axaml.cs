using System;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using DynamicDash.Contracts;

namespace DynamicDash.App;

public partial class MainWindow : Window
{
    private WidgetManager _widgetManager = null!;

    public MainWindow()
    {
        InitializeComponent();
        _widgetManager = new WidgetManager();

        // Subscribe to widget changes
        _widgetManager.WidgetsChanged += OnWidgetsChanged;

        SetupEventHandlers();
        _ = LoadWidgetsAsync();
    }

    private async void OnWidgetsChanged(object? sender, EventArgs e)
    {
        // Reload widgets on UI thread
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            DisplayWidgets();
            var widgetCount = _widgetManager.GetAllWidgets().Count();
            UpdateStatus("Widgets reloaded.");
            UpdateInfo($"Widgets: {widgetCount}");
        });
    }

    private void SetupEventHandlers()
    {
        var sendDataButton = this.FindControl<Button>("SendDataButton");

        if (sendDataButton != null)
            sendDataButton.Click += OnSendDataButtonClick;
    }

    private void OnSendDataButtonClick(object? sender, RoutedEventArgs e)
    {
        var dataTextBox = this.FindControl<TextBox>("DataTextBox");
        var statusLabel = this.FindControl<TextBlock>("StatusLabel");

        if (dataTextBox != null && !string.IsNullOrWhiteSpace(dataTextBox.Text))
        {
            // Publish the data through Event Aggregator
            var eventAggregator = _widgetManager.EventAggregator;
            var dataEvent = new DataSubmittedEvent(dataTextBox.Text);
            eventAggregator.Publish(dataEvent);

            if (statusLabel != null)
                statusLabel.Text = $"Data sent: '{dataTextBox.Text}'";
        }
        else
        {
            if (statusLabel != null)
                statusLabel.Text = "Please enter some data first!";
        }
    }


    private async Task LoadWidgetsAsync()
    {
        try
        {
            UpdateStatus("Loading widgets...");

            await Task.Run(() =>
            {
                _widgetManager.LoadWidgets();
            });

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                DisplayWidgets();
                var widgetCount = _widgetManager.GetAllWidgets().Count();
                UpdateStatus("Widgets ready.");
                UpdateInfo($"Widgets: {widgetCount}");
            });
        }
        catch (Exception ex)
        {
            UpdateStatus($"Error loading widgets: {ex.Message}");
            UpdateInfo("Failed to load widgets. Check console for details.");
        }
    }

    private void DisplayWidgets()
    {
        var widgetContainer = this.FindControl<WrapPanel>("WidgetContainer");
        if (widgetContainer == null) return;

        widgetContainer.Children.Clear();

        foreach (var widget in _widgetManager.GetAllWidgets())
        {
            try
            {
                // Get widget view - this should create a fresh view each time
                var widgetView = widget.View as Control;

                // Check if the view is already parented - if so, skip it or create a new one
                if (widgetView?.Parent != null)
                {
                    Console.WriteLine($"Warning: Widget view for {widget.Name} already has a parent, skipping...");
                    continue;
                }

                    var widgetWrapper = new Border
                    {
                        BorderBrush = new SolidColorBrush(Color.FromRgb(21, 101, 192)),
                        BorderThickness = new Avalonia.Thickness(2),
                        Margin = new Avalonia.Thickness(10),
                        Padding = new Avalonia.Thickness(8),
                        CornerRadius = new Avalonia.CornerRadius(10),
                        Background = new SolidColorBrush(Color.FromRgb(240, 248, 255)),
                        Child = new StackPanel
                        {
                            Spacing = 6,
                            Children =
                            {
                                new TextBlock
                                {
                                    Text = widget.Name,
                                    FontWeight = FontWeight.Bold,
                                    Foreground = new SolidColorBrush(Color.FromRgb(13, 71, 161)),
                                    Margin = new Avalonia.Thickness(0, 0, 0, 4),
                                    HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center
                                },
                                widgetView ?? new TextBlock { Text = "Widget view not available", Foreground = new SolidColorBrush(Color.FromRgb(13, 71, 161)) }
                            }
                        }
                    };

                widgetContainer.Children.Add(widgetWrapper);
                Console.WriteLine($"Displayed widget: {widget.Name}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error displaying widget: {ex.Message}");
            }
        }
    }

    private void UpdateStatus(string message)
    {
        var statusLabel = this.FindControl<TextBlock>("StatusLabel");
        if (statusLabel != null)
        {
            statusLabel.Text = message;
            Console.WriteLine($"Status: {message}");
        }
    }

    private void UpdateInfo(string message)
    {
        var infoLabel = this.FindControl<TextBlock>("InfoLabel");
        if (infoLabel != null)
        {
            infoLabel.Text = message;
        }
    }

    protected override void OnClosed(EventArgs e)
    {
        _widgetManager?.Dispose();
        base.OnClosed(e);
    }
}
