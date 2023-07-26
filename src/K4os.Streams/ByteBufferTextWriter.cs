using System.Runtime.CompilerServices;
using System.Text;
using K4os.Streams.Buffers;
using K4os.Streams.Internal;

namespace K4os.Streams;

public class ByteBufferTextWriter<TByteBuffer>: TextWriter
	where TByteBuffer: struct, IBuffer<byte>
{
	internal unsafe struct CharArray
	{
		public const int Size = 256;
		public fixed char Data[Size];
	}

	// it is not technically readonly
	// ReSharper disable once FieldCanBeMadeReadOnly.Local
	private CharArray _internal = default;
	private int _offset;
	
	private TByteBuffer _bytes;
	private readonly Encoding _encoding;
	private readonly int _charArrayEncodedSize;

	public override Encoding Encoding => _encoding;
	protected ref TByteBuffer Bytes => ref _bytes;

	public ByteBufferTextWriter(IFormatProvider? formatProvider, Encoding? encoding):
		base(formatProvider)
	{
		Polyfills.Noop(_internal);
		_encoding = encoding ??= Encoding.UTF8;
		_charArrayEncodedSize = encoding.GetMaxByteCount(CharArray.Size);
	}

	public ByteBufferTextWriter(IFormatProvider? formatProvider):
		this(formatProvider, null) { }

	public ByteBufferTextWriter(Encoding? encoding):
		this(null, encoding) { }

	public ByteBufferTextWriter(): this(null, null) { }

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private void Write(ReadOnlySpan<byte> source) =>
		Bytes.Write(source);

	public override unsafe void Write(char value) =>
		WriteSpan(new Span<char>(&value, 1));

	public override void Write(string? value) =>
		WriteSpan(value.AsSpan());

	public override void Write(char[]? buffer) =>
		WriteSpan(buffer.AsSpan());

	public override void Write(char[] buffer, int index, int count) =>
		WriteSpan(buffer.AsSpan(index, count));

	private unsafe void WriteSpan(ReadOnlySpan<char> source)
	{
		fixed (char* dataP = _internal.Data)
			_offset = WriteSpan(source, new Span<char>(dataP, CharArray.Size), _offset);
	}

	private int WriteSpan(ReadOnlySpan<char> source, Span<char> target, int offset)
	{
		while (!source.IsEmpty)
		{
			if (offset >= CharArray.Size)
			{
				FlushSpan(offset);
				offset = 0;
			}

			var chunk = Math.Min(source.Length, CharArray.Size - offset);
			source.Slice(0, chunk).CopyTo(target.Slice(offset, chunk));
			source = source.Slice(chunk);
			offset += chunk;
		}

		return offset;
	}

	private unsafe void FlushSpan(int used)
	{
		Span<byte> buffer = stackalloc byte[_charArrayEncodedSize];
		fixed (char* dataP = _internal.Data)
		{
			var source = new Span<char>(dataP, used);
			var bytes = Encode(source, buffer);
			Write(buffer.Slice(0, bytes));
		}
	}

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
		// It would be tempting to use stackalloc here, but it is not possible
		// as StringBuilder.CopyTo requires actual array
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
	
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private int Encode(ReadOnlySpan<char> source, Span<byte> buffer) =>
		_encoding.GetBytes(source, buffer);
	
	#else
	
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private unsafe int Encode(ReadOnlySpan<char> source, Span<byte> target)
	{
		fixed (char* sourceP = source)
		fixed (byte* targetP = target)
			return _encoding.GetBytes(sourceP, source.Length, targetP, target.Length);
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

	public override void Flush()
	{
		FlushSpan(_offset);
		_offset = 0;
	}

	public override Task FlushAsync()
	{
		Flush();
		return Task.CompletedTask;
	}

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