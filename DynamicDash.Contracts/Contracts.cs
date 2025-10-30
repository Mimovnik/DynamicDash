using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;

namespace DynamicDash.Contracts;

// Main widget interface that all widgets must implement
public interface IWidget
{
    string Name { get; }
    object View { get; } // UserControl or similar view object
}

// Event class for data submission through Event Aggregator
public class DataSubmittedEvent
{
    public string Data { get; }

    public DataSubmittedEvent(string data)
    {
        Data = data;
    }
}

// Simple Event Aggregator interface (we'll implement our own)
public interface IEventAggregator
{
    void Subscribe<T>(Action<T> handler);
    void Publish<T>(T eventObj);
}

// Simple Event Aggregator implementation
public class EventAggregator : IEventAggregator
{
    private readonly Dictionary<Type, List<object>> _handlers = new();

    public void Subscribe<T>(Action<T> handler)
    {
        var eventType = typeof(T);
        if (!_handlers.ContainsKey(eventType))
            _handlers[eventType] = new List<object>();

        _handlers[eventType].Add(handler);
    }

    public void Publish<T>(T eventObj)
    {
        var eventType = typeof(T);
        if (_handlers.ContainsKey(eventType))
        {
            foreach (var handler in _handlers[eventType].Cast<Action<T>>())
            {
                handler(eventObj);
            }
        }
    }
}
