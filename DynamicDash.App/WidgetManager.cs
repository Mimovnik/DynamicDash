using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using DynamicDash.Contracts;

namespace DynamicDash.App;

public class WidgetManager : IDisposable
{
    [ImportMany]
    public IWidget[] Widgets { get; set; } = Array.Empty<IWidget>();

    public IEventAggregator EventAggregator { get; }

    private CompositionContainer? _container;
    private FileSystemWatcher? _fileSystemWatcher;
    private readonly string _widgetsPath;

    public event EventHandler? WidgetsChanged;

    public WidgetManager()
    {
        EventAggregator = new EventAggregator();
        _widgetsPath = Path.Combine(Directory.GetCurrentDirectory(), "Widgets");

        // Ensure Widgets directory exists
        if (!Directory.Exists(_widgetsPath))
            Directory.CreateDirectory(_widgetsPath);

        SetupFileSystemWatcher();
    }

    private void SetupFileSystemWatcher()
    {
        try
        {
            _fileSystemWatcher = new FileSystemWatcher(_widgetsPath)
            {
                Filter = "*.dll",
                NotifyFilter = NotifyFilters.FileName | NotifyFilters.CreationTime | NotifyFilters.LastWrite,
                EnableRaisingEvents = true
            };

            _fileSystemWatcher.Created += OnWidgetFileChanged;
            _fileSystemWatcher.Deleted += OnWidgetFileChanged;
            _fileSystemWatcher.Changed += OnWidgetFileChanged;

            Console.WriteLine($"FileSystemWatcher monitoring: {_widgetsPath}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Warning: Could not setup FileSystemWatcher: {ex.Message}");
            Console.WriteLine("Dynamic widget loading will not be available.");
            _fileSystemWatcher = null;
        }
    }

    private void OnWidgetFileChanged(object sender, FileSystemEventArgs e)
    {
        Console.WriteLine($"Widget file {e.ChangeType}: {e.Name}");

        // Delay to allow file operations to complete
        Task.Delay(500).ContinueWith(_ =>
        {
            try
            {
                ReloadWidgets();
                WidgetsChanged?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error reloading widgets: {ex.Message}");
            }
        });
    }

    public void LoadWidgets()
    {
        try
        {
            // Clear existing widgets array to ensure fresh instances
            Widgets = Array.Empty<IWidget>();

            var catalog = new AggregateCatalog();

            // Add the current assembly (main app) for EventAggregator
            catalog.Catalogs.Add(new AssemblyCatalog(Assembly.GetExecutingAssembly()));

            // Only load widgets from Widgets folder - no built-in widgets
            LoadWidgetsFromFolder(catalog);

            // Dispose old container to release old widget instances
            _container?.Dispose();
            _container = new CompositionContainer(catalog);

            // Export the EventAggregator to MEF container so widgets can import it
            var batch = new CompositionBatch();
            batch.AddExportedValue(EventAggregator);
            _container.Compose(batch);

            // This will create fresh widget instances
            _container.ComposeParts(this);

            Console.WriteLine($"Loaded {Widgets.Length} widgets");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading widgets: {ex.Message}");
            throw;
        }
    }

    private void LoadWidgetsFromFolder(AggregateCatalog catalog)
    {
        if (!Directory.Exists(_widgetsPath))
            return;

        var dllFiles = Directory.GetFiles(_widgetsPath, "*.dll");

        foreach (var dllFile in dllFiles)
        {
            try
            {
                var assembly = Assembly.LoadFrom(dllFile);
                catalog.Catalogs.Add(new AssemblyCatalog(assembly));
                Console.WriteLine($"Loaded widget assembly: {Path.GetFileName(dllFile)}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to load widget {Path.GetFileName(dllFile)}: {ex.Message}");
            }
        }
    }

    private void ReloadWidgets()
    {
        Console.WriteLine("Reloading widgets...");
        LoadWidgets();
    }

    public void ManualReload()
    {
        ReloadWidgets();
        WidgetsChanged?.Invoke(this, EventArgs.Empty);
    }

    public IEnumerable<IWidget> GetAllWidgets() => Widgets;

    public void Dispose()
    {
        _fileSystemWatcher?.Dispose();
        _container?.Dispose();
    }
}
