using System.Threading.Channels;

namespace UrlShortener.Api.BackgroundProcessing;

/// <summary>
/// In-process queue of short codes waiting to have their click count
/// incremented. Registered as a singleton; <see cref="IClickTrackingQueue"/>
/// is the write side exposed to controllers, the concrete type's
/// <see cref="Reader"/> is consumed by <see cref="ClickTrackingBackgroundService"/>.
/// </summary>
public class ClickTrackingQueue : IClickTrackingQueue
{
    private readonly Channel<string> _channel = Channel.CreateUnbounded<string>(
        new UnboundedChannelOptions { SingleReader = true });

    public ChannelReader<string> Reader => _channel.Reader;

    public void Enqueue(string code) => _channel.Writer.TryWrite(code);
}
