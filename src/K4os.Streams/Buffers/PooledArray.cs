using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using K4os.Streams.Internal;

namespace K4os.Streams.Buffers;

/// <summary>
/// Contains abstraction for <see cref="ArrayPool{T}"/>.
/// Makes some assumptions about reasonable minimum and maximum size of pooled arrays.
/// </summary>
internal static unsafe class PooledArray<TItem> where TItem: unmanaged
{
	// see: https://learn.microsoft.com/en-us/dotnet/api/system.array?redirectedfrom=MSDN&view=netcore-3.1
	public const int MAX_ARRAY_ITEMS = 0x7FFFFFC7;
	public const int SAFE_ARRAY_ITEMS = 0x40000000 - 1;
	
	public const int MIN_ALLOCATED_SIZE_BYTES = 256;
	public const int MIN_POOLED_SIZE_BYTES = 1024;
	public const int OPTIMAL_POOLED_SIZE_BYTES = 64 * 1024;
	public const int MAX_POOLED_SIZE_BYTES = 1024 * 1024;
	
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int BytesToItems(int bytes) => bytes / sizeof(TItem);

	public static int MinimumAllocatedSize
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => BytesToItems(MIN_ALLOCATED_SIZE_BYTES);
	}
	
	public static int MinimumPooledSize
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => BytesToItems(MIN_POOLED_SIZE_BYTES);
	}

	public static int MaximumPooledSize
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => BytesToItems(MAX_POOLED_SIZE_BYTES);
	}

	// The term "optimal" is quite loose here. It's just a size that is big enough
	// to be worth pooling, but not too big to be a problem for GC (below LOH threshold).
	public static int OptimalPooledSize
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => BytesToItems(OPTIMAL_POOLED_SIZE_BYTES);
	}
	
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static TItem[] Allocate(int size) =>
		size <= 0 ? Array.Empty<TItem>() :
		size < MinimumPooledSize ? new TItem[size] :
		ArrayPool<TItem>.Shared.Rent(size);
	
	/// <summary>
	/// Allocates a chunk of memory that is not bigger than <paramref name="hint"/>.
	/// It can be smaller though. It might be used to allocate a buffer to copy
	/// items across, but trying to not allocate too much memory. 
	/// </summary>
	/// <param name="hint">Maximum length needed, used as a hint.</param>
	/// <returns>An allocated buffer. It might be bigger and smaller than <paramref name="hint"/>.
	/// It is just roughly "around".</returns>
	[MethodImpl(MethodImplOptions.NoInlining)]
	public static TItem[] AllocateChunk(int hint) => 
		Allocate(Math.Min(hint, OptimalPooledSize));

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void Recycle(TItem[]? array)
	{
		if (array is not null && array.Length >= MinimumPooledSize)
			ArrayPool<TItem>.Shared.Return(array);
	}
	
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static TItem[] Reallocate(TItem[]? source, int usedBytes, int minimumLength)
	{
		if (source is null)
			return Allocate(minimumLength);
		
		// old capacity is the same as new capacity, then we don't need any allocation
		if (minimumLength == source.Length)
			return source;

		var target = Allocate(minimumLength);
		var bytesToCopy = Math.Min(usedBytes, minimumLength) * sizeof(TItem);
		Buffer.BlockCopy(source, 0, target, 0, bytesToCopy);
		Recycle(source);

		return target;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int ArraySize(long length) =>
		length switch {
			<= 0 => 0,
			_ when length <= MinimumAllocatedSize => MinimumAllocatedSize,
			<= SAFE_ARRAY_ITEMS => (int)Polyfills.RoundUpPow2((uint)length),
			<= MAX_ARRAY_ITEMS => MAX_ARRAY_ITEMS,
			_ => ThrowOutOfMemory<int>(length),
		};

	[DoesNotReturn]
	[MethodImpl(MethodImplOptions.NoInlining)]
	private static T ThrowOutOfMemory<T>(long length) => 
		throw new OutOfMemoryException($"Expected buffer capacity is too large ({length})");
}
