using K4os.Streams.Buffers;

namespace K4os.Streams;

/// <summary>
/// Replacement for <see cref="MemoryStream"/> with better performance, focused on memory pooling.
/// Data is stored in a single block of memory, so it is bound by maximum array size, and requires
/// contiguous memory block. This restrictions however come with better performance, at least
/// for small streams.
/// If you think you need something move advanced see <see cref="ChunkedByteBufferStream"/>.
/// </summary>
public class ResizingByteBufferStream: ByteBufferStreamAdapter<ResizingByteBuffer>
{
	/// <summary>
	/// Provides access to underlying data as a span.
	/// Please note, this method should be used with care, as it references
	/// pooled memory block, which might be reused by other components if stream
	/// is modified (e.g. by writing to it, or by changing its length).
	/// There is a real danger of memory corruption if this method is not used wisely. 
	/// </summary>
	/// <returns>Reference to memory block at the time of execution.</returns>
	public Span<byte> AsSpan() => Bytes.Peek();
}
