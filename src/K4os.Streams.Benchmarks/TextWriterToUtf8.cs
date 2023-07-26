using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text;
using BenchmarkDotNet.Attributes;
using K4os.Streams.Buffers;

namespace K4os.Streams.Benchmarks;

[MemoryDiagnoser]
public class TextWriterToUtf8
{
	[Params(1, 10, 100, 1000)]
	public int Messages { get; set; }
	
//	[Benchmark]
//	public void MemoryStream()
//	{
//		using var stream = new MemoryStream();
//		using var writer = new StreamWriter(stream, Encoding.UTF8);
//		WriteMessage(writer);
//		writer.Flush();
//		Consume(stream.ToArray());
//	}

	[Benchmark(Baseline = true)]
	public void StringBuilder()
	{
		var builder = new StringBuilder();
		var writer = new StringWriter(builder);
		WriteMessage(writer);
		writer.Flush();
		Consume(builder.ToString());
	}

//	[Benchmark]
//	public void ResizingStream()
//	{
//		using var stream = new ResizingByteBufferStream();
//		using var writer = new StreamWriter(stream, Encoding.UTF8);
//		WriteMessage(writer);
//		writer.Flush();
//		Consume(stream.AsSpan());
//	}
	
	[Benchmark]
	public void ResizingByteTextWriter()
	{
		using var writer = new ResizingByteBufferTextWriter();
		WriteMessage(writer);
		writer.Flush();
		Consume(writer.AsSpan());
	}
	
	[Benchmark]
	public void ResizingCharWriter()
	{
		using var writer = new ResizingCharBufferTextWriter();
		WriteMessage(writer);
		writer.Flush();
		Consume(writer.AsSpan());
	}
	
	public void WriteMessage(TextWriter writer)
	{
		for (var i = 0; i < Messages; i++)
		{
			writer.Write(DateTime.UtcNow);
			writer.Write(" Request ");
			writer.Write("GET");
			writer.Write(" ");
			writer.Write("http://localhost/controller/action/");
			writer.Write(" finished with status code ");
			writer.Write(202);
			writer.Write(" in ");
			writer.Write(123.324);
			writer.Write("ms");
			writer.WriteLine();
		}
	}
	
	[MethodImpl(MethodImplOptions.NoInlining)]
	[SuppressMessage("ReSharper", "UnusedParameter.Local")]
	private static void Consume(ReadOnlySpan<byte> utf8) { }

	[MethodImpl(MethodImplOptions.NoInlining)]
	[SuppressMessage("ReSharper", "UnusedParameter.Local")]
	private static void Consume(ReadOnlySpan<char> text)
	{
		var limit = Encoding.UTF8.GetMaxByteCount(text.Length);
		Span<byte> buffer = stackalloc byte[limit];
		var bytes = Encoding.UTF8.GetBytes(text, buffer);
		Consume(buffer.Slice(0, bytes));
	}
}
