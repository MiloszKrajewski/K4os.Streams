using System.Buffers;
using System.Runtime.CompilerServices;
using K4os.Streams.Buffers;

namespace K4os.Streams.Internal;

/// <summary>
/// Minimalistic list implementation that uses pooled array for storage.
/// Replaces <see cref="IList{T}"/> in <see cref="ChunkedBuffer{T}"/>.
/// </summary>
/// <typeparam name="T">Type of item.</typeparam>
public class PooledList<T>
{
	private T[] _data;
	private int _count;
	private readonly int _capacity0;

	/// <summary>Number of items in the list.</summary>
	public int Count
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => _count;
	}

	private int Capacity
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => _data.Length;
	}

	/// <summary>Creates new instance of <see cref="PooledList{T}"/> with pre-allocated capacity.</summary>
	public PooledList(int capacity0)
	{
		_capacity0 = Math.Max(capacity0, 4);
		_data = ArrayPool<T>.Shared.Rent(_capacity0);
		_count = 0;
	}

	/// <summary>Access to list items.</summary>
	/// <param name="index">Index of item to access.</param>
	public ref T this[int index]
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => ref _data[index];
	}

	/// <summary>Adds item to the list, increases capacity if needed.</summary>
	/// <param name="item"></param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Add(T item)
	{
		if (_count == Capacity)
		{
			Reallocate(_count + 1);
		}

		_data[_count++] = item;
	}

	private void Reallocate(int count)
	{
		var capacity = Math.Max((int)Polyfills.RoundUpPow2((uint)count), _capacity0);
		if (capacity == Capacity)
		{
			return;
		}

		var source = _data;
		var target = ArrayPool<T>.Shared.Rent(capacity);
		source.AsSpan(0, _count).CopyTo(target);
		ArrayPool<T>.Shared.Return(source);
		_data = target;
	}

	/// <summary>
	/// Removes all items from the list starting from specified index.
	/// Reallocates array if usage percentage falls below 1/3.
	/// </summary>
	/// <param name="index">Starting index.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void TruncateAt(int index)
	{
		_data.AsSpan(index, _count - index).Clear();
		_count = index;
		var capacity = Capacity;
		if (_count < capacity >> 1 && capacity > _capacity0)
		{
			Reallocate(_count);
		}
	}
}
