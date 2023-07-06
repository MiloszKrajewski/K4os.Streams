using K4os.Streams.Buffers;

namespace K4os.Streams;

/// <summary>
/// Adapter for <see cref="IByteBuffer"/> to expose functionality like <see cref="Stream"/>.
/// </summary>
/// <typeparam name="TState"><see cref="IByteBuffer"/></typeparam>
public class ByteBufferStreamAdapter<TState>: Stream 
	where TState: struct, IByteBuffer
{
	private TState _bytes;
	
	/// <summary>Internal <see cref="IByteBuffer"/> instance.</summary>
	protected ref TState Bytes => ref _bytes;

	/// <inheritdoc />
	public override bool CanTimeout => false;
	
	/// <inheritdoc />
	public override bool CanRead => true;
	
	/// <inheritdoc />
	public override bool CanSeek => true;
	
	/// <inheritdoc />
	public override bool CanWrite => true;
	
	/// <inheritdoc />
	public override long Length => Bytes.Length;

	/// <inheritdoc />
	public override long Position { get => Bytes.Position; set => Bytes.Position = value; }

	/// <inheritdoc />
	public override void Flush() { }
	
	/// <inheritdoc />
	public override Task FlushAsync(CancellationToken cancellationToken) => Task.CompletedTask;

	/// <inheritdoc />
	public override Task<int> ReadAsync(
		byte[] buffer, int offset, int count, CancellationToken cancellationToken) =>
		Task.FromResult(Read(buffer, offset, count));

	/// <inheritdoc />
	public override int ReadByte()
	{
		Span<byte> buffer = stackalloc byte[1];
		return Bytes.Read(buffer) switch { <= 0 => -1, _ => buffer[0] };
	}

	/// <inheritdoc />
	public override Task WriteAsync(
		byte[] buffer, int offset, int count, CancellationToken cancellationToken)
	{
		Write(buffer, offset, count);
		return Task.CompletedTask;
	}

	/// <inheritdoc />
	public override void WriteByte(byte value)
	{
		Span<byte> buffer = stackalloc byte[1];
		buffer[0] = value;
		Bytes.Write(buffer);
	}

	/// <inheritdoc />
	public override long Seek(long offset, SeekOrigin origin)
	{
		var baseline = origin switch {
			SeekOrigin.Current => Bytes.Position,
			SeekOrigin.End => Bytes.Length,
			_ => 0,
		};
		Bytes.Position = baseline + offset;
		return Bytes.Position;
	}

	/// <inheritdoc />
	public override void SetLength(long value) =>
		Bytes.Length = Math.Max(0, value);

	/// <inheritdoc />
	public override int Read(byte[] buffer, int offset, int count) =>
		Bytes.Read(buffer.AsSpan(offset, count));

	/// <inheritdoc />
	public override void Write(byte[] buffer, int offset, int count) =>
		Bytes.Write(buffer.AsSpan(offset, count));

	/// <inheritdoc />
	protected override void Dispose(bool disposing)
	{
		if (disposing)
		{
			Bytes.Dispose();
		}

		base.Dispose(disposing);
	}

	#if NETSTANDARD2_1_OR_GREATER || NET5_0_OR_GREATER

	/// <inheritdoc />
	public override int Read(Span<byte> buffer) => Bytes.Read(buffer);

	/// <inheritdoc />
	public override void Write(ReadOnlySpan<byte> buffer) => Bytes.Write(buffer);

	/// <inheritdoc />
	public override ValueTask<int> ReadAsync(
		Memory<byte> buffer, CancellationToken cancellationToken = default) =>
		new(Read(buffer.Span));

	/// <inheritdoc />
	public override ValueTask WriteAsync(
		ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
	{
		Write(buffer.Span);
		return default;
	}

	#endif

	/// <summary>
	/// Exports content of this stream to <paramref name="target"/> span.
	/// Returns number of bytes actually exported.
	/// If <paramref name="target"/> is too small, only part of the stream will be exported,
	/// but no exception will be thrown.
	/// </summary>
	/// <param name="target">Target span.</param>
	/// <returns>Number of bytes exported.</returns>
	public int ExportTo(Span<byte> target) => Bytes.ExportTo(target);

	/// <summary>Exports content of this stream to new array.
	/// Please note, this involves memory allocation for new array the same way
	/// <see cref="MemoryStream"/> is doing it. if you want to avoid it,
	/// you should rather use <see cref="ExportTo"/> method.
	/// </summary>
	/// <returns>New array with all stream content.</returns>
	public byte[] ToArray()
	{
		var array = new byte[Bytes.Length];
		Bytes.ExportTo(array);
		return array;
	}
}