#pragma warning disable CS8321

using BenchmarkDotNet.Running;
using K4os.Streams.Benchmarks;

#if !DEBUG
// ExecuteRoundtripReadWrite();
BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
#else
ExecuteRoundtripReadWrite();
#endif

void ExecuteRoundtripReadWrite()
{
	var test = new RoundtripWriteRead { Length = 128*1024*1024 };
	test.Setup();
	
	for (var i = 0; i < 100; i++)
	{
		test.ChunkedStream();
	}
}

