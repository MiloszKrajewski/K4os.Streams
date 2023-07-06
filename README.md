# K4os.Streams

[![NuGet Stats](https://img.shields.io/nuget/v/K4os.Streams.svg)](https://www.nuget.org/packages/K4os.Streams)

# Description

The need for this library was triggered by a project which used `MemoryStream` a lot and I was told by
memory profiler that is very heavy on memory allocation.

I was aware that `RecyclableMemoryStream` exists but I wanted something lighter (the question if I succeeded is a
different matter, lol).

There are two (so far) stream implementations in this library: `ResizingByteBufferStream` and `ChunkedByteBufferStream`.
Both of them are using `ArrayPool<byte>` but `ResizingByteBufferStream` stores data in one (potentially) large array 
(the same approach as `MemoryStream`) while `ChunkedByteBufferStream` stores data in a list of chunks.

## Measuring performance

It is hard to measure performance, because lot of it depends on usage patterns. 
Are you using small or large streams? Do they stay in memory for long? Do you read/write them in small or large chunks?
What are the thresholds for certain actions (like resizing or chunking)? Do you measure it just before threshold 
or just after?

It is hard to have some objective numbers.

What I measured was continuous writing (no `Seek`) of small chunks (1K) and then continuous reading but in 
bigger chunks (8K). This was based on usage pattern where I was building a json payload from data (small `Write`s)  
and then sending them over network (bigger `Read`s).

Note, I think I already notices that `RecyclableMemoryStream` prefer larger chunks, so results could be different.

All measurements were done using:

```
BenchmarkDotNet=v0.13.5, OS=Windows 11 (10.0.22621.1848/22H2/2022Update/SunValley2)
AMD Ryzen 5 3600, 1 CPU, 12 logical and 6 physical cores
.NET SDK=6.0.410
  [Host]     : .NET 6.0.18 (6.0.1823.26907), X64 RyuJIT AVX2
  DefaultJob : .NET 6.0.18 (6.0.1823.26907), X64 RyuJIT AVX2
```

NOTE: in first column names of streams has been shortened to fit in table:

| Name             | Actual class                                                                                                                          |
|------------------|---------------------------------------------------------------------------------------------------------------------------------------|
| MemoryStream     | `MemoryStream` from `System.IO`                                                                                                       |
| RecyclableStream | `RecyclableMemoryStream` from [Microsoft.IO.RecyclableMemoryStream](https://github.com/Microsoft/Microsoft.IO.RecyclableMemoryStream) |
| ResizingStream   | `ResizingByteBufferStream` from `K4os.Streams`                                                                                        |
| ChunkedStream    | `ChunkedByteBufferStream` from `K4os.Streams`                                                                                         |

## Small streams (128B - 64KB)

|           Method | Length |        Mean | Ratio |    Gen0 |   Gen1 | Allocated |
|----------------- |------- |------------:|------:|--------:|-------:|----------:|
|     MemoryStream |    128 |    51.95 ns |  1.00 |  0.0411 |      - |     344 B |
| RecyclableStream |    128 |   278.25 ns |  5.36 |  0.0324 |      - |     272 B |
|   ResizingStream |    128 |    44.52 ns |  0.86 |  0.0401 |      - |     336 B |
|    ChunkedStream |    128 |    46.08 ns |  0.89 |  0.0421 |      - |     352 B |
|                  |        |             |       |         |        |           |
|     MemoryStream |   1024 |   101.99 ns |  1.00 |  0.1329 | 0.0005 |    1112 B |
| RecyclableStream |   1024 |   312.58 ns |  3.06 |  0.0324 |      - |     272 B |
|   ResizingStream |   1024 |    85.31 ns |  0.79 |  0.0067 |      - |      56 B |
|    ChunkedStream |   1024 |    90.07 ns |  0.88 |  0.0086 |      - |      72 B |
|                  |        |             |       |         |        |           |
|     MemoryStream |   8192 |   982.99 ns |  1.00 |  1.8539 | 0.0668 |   15520 B |
| RecyclableStream |   8192 |   627.14 ns |  0.63 |  0.0324 |      - |     272 B |
|   ResizingStream |   8192 |   479.17 ns |  0.48 |  0.0067 |      - |      56 B |
|    ChunkedStream |   8192 |   483.87 ns |  0.49 |  0.0086 |      - |      72 B |
|                  |        |             |       |         |        |           |
|     MemoryStream |  65335 | 7,278.42 ns |  1.00 | 15.5029 | 3.8681 |  130280 B |
| RecyclableStream |  65335 | 3,460.21 ns |  0.48 |  0.0305 |      - |     272 B |
|   ResizingStream |  65335 | 3,711.39 ns |  0.51 |  0.0038 |      - |      56 B |
|    ChunkedStream |  65335 | 3,851.97 ns |  0.53 |  0.0076 |      - |      72 B |


## Medium streams (128KB - 8MB)

|           Method |  Length |         Mean | Ratio |     Gen0 |     Gen1 |     Gen2 |
|----------------- |-------- |-------------:|------:|---------:|---------:|---------:|
|     MemoryStream |  131072 |    60.229 us |  1.00 |  41.6260 |  41.6260 |  41.6260 |
| RecyclableStream |  131072 |     6.554 us |  0.11 |   0.0305 |        - |        - |
|   ResizingStream |  131072 |     7.403 us |  0.12 |        - |        - |        - |
|    ChunkedStream |  131072 |     6.836 us |  0.11 |   0.0458 |        - |        - |
|                  |         |              |       |          |          |          |
|     MemoryStream | 1048576 |   770.487 us |  1.00 | 499.0234 | 499.0234 | 499.0234 |
| RecyclableStream | 1048576 |    52.645 us |  0.07 |   0.0610 |        - |        - |
|   ResizingStream | 1048576 |    60.258 us |  0.08 |        - |        - |        - |
|    ChunkedStream | 1048576 |    46.239 us |  0.06 |        - |        - |        - |
|                  |         |              |       |          |          |          |
|     MemoryStream | 8388608 | 7,484.830 us |  1.00 | 742.1875 | 742.1875 | 742.1875 |
| RecyclableStream | 8388608 |   439.533 us |  0.06 |   2.4414 |        - |        - |
|   ResizingStream | 8388608 | 1,543.618 us |  0.22 |        - |        - |        - |
|    ChunkedStream | 8388608 |   380.532 us |  0.05 |        - |        - |        - |


## Large streams (128MB - 512MB)

|           Method |    Length |      Mean | Ratio |      Gen0 |      Gen1 |      Gen2 |
|----------------- |---------- |----------:|------:|----------:|----------:|----------:|
|     MemoryStream | 134217728 | 123.99 ms |  1.00 | 4800.0000 | 4800.0000 | 4800.0000 |
| RecyclableStream | 134217728 |  28.94 ms |  0.23 |  500.0000 |   31.2500 |         - |
|   ResizingStream | 134217728 |  41.55 ms |  0.33 |         - |         - |         - |
|    ChunkedStream | 134217728 |  28.85 ms |  0.23 |  125.0000 |  125.0000 |  125.0000 |
|                  |           |           |       |           |           |           |
|     MemoryStream | 536870912 | 753.93 ms |  1.00 | 6000.0000 | 6000.0000 | 6000.0000 |
| RecyclableStream | 536870912 | 138.75 ms |  0.18 | 8000.0000 |  800.0000 |         - |
|   ResizingStream | 536870912 | 163.87 ms |  0.20 |         - |         - |         - |
|    ChunkedStream | 536870912 | 136.63 ms |  0.18 |         - |         - |         - |

## Observations

* `ResizingByteBufferStream` is the fastest for small streams
* `ChunkedByteBufferStream` is not much worse in small stream range, but shines in medium and large streams
* `RecyclableMemoryStream` has quite a lot of overhead, that's why it's 5x slower than `MemoryStream` for tiny streams
* `RecyclableMemoryStream` is very good for medium and large stream
* `MemoryStream` is the kind of good for tiny streams, but nothing more
* `MemoryStream` is the worst for large streams

## Decision tree

I just roughly scored choosing given stream implementations for certain ranges:

What I would say, the result can be read as: `ResizingByteBufferStream` is the best for small streams,
while `ChunkedByteBufferStream` is the best all-rounder. `MemoryStream` is terrible for large streams,
while `RecyclableMemoryStream` is quite bad for small streams.


| Size   | MemoryStream | ResizingStream | ChunkedStream | RecyclableStream |
|:-------|:------------:|:--------------:|:-------------:|:----------------:|
| tiny   |      B       |       A*       |       A       |        F         |
| small  |      D       |       A*       |       A       |        B         |
| medium |      F       |       B        |      A*       |        A         |
| large  |      F       |       C        |      A*       |        A*        |

* If your streams are always very small, use `ResizingByteBufferStream`
* If your streams are always quite large, use `RecyclableMemoryStream` or `ChunkedByteBufferStream`
* If you need a compromise, have medium or unpredictable sizes, use `ChunkedByteBufferStream`

# Usage

One very important note is those streams need to be disposed to get the benefit, if you don't dispose them
the performance will be roughly the same as `MemoryStream`.

It is a little bit problematic though as memory is disposed at `Dispose` so you may not access it `.ToArray()` 
after that.

**If you need to get data from stream, do it before disposing it!**

```csharp
using var stream = new ChunkedByteBufferStream();
using var writer = new StreamWriter(stream, leaveOpen: true); // NOTE: leaveOpen!
writer.Write("Hello, world!");
writer.Flush();
Console.WriteLine(Encoding.UTF8.GetString(stream.ToArray());
```

There are some memory specific methods available on both streams allowing quickly access data in them:

```charp
class ResizingByteBufferStream : Stream
{
    Span<byte> Peek();
    
    int ExportTo(Span<byte> target);
    byte[] ToArray();
}

class ChunkedByteBufferStream
{
    int ExportTo(Span<byte> target);
    byte[] ToArray();
}
```

Other than that it is just a `Stream`.

# Build

```shell
build
```
