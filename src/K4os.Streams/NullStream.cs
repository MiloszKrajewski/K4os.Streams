namespace K4os.Streams;

/// <summary>
/// Null stream. Use <see cref="NullStream.Instance"/> to get singleton instance.
/// </summary>
public class NullStream: Stream
{
	/// <summary>Singleton instance of <see cref="NullStream"/>.</summary>
	public static readonly NullStream Instance = new();

	private NullStream() { }

	/// <inheritdoc />
	public override void Flush() { }

	/// <inheritdoc />
	public override int Read(byte[] buffer, int offset, int count) => 0;

	/// <inheritdoc />
	public override long Seek(long offset, SeekOrigin origin) => 0;

	/// <inheritdoc />
	public override void SetLength(long value) { }

	/// <inheritdoc />
	public override void Write(byte[] buffer, int offset, int count) { }

	/// <inheritdoc />
	public override bool CanRead => true;

	/// <inheritdoc />
	public override bool CanSeek => true;

	/// <inheritdoc />
	public override bool CanWrite => true;

	/// <inheritdoc />
	public override long Length => 0;

	/// <inheritdoc />
	public override long Position { get => 0; set { } }
}
