namespace UrlShortener.Api.BackgroundProcessing;

/// <summary>
/// Write side of the click-tracking queue, seen by controllers. Enqueuing
/// never touches the database, so it can't add latency to a redirect.
/// </summary>
public interface IClickTrackingQueue
{
    void Enqueue(string code);
}
