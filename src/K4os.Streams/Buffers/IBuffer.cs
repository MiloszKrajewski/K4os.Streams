namespace K4os.Streams.Buffers;

/// <summary>
/// Abstraction over byte buffer allowing sequential reading and writing.
/// Like <see cref="Stream"/> but an `interface` and much smaller.
/// </summary>
public interface IBuffer<TItem>: IDisposable
{
	/// <summary>Allows to get and set current position.</summary>
	long Position { get; set; }
	
	/// <summary>Allows to get and set length of the buffer.
	/// Truncating buffer may adjust current position, while extending the buffer may
	/// file buffer with random data.</summary>
	long Length { get; set; }

	/// <summary>Reads data from the buffer.</summary>
	/// <param name="target">Target buffer to store data into.</param>
	/// <returns>Number of bytes actually read.</returns>
	int Read(Span<TItem> target);
	
	/// <summary>Writes data to the buffer.</summary>
	/// <param name="source">Source buffer to take data from.</param>
	/// <returns>Number of bytes actually written.
	/// Note: it is relatively safe to assume all data has been written.</returns>
	int Write(ReadOnlySpan<TItem> source);
	
	/// <summary>
	/// Export whole content of the buffer to the target span. If target span is too small
	/// then data will be truncated, but not exception will be thrown.
	/// </summary>
	/// <param name="target">Target buffer.</param>
	/// <returns>Number of bytes actually exported.</returns>
	int ExportTo(Span<TItem> target);
}
