using System.Runtime.CompilerServices;
using K4os.Streams.Buffers;
using K4os.Streams.Internal;

namespace K4os.Streams;

public static class StringBufferExtensions
{
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static T Append<T>(this T buffer, ReadOnlySpan<char> text) where T: IStringBuffer
	{
		buffer.Extend(text);
		return buffer;
	}
	
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static unsafe T Append<T>(this T buffer, char symbol) where T: IStringBuffer => 
		buffer.Append(new ReadOnlySpan<char>(&symbol, 1));

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static T Append<T>(this T buffer, string? text) where T: IStringBuffer => 
		buffer.Append(text.AsSpan());

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static T Append<T>(this T buffer, char[] text) where T: IStringBuffer => 
		buffer.Append(text.AsSpan());

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static T Append<T>(this T buffer, char[] text, int offset, int length) 
		where T: IStringBuffer => 
		buffer.Append(text.AsSpan(offset, length));

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static string ExportToString<T>(this T buffer) where T: IBuffer<char> => 
		Polyfills.CreateString(
			ClampStringLength(buffer.Length), buffer, 
			static (target, self) => self.ExportTo(target));

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static int ClampStringLength(long length) => 
		(int)Math.Min(length, PooledArray<char>.MAX_ARRAY_ITEMS);
}
