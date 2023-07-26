using System.Text;
using K4os.Streams.Buffers;

namespace K4os.Streams;

public class CharBufferTextWriter<TCharBuffer>: TextWriter
	where TCharBuffer: struct, IBuffer<char>
{
	private TCharBuffer _bytes;

	public override Encoding Encoding => Encoding.Unicode;
	protected ref TCharBuffer Bytes => ref _bytes;

	public CharBufferTextWriter(IFormatProvider? formatProvider):
		base(formatProvider) { }

	public CharBufferTextWriter(): this(null) { }

	public override unsafe void Write(char value) =>
		WriteSpan(new Span<char>(&value, 1));

	public override void Write(string? value) =>
		WriteSpan(value.AsSpan());

	public override void Write(char[]? buffer) =>
		WriteSpan(buffer.AsSpan());

	public override void Write(char[] buffer, int index, int count) =>
		WriteSpan(buffer.AsSpan(index, count));

	private void WriteSpan(ReadOnlySpan<char> source) => 
		Bytes.Write(source);

	#if NET5_0_OR_GREATER
	
	public override void Write(StringBuilder? value)
	{
		if (value is null) return;

		foreach (var chunk in value.GetChunks())
		{
			WriteSpan(chunk.Span);
		}
	}
	
	#else
	
	public virtual void Write(StringBuilder? source)
	{
		if (source is null) return;

		var sourceLength = source.Length;
		var target = PooledArray<char>.AllocateChunk(sourceLength);
		var targetLength = target.Length;

		var offset = 0;
		while (offset < sourceLength)
		{
			var chunk = Math.Min(sourceLength - offset, targetLength);
			source.CopyTo(offset, target, 0, chunk);
			WriteSpan(target.AsSpan(0, chunk));
			offset += chunk;
		}
		
		PooledArray<char>.Recycle(target);
	}
	
	#endif

	#if NETSTANDARD2_1_OR_GREATER || NET5_0_OR_GREATER
	override
	#else
	virtual
	#endif
		public void Write(ReadOnlySpan<char> buffer) =>
		WriteSpan(buffer);

	public override void WriteLine(string? value)
	{
		// base class implementation is copying string to temporary buffer in .NET 4.6
		// base.WriteLine(value);
		Write(value);
		WriteLine();
	}

	#if NETSTANDARD2_1_OR_GREATER || NET5_0_OR_GREATER
	override
	#else
	virtual
	#endif
		public void WriteLine(ReadOnlySpan<char> buffer)
	{
		// base class implementation is copying string to temporary buffer
		// base.WriteLine(value);
		Write(buffer);
		WriteLine();
	}

	protected override void Dispose(bool disposing)
	{
		if (disposing)
		{
			_bytes.Dispose();
		}
	}

	public override void Flush() { }

	public override Task FlushAsync() => Task.CompletedTask;

	public override Task WriteAsync(char value)
	{
		Write(value);
		return Task.CompletedTask;
	}

	#if NETSTANDARD2_1_OR_GREATER || NET5_0_OR_GREATER
	override
	#else
	virtual
	#endif
		public Task WriteAsync(
			ReadOnlyMemory<char> buffer, CancellationToken cancellationToken = default)
	{
		Write(buffer.Span);
		return Task.CompletedTask;
	}

	public override Task WriteAsync(char[] buffer, int index, int count)
	{
		Write(buffer, index, count);
		return Task.CompletedTask;
	}

	public override Task WriteAsync(string? value)
	{
		Write(value);
		return Task.CompletedTask;
	}

	#if NET5_0_OR_GREATER
	override
	#else
	virtual
	#endif
		public Task WriteAsync(StringBuilder? value, CancellationToken cancellationToken = default)
	{
		Write(value);
		return Task.CompletedTask;
	}

	public override Task WriteLineAsync()
	{
		WriteLine();
		return Task.CompletedTask;
	}

	#if NET5_0_OR_GREATER
	override
	#else
	virtual
	#endif
		public Task WriteLineAsync(
			StringBuilder? value, CancellationToken cancellationToken = default)
	{
		WriteLine(value);
		return Task.CompletedTask;
	}

	public override Task WriteLineAsync(string? value)
	{
		WriteLine(value);
		return Task.CompletedTask;
	}

	#if NETSTANDARD2_1_OR_GREATER || NET5_0_OR_GREATER
	override
	#else
	virtual
	#endif
		public Task WriteLineAsync(
			ReadOnlyMemory<char> buffer, CancellationToken cancellationToken = default)
	{
		WriteLine(buffer.Span);
		return Task.CompletedTask;
	}

	public override Task WriteLineAsync(char value)
	{
		WriteLine(value);
		return Task.CompletedTask;
	}

	public override Task WriteLineAsync(char[] buffer, int index, int count)
	{
		WriteLine(buffer, index, count);
		return Task.CompletedTask;
	}

	/*
	// There is nothing to add to baseline implementation 
	public override void WriteLine(char[] buffer, int index, int count) { base.WriteLine(buffer, index, count); }
	public override void WriteLine(StringBuilder? value) { base.WriteLine(value); }
	public override void WriteLine(char[]? buffer) { base.WriteLine(buffer); }
	*/
}
