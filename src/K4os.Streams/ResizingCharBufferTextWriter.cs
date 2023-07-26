using K4os.Streams.Buffers;

namespace K4os.Streams;

public class ResizingCharBufferTextWriter: CharBufferTextWriter<ResizingBuffer<char>>
{
	public ResizingCharBufferTextWriter(IFormatProvider? formatProvider):
		base(formatProvider) { }

	public ResizingCharBufferTextWriter(): this(null) { }

	public Span<char> AsSpan() => Bytes.Peek();
}
