using System.Buffers;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

#if NET6_0_OR_GREATER
using System.Numerics;
#endif

namespace K4os.Streams.Internal;

/// <summary>
/// This class is a collection of polyfills for older version of .NET.
/// </summary>
internal static class Polyfills
{
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static uint RoundUpPow2(uint value)
	{
		#if NET6_0_OR_GREATER
		return BitOperations.RoundUpToPowerOf2(value);
		#else
		value--;
		value |= value >> 1;
		value |= value >> 2;
		value |= value >> 4;
		value |= value >> 8;
		value |= value >> 16;
		return value + 1;
		#endif
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int Clamp(int value, int min, int max)
	{
		#if NETSTANDARD2_1 || NET5_0_OR_GREATER
		return Math.Clamp(value, min, max);
		#else
		Debug.Assert(min <= max);
		return value < min ? min : value > max ? max : value;
		#endif
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static long Clamp(long value, long min, long max)
	{
		#if NETSTANDARD2_1 || NET5_0_OR_GREATER
		return Math.Clamp(value, min, max);
		#else
		Debug.Assert(min <= max);
		return value < min ? min : value > max ? max : value;
		#endif
	}

	[DoesNotReturn]
	[MethodImpl(MethodImplOptions.NoInlining)]
	public static T Throw<T>(this Exception exception) => throw exception;

	#if NETSTANDARD2_1 || NET5_0_OR_GREATER

	[MethodImpl(MethodImplOptions.NoInlining)]
	public static string CreateString<TContext>(
		int length, TContext context, SpanAction<char, TContext> action) =>
		string.Create(length, context, action);

	#else
	private const int MAX_STACKALLOC_CHARS = 1024;

	public delegate void SpanAction<TItem, in TContext>(Span<TItem> span, TContext context);
	
	public static unsafe string CreateString<TContext>(
		int length, TContext context, SpanAction<char, TContext> action)
	{
		var pooled = length > MAX_STACKALLOC_CHARS
			? ArrayPool<char>.Shared.Rent(length)
			: null;

		var target = pooled is null
			? stackalloc char[length]
			: pooled.AsSpan(0, length);
		
		action(target, context);

		string result;
		fixed (char* targetP = target) 
			result = new string(targetP, 0, length);
		
		if (pooled is not null)
			ArrayPool<char>.Shared.Return(pooled);

		return result;
	}

	#endif

	[Conditional("CALL_NOOP")]
	public static void Noop<T>(T subject) { }
}
