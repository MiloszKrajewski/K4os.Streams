using System.Text;
using Xunit;

namespace K4os.Streams.Test;

public class ExportTests
{
	private static StreamWriter CreateUtf8Writer(Stream stream) => 
		new(stream, new UTF8Encoding(false), 1024, true);

	[Fact]
	public void ExportReturnAllData()
	{
		using var stream = new ChunkedByteBufferStream();
		using var writer = new StreamWriter(stream);
		var guid = Guid.NewGuid().ToString();
		writer.Write(guid);
		writer.Flush();
		var actual = Encoding.UTF8.GetString(stream.ToArray());
		Assert.Equal(guid, actual);
	}
	
	[Fact]
	public void WhenDisposeIsNotCascadingWeAreGood()
	{
		using var stream = new ChunkedByteBufferStream();
		var writer = CreateUtf8Writer(stream);
		var guid = Guid.NewGuid().ToString();
		writer.Write(guid);
		writer.Flush();
		writer.Dispose();
		var actual = Encoding.UTF8.GetString(stream.ToArray());
		Assert.Equal(guid, actual);
	}

	[Fact]
	public void ThereIsNoDataAfterDispose()
	{
		using var stream = new ChunkedByteBufferStream();
		var writer = new StreamWriter(stream);
		var guid = Guid.NewGuid().ToString();
		writer.Write(guid);
		writer.Flush();
		writer.Dispose();
		Assert.Equal(string.Empty, Encoding.UTF8.GetString(stream.ToArray()));
	}
}
