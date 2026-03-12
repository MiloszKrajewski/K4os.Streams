namespace K4os.Streams;

public class IsolatingStream: TransparentStream
{
	public IsolatingStream(Stream innerStream): base(innerStream) { }

	/// <inheritdoc/>
	protected override void Dispose(bool disposing) { }

	#if NETSTANDARD2_1 || NET5_0_OR_GREATER
	
	/// <inheritdoc/>
	public override ValueTask DisposeAsync() => default;

	#endif
}

public static class StreamExtensions
{
	public static IsolatingStream Isolate(this Stream stream) => 
		stream as IsolatingStream ?? new IsolatingStream(stream);
}