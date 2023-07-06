namespace K4os.Streams.Buffers;

public partial struct ResizingByteBuffer
{
	/// <summary>
	/// Allows to peek at the buffer content without copying it.
	/// Please note, this is a dangerous operation as it exposes internal buffer which
	/// is pooled, so can be returned to the pool and reused by other components.
	/// </summary>
	/// <returns>Reference to internal buffer.</returns>
	public Span<byte> Peek() => 
		_block0 is null 
			? Span<byte>.Empty 
			: _block0.AsSpan(0, (int)_length);
}
