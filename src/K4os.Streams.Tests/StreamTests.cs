using System.Diagnostics;
using System.Security.Cryptography;
using K4os.Streams.Buffers;
using Xunit;

namespace K4os.Streams.Test;

public class ResizingStreamTests: StreamTests<ByteBufferStream<ResizingBuffer<byte>>> { }

public class ChunkedStreamTests: StreamTests<ByteBufferStream<ChunkedBuffer<byte>>> { }

public abstract class StreamTests<TStream> where TStream: Stream, new()
{
	[Fact]
	public void EmptyStreamIsEmpty()
	{
		var stream = new TStream();

		Assert.Equal(0, stream.Length);
		Assert.Equal(0, stream.Position);

		var buffer = new byte[1024];
		var read = stream.Read(buffer, 0, buffer.Length);

		Assert.Equal(0, read);
		Assert.Equal(0, stream.Position);

		stream.Position = 0;

		Assert.Equal(0, stream.Position);
		Assert.Equal(0, stream.Length);
	}

	[Fact]
	public void YouCanWriteFewBytesToStream()
	{
		var stream = new TStream();

		var buffer = new byte[1024];
		new Random().NextBytes(buffer);

		stream.Write(buffer, 0, 100);

		Assert.Equal(100, stream.Length);
		Assert.Equal(100, stream.Position);
	}

	[Fact]
	public void BytesWrittenCanBeReadBack()
	{
		var stream = new TStream();

		var source = new byte[1024];
		new Random().NextBytes(source);

		stream.Write(source, 0, 100);
		stream.Position = 0;

		var target = new byte[1024];
		var read = stream.Read(target, 0, 100);

		Assert.Equal(100, read);

		CompareBuffers(source.AsSpan(0, 100), target.AsSpan(0, 100));
	}

	[Theory]
	[InlineData(1673)]
	[InlineData(1337 * 1024 + 1337)]
	[InlineData(12 * 1024 * 1024 + 1337)]
	public void WriteVeryLargeBuffer(int length)
	{
		var stream = new TStream();

		var source = new byte[length];
		new Random().NextBytes(source);

		stream.Write(source, 0, source.Length);

		Assert.Equal(source.Length, stream.Length);
		Assert.Equal(source.Length, stream.Position);

		stream.Position = 0;

		var target = new byte[length];
		var read = stream.Read(target, 0, target.Length);

		Assert.Equal(target.Length, read);

		CompareBuffers(source, target);
	}

	[Theory]
	[InlineData(1673)]
	[InlineData(1337 * 1024 + 1337)]
	[InlineData(12 * 1024 * 1024 + 1337)]
	public void ReadStreamWithRandomAccess(int length)
	{
		var stream = new TStream();

		var source = new byte[length];
		new Random().NextBytes(source);

		var target = new byte[0x10000];

		stream.Write(source, 0, source.Length);
		var random = new Random(0);

		var limit = Math.Max(1000, length >> 8);
		for (var i = 0; i < limit; i++)
		{
			var position = random.Next(0, length);
			var chunk = Math.Min(random.Next(1, target.Length), length - position);
			if (chunk <= 0) continue;

			stream.Position = position;
			var read = stream.Read(target, 0, chunk);

			Assert.Equal(chunk, read);

			CompareBuffers(source.AsSpan(position, chunk), target.AsSpan(0, chunk));
		}
	}

	[Theory]
	[InlineData(1673)]
	[InlineData(1337 * 1024 + 1337)]
	[InlineData(12 * 1024 * 1024 + 1337)]
	public void ReadStreamAtRandom(int length)
	{
		var stream = new TStream();

		var source = new byte[length];
		new Random().NextBytes(source);

		var random = new Random(0);
		var position = 0;
		var left = source.Length;

		while (left > 0)
		{
			var chunk = Math.Min(random.Next(1, 1337), left);
			stream.Write(source, position, chunk);
			position += chunk;
			left -= chunk;
		}

		CompareStreams(new MemoryStream(source), stream);
	}

	[Theory]
	[InlineData(1000, 500)]
	[InlineData(16384, 256)]
	[InlineData(8 * 1024 * 1024, 16)]
	public void StreamCanBeTrimmed(int length, int checkpoint)
	{
		var stream = new TStream();

		var source = new byte[length];
		new Random().NextBytes(source);

		stream.Write(source, 0, source.Length);

		Debug.Assert(stream.Length == length);
		Debug.Assert(stream.Position == length);

		stream.SetLength(checkpoint);

		Debug.Assert(stream.Length == checkpoint);
		Debug.Assert(stream.Position == checkpoint);
	}

