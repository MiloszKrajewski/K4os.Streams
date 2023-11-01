using System.Text;
using Xunit;

namespace K4os.Streams.Test;

public class StringBufferTests
{
	[Fact]
	public void EmptyBufferProducesEmptyString()
	{
		var buffer = new ResizingStringBuffer();
		Assert.Equal(string.Empty, buffer.ToString());
	}
	
	[Fact]
	public void AddingEmptyStringDoesNotChangeBufferSize()
	{
		var buffer = new ResizingStringBuffer();
		buffer.Append(string.Empty);
		Assert.Equal(string.Empty, buffer.ToString());
	}
	
	[Fact]
	public void AddingNullDoesNotChangeBufferSize()
	{
		var buffer = new ResizingStringBuffer();
		buffer.Append(default(string));
		Assert.Equal(string.Empty, buffer.ToString());
	}

	[Fact]
	public void LengthOfEmptyBufferIs0()
	{
		var buffer = new ResizingStringBuffer();
		buffer.Append(string.Empty);
		Assert.Equal(0, buffer.Length);
	}
	
	[Fact]
	public void OneCharacterCanBeAdded()
	{
		var buffer = new ResizingStringBuffer();
		buffer.Append('X');
		Assert.Equal("X", buffer.ToString());
		Assert.Equal(1, buffer.Length);
	}
	
	[Fact]
	public void StringCanBeAdded()
	{
		var buffer = new ResizingStringBuffer();
		var guid = Guid.NewGuid().ToString();
		buffer.Append(guid);
		Assert.Equal(guid, buffer.ToString());
		Assert.Equal(guid.Length, buffer.Length);
	}
	
	[Theory]
	[InlineData(22)]
	[InlineData(1024)]
	[InlineData(1337)]
	[InlineData(1024 * 1024 + 13)]
	public void ManyStringsCanBeAdded(int length)
	{
		var buffer = new ResizingStringBuffer();
		var builder = new StringBuilder();
		var remaining = length;

		while (remaining > 0)
		{
			var text = Guid.NewGuid().ToString();
			if (text.Length > remaining) text = text.Substring(0, remaining);
			builder.Append(text);
			buffer.Append(text);
			remaining -= text.Length;
		}
		
		Assert.Equal(builder.ToString(), buffer.ToString());
		Assert.Equal(builder.Length, buffer.Length);
		Assert.Equal(length, buffer.Length);
	}
}
