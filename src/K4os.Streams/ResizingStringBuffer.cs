using K4os.Streams.Buffers;

namespace K4os.Streams;

public class ResizingStringBuffer: StringBuffer<ResizingBuffer<char>>
{
	public Span<char> AsSpan() => Bytes.Peek();
}

public class ChunkedStringBuffer: StringBuffer<ChunkedBuffer<char>> { }
