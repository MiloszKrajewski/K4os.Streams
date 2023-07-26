using K4os.Streams.Buffers;

namespace K4os.Streams;

public class ResizingStringBuffer: StringBuffer<ResizingBuffer<char>>
{
	public new ResizingStringBuffer Clear()
	{
		base.Clear();
		return this;
	}

	public new ResizingStringBuffer Append(char text)
	{
		base.Append(text);
		return this;
	}

	public new ResizingStringBuffer Append(ReadOnlySpan<char> text)
	{
		base.Append(text);
		return this;
	}

	public new ResizingStringBuffer Append(string? text)
	{
		base.Append(text);
		return this;
	}

	public new ResizingStringBuffer Append(char[] text)
	{
		base.Append(text);
		return this;
	}

	public new ResizingStringBuffer Append(char[] text, int offset, int length)
	{
		base.Append(text, offset, length);
		return this;
	}
	
	public Span<char> AsSpan() => Bytes.Peek();
}
