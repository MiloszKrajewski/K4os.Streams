using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using K4os.Streams.Internal;

namespace K4os.Streams.Buffers;

/// <summary>
/// Contains abstraction for <see cref="ArrayPool{T}"/>.
/// Makes some assumptions about reasonable minimum and maximum size of pooled arrays.
/// </summary>
internal static class ByteArray
{
	// see: https://learn.microsoft.com/en-us/dotnet/api/system.array?redirectedfrom=MSDN&view=netcore-3.1
	public const int MAX_ARRAY_SIZE = 0X7FFFFFC7;
	public const int SAFE_ARRAY_SIZE_LIMIT = 0x40000000 - 1;

	public const int MIN_ARRAY_SIZE = 256;
	public const int MIN_POOLED_SIZE = 1 * 1024;
	public const int MAX_POOLED_SIZE = 1024 * 1024;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static byte[] Allocate(int size) =>
		size <= 0 ? Array.Empty<byte>() :
		size < MIN_POOLED_SIZE ? new byte[size] :
		ArrayPool<byte>.Shared.Rent(size);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void Recycle(byte[]? array)
	{
		if (array is { Length: >= MIN_POOLED_SIZE })
			ArrayPool<byte>.Shared.Return(array);
	}
	
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static byte[] Reallocate(byte[]? source, int usedBytes, int minimumLength)
	{
		if (source is null)
		{
			return Allocate(minimumLength);
		}
		
		if (minimumLength == source.Length)
		{
			// old capacity is the same as new capacity, then we don't need any allocation
			return source;
		}

		var target = Allocate(minimumLength);
		var bytesToCopy = Math.Min(usedBytes, minimumLength);
		Buffer.BlockCopy(source, 0, target, 0, bytesToCopy);
		Recycle(source);

		return target;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int ArraySize(long length) =>
		length switch {
			<= 0 => 0,
			<= MIN_ARRAY_SIZE => MIN_ARRAY_SIZE,
			<= SAFE_ARRAY_SIZE_LIMIT => (int)Polyfills.RoundUpPow2((uint)length),
			<= MAX_ARRAY_SIZE => MAX_ARRAY_SIZE,
			_ => ThrowOutOfMemory<int>(length),
		};

	[DoesNotReturn]
	[MethodImpl(MethodImplOptions.NoInlining)]
	private static T ThrowOutOfMemory<T>(long length) => 
		throw new OutOfMemoryException($"Expected buffer capacity is too large ({length})");
}

