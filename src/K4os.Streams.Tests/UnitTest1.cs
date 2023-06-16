using Xunit;

namespace K4os.Streams.Test;

public class UnitTest1
{
	private readonly Class1 _sut = new();

	[Fact]
	public void CanTestOriginalAssembly()
	{
		Assert.Equal("42", _sut.IntToString(42));
	}
		
	[Fact]
	public void InternalMethodsAreVisibleToTests()
	{
		Assert.Equal("true", _sut.BoolToString(true));
	}
}