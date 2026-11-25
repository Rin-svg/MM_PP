using SlideStudio.Core.Model;

namespace SlideStudio.UI.Canvas;

/// <summary>Cache borné des vignettes rendues pour une présentation.</summary>
public sealed class ThumbnailCache : IDisposable
{
    private const int ThumbnailWidth = 192;
    private const int ThumbnailHeight = 108;
    private const long MaximumCacheBytes = 24L * 1024 * 1024;
    private readonly SlideRenderer _renderer;
    private readonly Dictionary<Guid, Entry> _entries = [];
    private readonly LinkedList<Guid> _lru = [];
    private long _cacheBytes;

    /// <summary>Crée un cache associé au renderer partagé.</summary>
    public ThumbnailCache(SlideRenderer renderer)
    {
        ArgumentNullException.ThrowIfNull(renderer);
        _renderer = renderer;
    }

    /// <summary>Nombre de vignettes actuellement mises en cache.</summary>
    public int Count => _entries.Count;

    /// <summary>Renvoie une vignette mise en cache dans un cadre donné.</summary>
    public void Draw(Graphics graphics, Slide slide, Rectangle bounds)
    {
        ArgumentNullException.ThrowIfNull(graphics);
        ArgumentNullException.ThrowIfNull(slide);
        if (bounds.Width <= 0 || bounds.Height <= 0)
        {
            return;
        }

        Bitmap thumbnail = GetOrCreate(slide);
        graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
        graphics.DrawImage(thumbnail, bounds);
    }

    /// <summary>Supprime une vignette devenue obsolète.</summary>
    public void Invalidate(Slide slide)
    {
        ArgumentNullException.ThrowIfNull(slide);
        if (_entries.Remove(slide.Id, out Entry? entry))
        {
            _lru.Remove(entry.Node);
            _cacheBytes -= entry.SizeBytes;
            entry.Bitmap.Dispose();
        }
    }

    /// <summary>Vide toutes les vignettes.</summary>
    public void Clear()
    {
        foreach (Entry entry in _entries.Values)
        {
            entry.Bitmap.Dispose();
        }
        _entries.Clear();
        _lru.Clear();
        _cacheBytes = 0;
    }

    /// <inheritdoc />
    public void Dispose() => Clear();

    private Bitmap GetOrCreate(Slide slide)
    {
        if (_entries.TryGetValue(slide.Id, out Entry? entry))
        {
            _lru.Remove(entry.Node);
            _lru.AddFirst(entry.Node);
            return entry.Bitmap;
        }

        Bitmap thumbnail = _renderer.Render(slide, new Size(ThumbnailWidth, ThumbnailHeight));
        long sizeBytes = (long)thumbnail.Width * thumbnail.Height * 4;
        if (_cacheBytes + sizeBytes > MaximumCacheBytes)
        {
            EvictUntilSize(sizeBytes);
        }

        LinkedListNode<Guid> node = new(slide.Id);
        _lru.AddFirst(node);
        Entry added = new(thumbnail, sizeBytes, node);
        _entries.Add(slide.Id, added);
        _cacheBytes += sizeBytes;
        return thumbnail;
    }

    private void EvictUntilSize(long incomingSizeBytes)
    {
        while (_cacheBytes + incomingSizeBytes > MaximumCacheBytes && _lru.Last is not null)
        {
            Guid oldestId = _lru.Last.Value;
            if (_entries.Remove(oldestId, out Entry? entry))
            {
                _lru.Remove(entry.Node);
                _cacheBytes -= entry.SizeBytes;
                entry.Bitmap.Dispose();
            }
            else
            {
                _lru.RemoveLast();
            }
        }
    }

    private sealed record Entry(Bitmap Bitmap, long SizeBytes, LinkedListNode<Guid> Node);
}
