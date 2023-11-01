namespace K4os.Streams;

public interface IStringBuffer
{
	public int Length { get; }
	void Reset();
	void Extend(ReadOnlySpan<char> text);
	public int ExportTo(Span<char> target);
}
