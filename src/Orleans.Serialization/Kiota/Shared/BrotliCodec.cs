using System.Buffers;
using System.IO.Compression;

namespace Orleans.Serialization;

internal static class BrotliCodec
{
    public const int Quality_Default = 4;
    public const int WindowBits_Default = 22;

    private const int InitialDecompressMultiplier = 4;

    public static IMemoryOwner<byte> Compress(ReadOnlySequence<byte> input, int quality = Quality_Default, int window = WindowBits_Default)
    {
        var inputSpan = GetContiguousSpan(input, out var inputOwner);
        try
        {
            var maxSize = BrotliEncoder.GetMaxCompressedLength(inputSpan.Length);
            var outputOwner = MemoryPool<byte>.Shared.Rent(maxSize);

            if (BrotliEncoder.TryCompress(inputSpan, outputOwner.Memory.Span, out int bytesWritten, quality, window))
                return new SlicedMemoryOwner(outputOwner, bytesWritten);

            outputOwner.Dispose();
            throw new InvalidOperationException("Compression failed.");
        }
        finally
        {
            inputOwner?.Dispose();
        }
    }

    public static IMemoryOwner<byte> Decompress(ReadOnlySequence<byte> input)
    {
        var inputSpan = GetContiguousSpan(input, out var inputOwner);
        try
        {
            var outputSize = Math.Max(inputSpan.Length * InitialDecompressMultiplier, 1);

            while (true)
            {
                var outputOwner = MemoryPool<byte>.Shared.Rent(outputSize);

                if (BrotliDecoder.TryDecompress(inputSpan, outputOwner.Memory.Span, out int bytesWritten))
                    return new SlicedMemoryOwner(outputOwner, bytesWritten);

                outputOwner.Dispose();
                outputSize = checked(outputSize * 2);
            }
        }
        finally
        {
            inputOwner?.Dispose();
        }
    }

    private static ReadOnlySpan<byte> GetContiguousSpan(ReadOnlySequence<byte> input, out IMemoryOwner<byte>? owner)
    {
        if (input.IsSingleSegment)
        {
            owner = null;
            return input.FirstSpan;
        }

        owner = MemoryPool<byte>.Shared.Rent((int)input.Length);
        input.CopyTo(owner.Memory.Span);
        return owner.Memory.Span[..(int)input.Length];
    }

    private sealed class SlicedMemoryOwner(IMemoryOwner<byte> inner, int length) : IMemoryOwner<byte>
    {
        public Memory<byte> Memory => inner.Memory[..length];

        public void Dispose() => inner.Dispose();
    }
}