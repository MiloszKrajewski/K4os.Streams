using BenchmarkDotNet.Attributes;
using K4os.Streams.Buffers;
using Microsoft.IO;

namespace K4os.Streams.Benchmarks;

[MemoryDiagnoser]
public class StreamRoundTrip
{
	private readonly byte[] _source = new byte[1024];
	private readonly byte[] _target = new byte[8192];
	
	// [Params(128, 1024, 8192, 65335, 128*1024, 8*1024*1024, 512*1024*1024)]
	// [Params(128, 1024, 8192, 65335, 128*1024)]
	// [Params(8*1024*1024, 512*1024*1024)]
	// [Params(128*1024, 8*1024*1024)]
	
	[Params(128, 1024, 8192, 65336)] // small
	// [Params(8192, 65336)] // small
	// [Params(128*1024, 1024*1024, 8*1024*1024)] // medium
	// [Params(128*1024*1024, 512*1024*1024)] // large
	public int Length { get; set; }
	
	[GlobalSetup]
	public void Setup()
	{
		Random.Shared.NextBytes(_source);
	}

	[Benchmark(Baseline = true)]
	public void MemoryStream()
	{
		using var stream = new MemoryStream();
		WriteBytesTo(stream);
		ReadAllBytesFrom(stream);
	}
	
	private static readonly RecyclableMemoryStreamManager RecyclingManager = new();
	
	[Benchmark]
	public void RecyclableStream()
	{
		using var stream = new RecyclableMemoryStream(RecyclingManager);
		WriteBytesTo(stream);
		ReadAllBytesFrom(stream);
	}

	[Benchmark]
	public void ResizingStream()
	{
		using var stream = new ByteBufferStream<ResizingBuffer<byte>>();
		WriteBytesTo(stream);
		ReadAllBytesFrom(stream);
	}
	
	[Benchmark]
	public void ChunkedStream()
	{
		using var stream = new ByteBufferStream<ChunkedBuffer<byte>>();
		WriteBytesTo(stream);
		ReadAllBytesFrom(stream);
	}
	
	internal void WriteBytesTo(Stream stream)
	{
		var left = Length;
		while (left > 0)
		{
			var chunk = Math.Min(left, _source.Length);
			stream.Write(_source, 0, chunk);
			left -= chunk;
		}
	}

	internal void ReadAllBytesFrom(Stream stream)
	{
		stream.Position = 0;
		var left = Length;
		while (left > 0)
		{
			var chunk = Math.Min(left, _target.Length);
			var read = stream.Read(_target, 0, chunk);
			left -= read;
		}
	}
}