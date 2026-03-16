namespace Orleans.Serialization;

public class KiotaMessagePackOptions
{
    /// <summary>
    /// Gets or sets a value indicating whether compression is enabled.
    /// </summary>
    public bool Compression { get; set; }

    /// <summary>
    /// A number representing quality of the Brotli compression. 0 is the minimum (no compression), 11 is the maximum.
    /// </summary>
    public int Quality { get; set; } = 4;

    /// <summary>
    /// A number representing the encoder window bits. The minimum value is 10, and the maximum value is 24.
    /// </summary>
    public int Window { get; set; } = 22;
}