	[Theory]
	[InlineData(1000, 500)]
	[InlineData(16384, 256)]
	[InlineData(8 * 1024 * 1024, 16)]
	[InlineData(8 * 1024 * 1024, 65536)]
	public void WriteToTrimmedStream(int length, int checkpoint)
	{
		var stream = new TStream();

		var source = new byte[length];
		new Random().NextBytes(source);

		stream.Write(source, 0, source.Length);

		Assert.Equal(length, stream.Length);
		Assert.Equal(length, stream.Position);

		stream.SetLength(checkpoint);

		Assert.Equal(checkpoint, stream.Length);
		Assert.Equal(checkpoint, stream.Position);

		stream.Write(source, 0, source.Length);

		Assert.Equal(length + checkpoint, stream.Length);
		Assert.Equal(length + checkpoint, stream.Position);
	}

	[Theory]
	[InlineData(0, 0x10000, 1000)]
	[InlineData(0, 1024*1024, 1000)]
	[InlineData(0, 0x10000 + 3247, 1000)]
	[InlineData(0, 512*1024*1024, 10000)]
	public void RandomSeekAndRead(int seed, int length, int count)
	{
		var a = new TStream();
		var b = new MemoryStream();
		
		var random = new Random(seed);

		Apply(a, b, s => WriteRandomBytes(s, 1337, length));

		for (var i = 0; i < count; i++)
		{
			var position = random.Next(0, length);
			Apply(a, b, s => s.Position = position);
			Apply(a, b, s => s.Position);
			
			var chunk = random.Next(1, 1337);
			Apply(a, b, s => HashBytes(ReadRandomBytes(s, 2432, chunk)));
		}
	}

	private static void WriteRandomBytes(Stream stream, int seed, int length)
	{
		var random = new Random(seed);
		var buffer = new byte[1024];
		var left = length;
		while (left > 0)
		{
			var chunk = random.Next(Math.Min(left, buffer.Length)) + 1;
			random.NextBytes(buffer);
			stream.Write(buffer, 0, chunk);
			left -= chunk;
		}
	}
	
	private static byte[] ReadRandomBytes(Stream stream, int seed, int length)
	{
		var random = new Random(seed);
		var buffer = new byte[length];
		var offset = 0;
		var left = length;
		while (left > 0)
		{
			var chunk = random.Next(Math.Min(left, buffer.Length)) + 1;
			var read = stream.Read(buffer, offset, chunk);
			if (read == 0) break;
			offset += read;
			left -= read;
		}
		
		if (left > 0)
		{
			var result = new byte[offset];
			Buffer.BlockCopy(buffer, 0, result, 0, offset);
			buffer = result;
		}

		return buffer;
	}
	
	private static Guid HashBytes(byte[] buffer) => 
		new(MD5.Create().ComputeHash(buffer));

	private static void Apply(Stream s, Stream t, Action<Stream> action)
	{
		action(s);
		action(t);
	}

	private static T Apply<T>(Stream s, Stream t, Func<Stream, T> action)
	{
		var sr = action(s);
		var tr = action(t);
		Assert.Equal(sr, tr);
		return sr;
	}

	private static void CompareStreams(Stream a, Stream b)
	{
		Assert.Equal(a.Length, b.Length);
		var length = a.Length;
		a.Position = 0;
		b.Position = 0;

		var bufferA = new byte[length];
		var bufferB = new byte[length];

		var readA = a.Read(bufferA, 0, bufferA.Length);
		var readB = b.Read(bufferB, 0, bufferB.Length);

		Assert.Equal(readA, readB);
		Assert.Equal(readA, length);
		Assert.Equal(readB, length);

		CompareBuffers(bufferA, bufferB);
	}

	private static void CompareBuffers(ReadOnlySpan<byte> a, ReadOnlySpan<byte> b)
	{
		Assert.True(a.Length == b.Length, "Buffers don't have equal length");
		for (var i = 0; i < a.Length; i++)
		{
			var equal = a[i] == b[i];
			if (!equal) Assert.Fail($"Buffers are different at {i}");
		}
	}
}
