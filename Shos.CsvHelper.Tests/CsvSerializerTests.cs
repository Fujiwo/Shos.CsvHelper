using System.Collections.Concurrent;
using System.Text;
using Shos.CsvHelper;
using Xunit;

namespace Shos.CsvHelper.Tests;

public class CsvSerializerTests
{
    [Fact]
    public void ReadCsv_WhenRowHasFewerFieldsThanHeader_LeavesMissingPropertiesAtDefault()
    {
        using var stream = CreateStream("Name,Count\nAlice");

        var rows = CsvSerializer.ReadCsv<Row>(stream).ToArray();

        Assert.Single(rows);
        Assert.Equal("Alice", rows[0].Name);
        Assert.Equal(0, rows[0].Count);
    }

    [Fact]
    public void ReadCsv_WhenInputIsEmpty_ReturnsEmptySequence()
    {
        using var stream = CreateStream("");

        var rows = CsvSerializer.ReadCsv<Row>(stream);

        Assert.NotNull(rows);
        Assert.Empty(rows);
    }

    [Fact]
    public void WriteCsv_IgnoresIndexerProperties()
    {
        var rows = new[] { new IndexedRow { Name = "Alice" } };
        using var stream = new MemoryStream();

        rows.WriteCsv(stream, bufferSize: 1024, leaveOpen: true);

        Assert.Equal("\uFEFFName\nAlice\n", Encoding.UTF8.GetString(stream.ToArray()));
    }

    [Fact]
    public void ReadCsv_IgnoresIndexerProperties()
    {
        using var stream = CreateStream("Name,Item\nAlice,0");

        var row = Assert.Single(CsvSerializer.ReadCsv<IndexedRow>(stream));

        Assert.Equal("Alice", row.Name);
    }

    [Fact]
    public void ReadCsv_WhenValueCannotBeParsed_LeavesPropertyAtDefault()
    {
        using var stream = CreateStream("Value\nnot-a-number");

        var row = Assert.Single(CsvSerializer.ReadCsv<ParsedRow>(stream));

        Assert.Equal(17, row.Value);
    }

    [Fact]
    public void ReadCsv_CanInitializeTypeCachesConcurrently()
    {
        var failures = new ConcurrentQueue<Exception>();

        Parallel.For(0, 64, index =>
        {
            try
            {
                using var stream = CreateStream($"Name\nrow-{index}");
                var row = Assert.Single(CsvSerializer.ReadCsv<ConcurrentRow<string>>(stream));
                Assert.Equal($"row-{index}", row.Name);
            }
            catch (Exception exception)
            {
                failures.Enqueue(exception);
            }
        });

        Assert.Empty(failures);
    }

    private static MemoryStream CreateStream(string csv)
        => new(Encoding.UTF8.GetBytes(csv));

    public class Row
    {
        public string Name { get; set; } = "";
        public int Count { get; set; }
    }

    public class IndexedRow
    {
        public string Name { get; set; } = "";
        public string this[int index]
        {
            get => index.ToString();
            set { }
        }
    }

    public class ParsedRow
    {
        public int Value { get; set; } = 17;
    }

    public class ConcurrentRow<T>
    {
        public string Name { get; set; } = "";
        public T? Value { get; set; }
    }
}
