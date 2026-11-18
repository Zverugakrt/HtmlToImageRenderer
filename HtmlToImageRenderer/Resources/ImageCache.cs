using System.Diagnostics.CodeAnalysis;

namespace HtmlToImageRenderer.Resources;

/// <summary>
/// Least-recently-used cache of decoded images keyed by the original <c>src</c> string.
/// Entries are reference counted: an image an in-flight render is still painting is only marked as
/// evicted and is disposed when its last reference is released, so eviction can never pull a bitmap
/// out from under the painter.
/// </summary>
public sealed class ImageCache : IDisposable
{
    private readonly object _gate = new();
    private readonly Dictionary<string, LinkedListNode<Entry>> _entries = new(StringComparer.Ordinal);

    // Most recently used entry at the head; the tail is the next eviction candidate.
    private readonly LinkedList<Entry> _order = new();

    private readonly int _capacity;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="ImageCache"/> class.
    /// </summary>
    /// <param name="capacity">Maximum number of cached images; values below one are clamped to one.</param>
    public ImageCache(int capacity)
    {
        _capacity = Math.Max(1, capacity);
    }

    /// <summary>
    /// Gets the maximum number of images the cache keeps.
    /// </summary>
    public int Capacity => _capacity;

    /// <summary>
    /// Gets the number of live entries, excluding images that are evicted but still in use.
    /// </summary>
    public int Count
    {
        get
        {
            lock (_gate)
            {
                return _order.Count;
            }
        }
    }

    /// <summary>
    /// Takes a reference to a cached image and marks it as most recently used.
    /// </summary>
    /// <param name="source">Original <c>src</c> value of the image.</param>
    /// <param name="image">Cached image when the method succeeds.</param>
    /// <returns><c>true</c> when the image was found and is not pending eviction.</returns>
    public bool TryAcquire(string source, [NotNullWhen(true)] out HtmlImage? image)
    {
        image = null;

        if (string.IsNullOrEmpty(source))
        {
            return false;
        }

        lock (_gate)
        {
            if (_disposed || !_entries.TryGetValue(source, out LinkedListNode<Entry>? node))
            {
                return false;
            }

            Entry entry = node.Value;

            if (entry.Evicted)
            {
                return false;
            }

            entry.References++;
            _order.Remove(node);
            _order.AddFirst(node);
            image = entry.Image;
            return true;
        }
    }

    /// <summary>
    /// Stores a freshly loaded image and takes the first reference to it. When an entry for the same
    /// source already exists, the incoming image is disposed and the cached instance is returned.
    /// </summary>
    /// <param name="source">Original <c>src</c> value of the image.</param>
    /// <param name="image">Image to cache; ownership transfers to the cache unless it is rejected.</param>
    /// <returns>The image the caller must use, which may be the already cached instance.</returns>
    public HtmlImage AddOrAcquire(string source, HtmlImage image)
    {
        ArgumentNullException.ThrowIfNull(image);

        lock (_gate)
        {
            if (_disposed)
            {
                return image;
            }

            if (_entries.TryGetValue(source, out LinkedListNode<Entry>? existing))
            {
                // Another load finished first, so the cached instance wins.
                image.Dispose();

                Entry entry = existing.Value;

                if (entry.Evicted)
                {
                    // The entry came back before its last reference was released: revive it.
                    entry.Evicted = false;
                    _order.AddFirst(existing);
                }
                else
                {
                    _order.Remove(existing);
                    _order.AddFirst(existing);
                }

                entry.References++;
                TrimToCapacity();
                return entry.Image;
            }

            var node = new LinkedListNode<Entry>(new Entry(source, image) { References = 1 });
            _order.AddFirst(node);
            _entries[source] = node;
            TrimToCapacity();
            return image;
        }
    }

    /// <summary>
    /// Releases one reference taken by <see cref="TryAcquire"/> or <see cref="AddOrAcquire"/>.
    /// </summary>
    /// <param name="source">Original <c>src</c> value of the image.</param>
    public void Release(string source)
    {
        if (string.IsNullOrEmpty(source))
        {
            return;
        }

        lock (_gate)
        {
            if (!_entries.TryGetValue(source, out LinkedListNode<Entry>? node))
            {
                return;
            }

            Entry entry = node.Value;

            if (entry.References > 0)
            {
                entry.References--;
            }

            if (entry.References == 0 && entry.Evicted)
            {
                _entries.Remove(source);
                entry.Image.Dispose();
            }
        }
    }

    /// <summary>
    /// Drops every entry. Images still referenced by an active render are disposed when released.
    /// </summary>
    public void Clear()
    {
        lock (_gate)
        {
            LinkedListNode<Entry>? node = _order.First;

            while (node is not null)
            {
                LinkedListNode<Entry>? next = node.Next;
                Evict(node);
                node = next;
            }
        }
    }

    /// <summary>
    /// Releases every cached image. It must not race with an active render.
    /// </summary>
    public void Dispose()
    {
        lock (_gate)
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;

            foreach (LinkedListNode<Entry> node in _entries.Values)
            {
                node.Value.Image.Dispose();
            }

            _entries.Clear();
            _order.Clear();
        }
    }

    /// <summary>
    /// Evicts entries from the tail until the cache fits its capacity again.
    /// </summary>
    private void TrimToCapacity()
    {
        while (_order.Count > _capacity)
        {
            LinkedListNode<Entry>? oldest = _order.Last;

            if (oldest is null)
            {
                return;
            }

            Evict(oldest);
        }
    }

    /// <summary>
    /// Removes an entry from the LRU order, disposing the image immediately when nobody uses it and
    /// deferring the disposal to <see cref="Release"/> otherwise.
    /// </summary>
    /// <param name="node">Order node of the entry to evict.</param>
    private void Evict(LinkedListNode<Entry> node)
    {
        Entry entry = node.Value;
        _order.Remove(node);

        if (entry.References <= 0)
        {
            _entries.Remove(entry.Source);
            entry.Image.Dispose();
            return;
        }

        // Still painted by an active render: keep it addressable by source so Release can finish it.
        entry.Evicted = true;
    }

    /// <summary>
    /// One cache entry: the image, how many renders use it, and whether it is on its way out.
    /// </summary>
    private sealed class Entry
    {
        public Entry(string source, HtmlImage image)
        {
            Source = source;
            Image = image;
        }

        /// <summary>Gets the original <c>src</c> value used as the cache key.</summary>
        public string Source { get; }

        /// <summary>Gets the decoded image owned by the cache.</summary>
        public HtmlImage Image { get; }

        /// <summary>Gets or sets the number of renders currently using the image.</summary>
        public int References { get; set; }

        /// <summary>Gets or sets a value indicating whether the entry was evicted while still in use.</summary>
        public bool Evicted { get; set; }
    }
}
