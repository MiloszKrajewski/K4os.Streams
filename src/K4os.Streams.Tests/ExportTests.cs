using System.Text;
using Xunit;

namespace K4os.Streams.Test;

public class ExportTests
{
	[Fact]
	public void ExportReturnAllData()
	{
		using var stream = new ChunkedByteBufferStream();
		using var writer = new StreamWriter(stream, leaveOpen: true); // NOTE: leave open!
		var guid = Guid.NewGuid().ToString();
		writer.Write(guid);
		writer.Flush();
		Assert.Equal(guid, Encoding.UTF8.GetString(stream.ToArray()));
	}
	
	[Fact]
	public void WhenDisposeIsNotCascadingWeAreGood()
	{
		using var stream = new ChunkedByteBufferStream();
		var writer = new StreamWriter(stream, leaveOpen: true); // NOTE: leave open!
		var guid = Guid.NewGuid().ToString();
		writer.Write(guid);
		writer.Flush();
		writer.Dispose();
		Assert.Equal(guid, Encoding.UTF8.GetString(stream.ToArray()));
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
