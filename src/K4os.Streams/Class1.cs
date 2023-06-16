namespace K4os.Streams;

public class Class1
{
	// NOTE: this is public method without xml-docs
	// so it should be a warning when building in release mode
	// and an error when publishing
	public string IntToString(int value) => value.ToString();

	internal string BoolToString(bool value) => value.ToString();
}