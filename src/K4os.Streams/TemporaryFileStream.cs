namespace K4os.Streams;

/// <summary>
/// Temporary stream stored in temp folder.
/// Mimics <see cref="MemoryStream"/> in behaviour (disappears when disposed)
/// but does not use memory.
/// </summary>
/// <seealso cref="Stream" />
public class TemporaryFileStream: FileStream
{
	private const int DefaultBufferSize = 4096;

	/// <summary>Creates temporary stream which will be deleted when stream is closed.</summary>
	/// <returns>New temporary stream.</returns>
	public static TemporaryFileStream Create() => new();

	/// <summary>Creates temporary stream which will be deleted when stream is closed.</summary>
	/// <param name="basePath">Base path, uses TEMP if not provided.</param>
	/// <returns>New temporary stream.</returns>
	public TemporaryFileStream(string? basePath = null):
		base(
			Path.Combine(basePath ?? Path.GetTempPath(), Guid.NewGuid().ToString("N")),
			FileMode.Create, FileAccess.ReadWrite, FileShare.None,
			DefaultBufferSize,
			FileOptions.DeleteOnClose) { }
}