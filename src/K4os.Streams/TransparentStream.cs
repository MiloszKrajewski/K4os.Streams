namespace K4os.Streams;

using System;

/// <summary>
/// Generic stream which passes all calls to internal stream.
/// Used as a base to Streams slightly modifying behavior
/// if other streams.
/// </summary>
public abstract class TransparentStream: Stream
{
	private Stream _stream;

	/// <summary>Gets the inner stream.</summary>
	/// <value>The inner stream.</value>
	public Stream InnerStream { get => _stream; protected set => _stream = value; }

	/// <summary>
	/// Initializes a new instance of the <see cref="TransparentStream"/> class.
	/// Initializes inner stream.
	/// </summary>
	/// <param name="innerStream">The inner stream.</param>
	protected TransparentStream(Stream innerStream)
	{
		_stream = innerStream ?? throw new ArgumentNullException(
			nameof(innerStream), "innerStream is null.");
	}

	/// <inheritdoc/>
	public override bool CanRead => _stream.CanRead;

	/// <inheritdoc/>
	public override bool CanSeek => _stream.CanSeek;

	/// <inheritdoc/>
	public override bool CanTimeout => _stream.CanTimeout;

	/// <inheritdoc/>
	public override bool CanWrite => _stream.CanWrite;

	/// <inheritdoc/>
	public override long Length => _stream.Length;

	/// <inheritdoc/>
	public override long Position { get => _stream.Position; set => _stream.Position = value; }

	/// <inheritdoc/>
	public override int ReadTimeout
	{
		get => _stream.ReadTimeout;
		set => _stream.ReadTimeout = value;
	}

	/// <inheritdoc/>
	public override int WriteTimeout
	{
		get => _stream.WriteTimeout;
		set => _stream.WriteTimeout = value;
	}

	/// <inheritdoc/>
	protected override void Dispose(bool disposing) => 
		_stream.Dispose();

	/// <inheritdoc/>
	public override void Flush() =>
		_stream.Flush();

	/// <inheritdoc/>
	public override int Read(byte[] buffer, int offset, int count) =>
		_stream.Read(buffer, offset, count);

	/// <inheritdoc/>
	public override int ReadByte() =>
		_stream.ReadByte();

	/// <inheritdoc/>
	public override long Seek(long offset, SeekOrigin origin) =>
		_stream.Seek(offset, origin);

	/// <inheritdoc/>
	public override void SetLength(long value) =>
		_stream.SetLength(value);

	/// <inheritdoc/>
	public override void Write(byte[] buffer, int offset, int count) =>
		_stream.Write(buffer, offset, count);

	/// <inheritdoc/>
	public override void WriteByte(byte value) =>
		_stream.WriteByte(value);

	/// <inheritdoc/>
	public override Task FlushAsync(CancellationToken cancellationToken) =>
		_stream.FlushAsync(cancellationToken);

	/// <inheritdoc/>
	public override Task<int> ReadAsync(
		byte[] buffer, int offset, int count, CancellationToken cancellationToken) =>
		_stream.ReadAsync(buffer, offset, count, cancellationToken);

	/// <inheritdoc/>
	public override Task WriteAsync(
		byte[] buffer, int offset, int count, CancellationToken cancellationToken) =>
		_stream.WriteAsync(buffer, offset, count, cancellationToken);

	/// <inheritdoc/>
	public override Task CopyToAsync(
		Stream destination, int bufferSize, CancellationToken cancellationToken) =>
		_stream.CopyToAsync(destination, bufferSize, cancellationToken);

	/// <inheritdoc/>
	public override void Close() => _stream.Close();
	
	#if NETSTANDARD2_1 || NET5_0_OR_GREATER

	/// <inheritdoc/>
	public override int Read(Span<byte> buffer) =>
		_stream.Read(buffer);

	/// <inheritdoc/>
	public override void Write(ReadOnlySpan<byte> buffer) =>
		_stream.Write(buffer);

	/// <inheritdoc/>
	public override ValueTask<int> ReadAsync(
		Memory<byte> buffer, CancellationToken cancellationToken = default) =>
		_stream.ReadAsync(buffer, cancellationToken);

	/// <inheritdoc/>
	public override ValueTask WriteAsync(
		ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default) =>
		_stream.WriteAsync(buffer, cancellationToken);

	/// <inheritdoc/>
	public override void CopyTo(Stream destination, int bufferSize) =>
		_stream.CopyTo(destination, bufferSize);

	/// <inheritdoc/>
	public override ValueTask DisposeAsync() =>
		_stream.DisposeAsync();

	#endif
}