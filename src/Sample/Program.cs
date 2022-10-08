using System.Buffers;
using System.IO.Compression;
using Cocona;

namespace Sample;

class Program
{
    private static readonly NLog.Logger _logger = NLog.LogManager.GetCurrentClassLogger();

    static void Main(string[] args)
    {
        CoconaLiteApp.Run<Program>(args);
    }

    public void Run()
    {
        var buffer = new byte[1024 * 1024];

        var CompressedBufferWriter = new ArrayBufferWriter<byte>();
        var DecompressedBufferWriter = new ArrayBufferWriter<byte>();

        Compress(new ReadOnlySequence<byte>(buffer), CompressedBufferWriter);
        Decompress(new ReadOnlySequence<byte>(CompressedBufferWriter.WrittenMemory), DecompressedBufferWriter);

        bool result = buffer.SequenceEqual(DecompressedBufferWriter.GetMemory().ToArray());
        Console.Out.WriteLine($"Result: {result}");
    }


    public static void Compress(ReadOnlySequence<byte> sequence, IBufferWriter<byte> writer)
    {
        using var encoder = new BrotliEncoder(11, 24);

        var reader = new SequenceReader<byte>(sequence);

        for (; ; )
        {
            var status = encoder.Compress(reader.UnreadSpan, writer.GetSpan(), out var consumed, out var written, false);
            if (status == OperationStatus.InvalidData) throw new Exception("invalid data");

            reader.Advance(consumed);
            writer.Advance(written);
            if (status == OperationStatus.Done) break;
        }

        for (; ; )
        {
            var status = encoder.Compress(ReadOnlySpan<byte>.Empty, writer.GetSpan(), out _, out var written, true);
            if (status == OperationStatus.InvalidData) throw new Exception("invalid data");

            writer.Advance(written);
            if (written == 0) break;
        }
    }

    public static void Decompress(ReadOnlySequence<byte> sequence, IBufferWriter<byte> writer)
    {
        var reader = new SequenceReader<byte>(sequence);

        using var decoder = new BrotliDecoder();

        for (; ; )
        {
            var status = decoder.Decompress(reader.UnreadSpan, writer.GetSpan(), out var consumed, out var written);
            if (status == OperationStatus.InvalidData) throw new Exception("invalid data");

            reader.Advance(consumed);
            writer.Advance(written);
            if (written == 0) break;
        }
    }
}
