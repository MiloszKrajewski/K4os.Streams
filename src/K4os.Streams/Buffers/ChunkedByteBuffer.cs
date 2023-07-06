using System.Runtime.CompilerServices;
using K4os.Streams.Internal;

namespace K4os.Streams.Buffers;

/// <summary>
/// Byte buffer implementation which stores data in chunks.
/// It allows to store data larger than 2GB, without allocating large arrays,
/// for the price of a little bit more complex implementation.
/// Please note, this a struct to reduce memory allocations, but comes in with caveats
/// that state can be easily corrupted if not used properly.
/// </summary>
public partial struct ChunkedByteBuffer: IByteBuffer
{
	private byte[]? _block0;

	private long _length;
	private long _capacity;
	private long _position;
	
	// these belong to chunks, but need to be defined here to avoid warning CS0282
	private PooledList<Chunk>? _chunks;
	private int _index;

	/// <summary>Gets or sets length of the buffer. Buffer will be expanded or shrunk if needed.</summary>
	public long Length { get => _length; set => SetLength(value); }

	/// <summary>Gets or sets position in a buffer.</summary>
	public long Position { get => _position; set => SetPosition(value); }

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private long SetLength(long length) =>
		length > _capacity ? Expand(length) :
		length <= 0 ? Clear() :
		length < _length ? Shrink(length) :
		_length = length;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private long TryExpandBeforeWrite(long length) =>
		length > _capacity ? Expand(length) :
		length > _length ? _length = length :
		_length;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private void SetPosition(long position) =>
		Seek(Polyfills.Clamp(position, 0, _length));

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private long Clear() =>
		_chunks is not null
			? ClearChunks()
			: ClearBlock0();

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private long Expand(long length) =>
		_chunks is not null || length > MAX_BLOCK0_SIZE
			? ExpandChunks(length)
			: ExpandBlock0(length);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private long Shrink(long length) =>
		_chunks is not null
			? ShrinkChunks(length)
			: ShrinkBlock0(length);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private long Seek(long position) =>
		_chunks is not null
			? SeekChunks(position)
			: SeekBlock0(position);

	/// <summary>Reads data from the buffer.</summary>
	/// <param name="target">Target span of bytes.</param>
	/// <returns>Number fo bytes actually read.</returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public int Read(Span<byte> target) =>
		_chunks is not null
			? ReadFromChunks(target)
			: ReadFromBlock0(target);
	
	/// <summary>Writes data to the buffer.</summary>
	/// <param name="source">Source span of bytes.</param>
	/// <returns>Number of bytes written.</returns>
	public int Write(ReadOnlySpan<byte> source)
	{
		TryExpandBeforeWrite(_position + source.Length);
		return WriteAfterExpansion(source);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private int WriteAfterExpansion(ReadOnlySpan<byte> source) =>
		_chunks is not null
			? WriteToChunks(source)
			: WriteToBlock0(source);
	
	/// <summary>
	/// Exports all data to provided buffer. Please note, it returns number of bytes written,
	/// as does not throw if te buffer is too small, it will just not export remaining data.
	/// </summary>
	/// <param name="target">Target buffer.</param>
	/// <returns>Number of bytes actually exported.</returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public int ExportTo(Span<byte> target) =>
		_chunks is not null
			? ExportChunksTo(target)
			: ExportBlock0To(target);

	/// <inheritdoc />
	public void Dispose() => Clear();

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private long SeekBlock0(long position) =>
		_position = position;

	private long ClearBlock0()
	{
		ByteArray.Recycle(_block0);
		_block0 = null;
		return _length = _capacity = _position = 0;
	}

	private long ExpandBlock0(long length)
	{
		var expectedCapacity = ByteArray.ArraySize(length);
		var usedBytes = (int)_length;
		var block = ByteArray.Reallocate(_block0, usedBytes, expectedCapacity);
		_block0 = block;
		_capacity = block.Length;
		return _length = length;
	}

	private long ShrinkBlock0(long length)
	{
		// in case we are truncating buffer below current position
		if (length < _position)
		{
			_position = length;
		}

		// don't be too quick with shrinking arrays
		// it is relatively expensive operation, so let's delay it
		// until size drops below 1/3 of current capacity
		if (length <= _capacity / 3)
		{
			var expectedCapacity = ByteArray.ArraySize(length);
			var usedBytes = (int)length; // min(length, _length) but it is definitely shrinking
			var block = ByteArray.Reallocate(_block0, usedBytes, expectedCapacity);
			_block0 = block;
			_capacity = block.Length;
		}

		return _length = length;
	}
	
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private int ReadFromBlock0(Span<byte> target)
	{
		var source = Readable0();
		var length = Math.Min(source.Length, target.Length);
		source.Slice(0, length).CopyTo(target);
		_position += length;
		return length;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private Span<byte> Readable0() =>
		_block0.AsSpan((int)_position, (int)(_length - _position));

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private int WriteToBlock0(ReadOnlySpan<byte> source)
	{
		var target = Writable0();
		var length = source.Length;
		source.CopyTo(target);
		_position += length;
		return length;
	}
	
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private Span<byte> Writable0() =>
		_block0.AsSpan((int)_position, (int)(_capacity - _position));

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private int ExportBlock0To(Span<byte> target)
	{
		var source = _block0.AsSpan(0, (int)_length);
		var length = Math.Min(source.Length, target.Length);
		source.Slice(0, length).CopyTo(target);
		return length;
	}
}
