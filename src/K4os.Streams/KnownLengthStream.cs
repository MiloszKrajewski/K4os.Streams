namespace K4os.Streams;

/// <summary>
/// Stream with known length. This has niche use cases, like HTTP content, or S3 streams.
/// We usually know their length (from headers, for example) but stream itself reports unknown length.
/// This is a simple wrapper which enforces that length is always well-defined.
/// </summary>
public class KnownLengthStream: TransparentStream
{
	private readonly long _knownLength;

	/// <summary>
	/// Ensures the length is truly known. In case that length is not provided
	/// (length being null) and not returned by stream itself
	/// (input.Length returning 0 or less or throwing exception) then stream
	/// is cloned to temporary stream.
	/// By default <see cref="TemporaryFileStream"/> is used (file backed) but
	/// any stream can be used thanks to <paramref name="factory"/> argument.
	/// </summary>
	/// <param name="input">Input stream.</param>
	/// <param name="length">Known length or <c>null</c></param>
	/// <param name="factory">Clones stream factory (optional)</param>
	/// <returns>Original or cloned stream with length property known.</returns>
	public static async Task<Stream> Ensure(
		Stream input, long? length, Func<Stream>? factory = null) =>
		(TryLengthOf(input), length) switch {
			(null, null) => await CloneStream(input, factory),
			(_, null) => input,
			var (a, l) when a == l => input,
			var (_, l) => Force(input, l.Value),
		};

	/// <summary>Forces stream to return given length.</summary>
	/// <param name="input">Input stream.</param>
	/// <param name="length">Known length.</param>
	/// <returns>Stream with known length.</returns>
	public static Stream Force(Stream input, long length) =>
		new KnownLengthStream(input, length);

	/// <summary>Forces stream to return -1 as length,
	/// which, in many cases, is interpreted as 'unknown'.</summary>
	/// <param name="input">Input stream.</param>
	/// <returns>Stream with unknown (-1) length.</returns>
	public static Stream Unknown(Stream input) =>
		new KnownLengthStream(input, -1);

	private static async Task<Stream> CloneStream(
		Stream input, Func<Stream>? factory = null)
	{
		var output = factory?.Invoke() ?? new TemporaryFileStream();
		var origin = output.Position;
		await input.CopyToAsync(output);
		output.Position = origin;
		return output;
	}

	private static long? TryLengthOf(Stream input)
	{
		try
		{
			return input.Length switch { var l and > 0 => l, _ => default(long?) };
		}
		catch
		{
			return null;
		}
	}

	/// <summary>
	/// Creates <see cref="KnownLengthStream"/> wrapping around other stream, but enforcing known length.
	/// </summary>
	/// <param name="inner">Inner stream.</param>
	/// <param name="knownLength">Known length stream.</param>
	public KnownLengthStream(Stream inner, long knownLength): base(inner) =>
		_knownLength = knownLength;

	/// <summary>
	/// Length of the stream.
	/// </summary>
	public override long Length => _knownLength;
}
