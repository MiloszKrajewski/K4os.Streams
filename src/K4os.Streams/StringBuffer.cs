using K4os.Streams.Buffers;
using K4os.Streams.Internal;

namespace K4os.Streams;

public class StringBuffer<TCharBuffer>
	where TCharBuffer: struct, IBuffer<char>
{
	private TCharBuffer _bytes;

	protected ref TCharBuffer Bytes => ref _bytes;
	
	public int Length => (int)Math.Min(Bytes.Length, PooledArray<char>.MAX_ARRAY_ITEMS);

	public StringBuffer<TCharBuffer> Clear()
	{
		Bytes.Length = 0;
		return this;
	}

	public unsafe StringBuffer<TCharBuffer> Append(char symbol)
	{
		Bytes.Write(new ReadOnlySpan<char>(&symbol, 1));
		return this;
	}

	public StringBuffer<TCharBuffer> Append(ReadOnlySpan<char> text)
	{
		Bytes.Write(text);
		return this;
	}

	public StringBuffer<TCharBuffer> Append(string? text)
	{
		Bytes.Write(text.AsSpan());
		return this;
	}

	public StringBuffer<TCharBuffer> Append(char[] text)
	{
		Bytes.Write(text.AsSpan());
		return this;
	}

	public StringBuffer<TCharBuffer> Append(char[] text, int offset, int length)
	{
		Bytes.Write(text.AsSpan(offset, length));
		return this;
	}
	
	public override string ToString() =>
		Polyfills.CreateString(Length, this, static (target, self) => self.Bytes.ExportTo(target));

	public int ExportTo(Span<char> target) => 
		Bytes.ExportTo(target);
}