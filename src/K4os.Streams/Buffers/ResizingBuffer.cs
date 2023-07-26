using System.Runtime.CompilerServices;
using K4os.Streams.Internal;

namespace K4os.Streams.Buffers;

/// <summary>
/// Byte buffer implementation which stores data in a single array.
/// It allows to store data up to 2GB, and is very fast up to certain size.
/// For bigger stream, I would suggest using <see cref="ChunkedBuffer{T}"/>.
/// </summary>
public partial struct ResizingBuffer<TItem>: IBuffer<TItem> where TItem: unmanaged
{
	private TItem[]? _block0;

	private long _length;
	private long _capacity;
	private long _position;
	
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
		ClearBlock0();

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private long Expand(long length) =>
		ExpandBlock0(length);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private long Shrink(long length) =>
		ShrinkBlock0(length);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private long Seek(long position) =>
		SeekBlock0(position);

	/// <summary>Reads data from the buffer.</summary>
	/// <param name="target">Target span of bytes.</param>
	/// <returns>Number fo bytes actually read.</returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public int Read(Span<TItem> target) =>
		ReadFromBlock0(target);
	
	/// <summary>Writes data to the buffer.</summary>
	/// <param name="source">Source span of bytes.</param>
	/// <returns>Number of bytes written.</returns>
	public int Write(ReadOnlySpan<TItem> source)
	{
		TryExpandBeforeWrite(_position + source.Length);
		return WriteAfterExpansion(source);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private int WriteAfterExpansion(ReadOnlySpan<TItem> source) =>
		WriteToBlock0(source);
	
	/// <summary>
	/// Exports all data to provided buffer. Please note, it returns number of bytes written,
	/// as does not throw if te buffer is too small, it will just not export remaining data.
	/// </summary>
	/// <param name="target">Target buffer.</param>
	/// <returns>Number of bytes actually exported.</returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public int ExportTo(Span<TItem> target) =>
		ExportBlock0To(target);

	/// <inheritdoc />
	public void Dispose() => Clear();

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private long SeekBlock0(long position) =>
		_position = position;

	private long ClearBlock0()
	{
		PooledArray<TItem>.Recycle(_block0);
		_block0 = null;
		return _length = _capacity = _position = 0;
	}

	private long ExpandBlock0(long length)
	{
		var expectedCapacity = PooledArray<TItem>.ArraySize(length);
		var usedBytes = (int)_length;
		var block = PooledArray<TItem>.Reallocate(_block0, usedBytes, expectedCapacity);
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
			var expectedCapacity = PooledArray<TItem>.ArraySize(length);
			var usedBytes = (int)length; // min(length, _length) but it is definitely shrinking
			var block = PooledArray<TItem>.Reallocate(_block0, usedBytes, expectedCapacity);
			_block0 = block;
			_capacity = block.Length;
		}

		return _length = length;
	}
	
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private int ReadFromBlock0(Span<TItem> target)
	{
		var source = Readable0();
		var length = Math.Min(source.Length, target.Length);
		source.Slice(0, length).CopyTo(target);
		_position += length;
		return length;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private Span<TItem> Readable0() =>
		_block0.AsSpan((int)_position, (int)(_length - _position));

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private int WriteToBlock0(ReadOnlySpan<TItem> source)
	{
		var target = Writable0();
		var length = source.Length;
		source.CopyTo(target);
		_position += length;
		return length;
	}
	
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private Span<TItem> Writable0() =>
		_block0.AsSpan((int)_position, (int)(_capacity - _position));

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private int ExportBlock0To(Span<TItem> target)
	{
		var source = _block0.AsSpan(0, (int)_length);
		var length = Math.Min(source.Length, target.Length);
		source.Slice(0, length).CopyTo(target);
		return length;
	}
}
