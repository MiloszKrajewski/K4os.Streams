using K4os.Streams.Buffers;

namespace K4os.Streams;

/// <summary>
/// Replacement for <see cref="MemoryStream"/> with better performance, focused on memory pooling.
/// Data is stored within a collection on chunks, so it is not bound by maximum array size, nor
/// it requires contiguous memory block. This comes at a cost of additional complexity.
/// If you think you need something simpler see <see cref="ResizingByteBufferStream"/>.
/// </summary>
public class ChunkedByteBufferStream: ByteBufferStream<ChunkedBuffer<byte>> { }
