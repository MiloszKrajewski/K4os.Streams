using System.Runtime.CompilerServices;
using K4os.Streams.Buffers;

namespace K4os.Streams;

public class StringBuffer<TCharBuffer>: IStringBuffer
	where TCharBuffer: struct, IBuffer<char>
{
	private TCharBuffer _bytes;

	protected ref TCharBuffer Bytes => ref _bytes;
	
	public int Length => 
		StringBufferExtensions.ClampStringLength(Bytes.Length);
	
	public void Reset() => 
		Bytes.Length = 0;
	
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Extend(ReadOnlySpan<char> text) => 
		Bytes.Write(text);
	
	public int ExportTo(Span<char> target) => 
		Bytes.ExportTo(target);
	
	public override string ToString() => 
		Bytes.ExportToString();
}