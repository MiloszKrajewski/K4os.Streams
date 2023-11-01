using System.Runtime.CompilerServices;
using System.Text;
using BenchmarkDotNet.Attributes;

namespace K4os.Streams.Benchmarks;

public class StringBufferVsStringBuilder
{
	// [Params(1, 10, 100, 1000, 10000)]
	[Params(8192, 16384, 32768, 65536, 128*1024)]
	public int Length { get; set; }

	public static readonly string Text = Guid.NewGuid().ToString();

	[Benchmark]
	public void UseStringBuilder()
	{
		var builder = new StringBuilder();
		for (var i = Length / Text.Length; i > 0; i--) builder.Append(Text);
		Consume(builder.ToString());
	}

	[Benchmark]
	public void UseResizingStringBuffer()
	{
		var builder = new ResizingStringBuffer();
		for (var i = Length / Text.Length; i > 0; i--) builder.Append(Text);
		Consume(builder.ToString());
	}
	
	[Benchmark]
	public void UseChunkedStringBuffer()
	{
		var builder = new ChunkedStringBuffer();
		for (var i = Length / Text.Length; i > 0; i--) builder.Append(Text);
		Consume(builder.ToString());
	}

//	[Benchmark]
//	public void UseStringBufferSpan()
//	{
//		var builder = new ResizingStringBuffer();
//		for (var i = Length / Text.Length; i > 0; i--) builder.Append(Text);
//		Consume(builder.AsSpan());
//	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static void Consume(string _) { }

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static void Consume(ReadOnlySpan<char> _) { }
}
