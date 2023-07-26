using System.Diagnostics;
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
public partial struct ChunkedBuffer<TItem> where TItem: unmanaged
{
	internal readonly record struct Chunk(TItem[] Data, long Start)
	{
		public const int SizeOfT = sizeof(long) + sizeof(long);
		public static readonly int MinimumAllocatedSize =
			PooledArray<byte>.MinimumAllocatedSize / SizeOfT;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool Contains(long position) => (ulong)(position - Start) <= (ulong)Data.Length;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Span<TItem> SpanAt(long position) => Data.AsSpan((int)(position - Start));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private long SeekChunks(long position)
	{
		Debug.Assert(_chunks is not null);

		var chunks = _chunks!;
		var count = chunks.Count;

		if (position <= 0)
		{
			// this is quite common case, so let's handle it separately
			_index = 0;
			return _position = 0;
		}

		if (position >= _length)
		{
			Debug.Assert(count > 0);

			// this would not be found using .Contains() method,
			// because it is technically not contained by last chunk
			_index = count - 1;
			return _position = _length;
		}

		var index = _index;

		if (index < count && chunks[index].Contains(position))
		{
			// this is slightly less common case, but quite cheap as it does not require scanning
			return _position = position;
		}

		const int binarySearchThreshold = 8;

		return count >= binarySearchThreshold
			? SeekChunksBinary(position)
			: SeekChunksScan(position);
	}

	private long SeekChunksScan(long position)
	{
		Debug.Assert(_chunks is not null);

		var chunks = _chunks!;
		var count = chunks.Count;

		Debug.Assert(count > 0);

		var index = count - 1;
		while (index >= 0 && !chunks[index].Contains(position))
		{
			--index;
		}

		_index = index;
		return _position = position;
	}

	private long SeekChunksBinary(long position)
	{
		Debug.Assert(_chunks is not null);

		var chunks = _chunks!;
		var count = chunks.Count;

		Debug.Assert(count > 0);

		var lo = 0;
		var hi = count - 1;

		while (lo <= hi)
		{
			var mid = lo + (hi - lo) / 2;

			var chunk = chunks[mid];
			if (chunk.Contains(position))
			{
				_index = mid;
				return _position = position;
			}

			if (position < chunk.Start)
			{
				hi = mid - 1;
			}
			else
			{
				lo = mid + 1;
			}
		}

		// this is the "not found" part of binary search
		// but in this case it cannot be "not found" as chunks are contiguous
		// and "before start" and "after end" cases are handled separately
		// in a sense it can still happen if someone tries to write to this stream
		// concurrently as it does not have locks (for performance reasons)
		return new ArgumentOutOfRangeException(
			nameof(position), $"Data stream is corrupted, could not find position {position}"
		).Throw<long>();
	}

	private long ClearChunks()
	{
		Debug.Assert(_chunks is not null);

		var chunks = _chunks!;
		var count = chunks.Count;

		for (var i = 0; i < count; ++i)
		{
			PooledArray<TItem>.Recycle(chunks[i].Data);
		}

		_chunks = null;
		_index = 0;

		return _length = _capacity = _position = 0;
	}

	private long ExpandChunks(long length)
	{
		var chunks = EnsureChunks();

		var count = chunks.Count;
		var size = count > 0 ? chunks[count - 1].Data.Length : 0;

		var capacity = _capacity;
		while (capacity < length)
		{
			var bytes = PooledArray<TItem>.Allocate(NextChunkSize(size));
			chunks.Add(new Chunk(bytes, capacity));
			size = bytes.Length;
			capacity += size;
		}

		_capacity = capacity;
		return _length = length;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static int NextChunkSize(int lastChunkSize) =>
		Polyfills.Clamp(
			(int)Polyfills.RoundUpPow2((uint)lastChunkSize << 1),
			MaximumBlock0Size, PooledArray<TItem>.MaximumPooledSize);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private PooledList<Chunk> EnsureChunks() =>
		_chunks ?? InitializeChunks();

	private PooledList<Chunk> InitializeChunks()
	{
		Debug.Assert(_chunks is null);

		// var chunks = new List<Chunk>(Chunk.MIN_ARRAY_SIZE);
		var chunks = new PooledList<Chunk>(Chunk.MinimumAllocatedSize);

		var block0 = _block0;
		if (block0 is not null)
		{
			chunks.Add(new Chunk(block0, 0));
			_block0 = null;
			_index = 0;
		}

		return _chunks = chunks;
	}

	private long ShrinkChunks(long length)
	{
		Debug.Assert(_chunks is not null);

		var chunks = _chunks!;
		var count = chunks.Count;
		var index = count - 1;

		var capacity = _capacity;
		while (index >= 0)
		{
			var (bytes, start) = chunks[index];
			if (start < length)
			{
				break;
			}

			var size = bytes.Length;
			capacity -= size;
			PooledArray<TItem>.Recycle(bytes);
			index--;
		}

		// chunks.RemoveRange(index + 1, count - index - 1);
		chunks.TruncateAt(index + 1);

		if (_position >= length)
		{
			_position = length;
			_index = index;
		}

		_capacity = capacity;

		return _length = length;
	}

	private int WriteToChunks(ReadOnlySpan<TItem> source)
	{
		Debug.Assert(_chunks is not null);

		var chunks = _chunks!;

		var position = _position;
		var index = _index - 1;
		var left = source.Length;
		var total = 0;

		Debug.Assert(left >= 0);
		Debug.Assert(_position + left <= _length);

		do
		{
			++index;

			// TODO: this can have more micro-optimizations as only first chunk needs .SpanAt the rest start @ 0
			var current = chunks[index];
			var target = current.SpanAt(position);
			var length = Math.Min(left, target.Length);
			source.Slice(0, length).CopyTo(target);
			source = source.Slice(length);
			position += length;
			total += length;
			left -= length;
		}
		while (left > 0);

		_index = index;
		_position = position;

		return total;
	}

	private int ReadFromChunks(Span<TItem> target)
	{
		Debug.Assert(_chunks is not null);

		var chunks = _chunks!;

		var position = _position;
		var index = _index - 1;
		var left = (int)Math.Min(target.Length, _length - position);
		var total = 0;

		Debug.Assert(left >= 0);
		Debug.Assert(_position + left <= _length);

		do
		{
			++index;

			// TODO: this can have more micro-optimizations as only first chunk needs .SpanAt the rest start @ 0
			var current = chunks[index];
			var source = current.SpanAt(position);
			var length = Math.Min(source.Length, left);
			source.Slice(0, length).CopyTo(target);
			target = target.Slice(length);
			position += length;
			total += length;
			left -= length;
		}
		while (left > 0);

		_index = index;
		_position = position;

		return total;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private int ExportChunksTo(Span<TItem> target)
	{
		Debug.Assert(_chunks is not null);

		var chunks = _chunks!;

		var index = -1;
		var left = (int)Math.Min(target.Length, _length);
		var total = 0;

		while (left > 0)
		{
			++index;

			var source = chunks[index].Data;
			var length = Math.Min(source.Length, left);
			source.AsSpan(0, length).CopyTo(target);
			target = target.Slice(length);
			total += length;
			left -= length;
		}

		return total;
	}
}
