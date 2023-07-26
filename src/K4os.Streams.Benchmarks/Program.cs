#pragma warning disable CS8321

using BenchmarkDotNet.Running;
using K4os.Streams.Benchmarks;

#if !DEBUG
// StreamRoundTripTest();
// TextWriterToUtf8Test();
BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
#else
// StreamRoundTripTest();
TextWriterToUtf8Test();
#endif

void StreamRoundTripTest()
{
	var test = new StreamRoundTrip { Length = 128*1024*1024 };
	test.Setup();
	
	for (var i = 0; i < 100; i++)
	{
		test.ChunkedStream();
	}
}

void TextWriterToUtf8Test()
{
	var test = new TextWriterToUtf8 { Messages = 10 };
	
	for (var i = 0; i < 10000; i++)
	{
		test.ResizingByteTextWriter();
	}
}

