using K4os.Streams.Buffers;

namespace K4os.Streams;

/// <summary>
/// A <see cref="TextWriter"/> implementation that writes to <see cref="ResizingBuffer"/>
/// providing access to written data as a span.
/// Roughly used in situations similar to <code>new StreamWriter(new MemoryStream())</code> or
/// <code>new StringWriter(new StringBuilder())</code>. 
/// </summary>
public class ResizingByteBufferTextWriter: ByteBufferTextWriter<ResizingBuffer<byte>>
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
