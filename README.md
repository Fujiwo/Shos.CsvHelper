# Shos.CsvHelper
Csv (comma-separated values) Library

## Projects

### Shos.CsvHelper
* Csv (comma-separated values) Library
* .NET Standard Library
* .NET Standard 1.3 or later
* for .NET Network 4.6 or later, .NET Core 1.1 or later
* Install as a NuGet Package: [NuGet Gallery | Shos.CsvHelper](https://www.nuget.org/packages/Shos.CsvHelper "NuGet Gallery | Shos.CsvHelper")

### Shos.CsvHelper.NetFramework
* Csv (comma-separated values) Library for .NET Framework
* .NET Framework 4.5.2 or later
* Install as a NuGet Package: [NuGet Gallery | Shos.CsvHelper.NetFramework](https://www.nuget.org/packages/Shos.CsvHelper.NetFramework "NuGet Gallery | Shos.CsvHelper.NetFramework")

### Shos.CsvHelperSample.NetCore
* .NET Core Console Sample for Shos.CsvHelper

### Shos.CsvHelperSample.NetFramework
* .NET Framework Console Sample for Shos.CsvHelper.NetFramework

## Sample

CsvHelperSample.cs | Shos.CsvHelperSample.NetCore or Shos.CsvHelperSample.NetFramework

```C#
// .NET Core 1.1 or later
// .NET Framework 4.5.2 or later

namespace Shos.CsvHelperSample
{
    using Shos.CsvHelper;
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Text;
    using System.Threading.Tasks;

    class Program
    {
        static void Main() => CsvHelperTester.Run().Wait();
    }

    static class CsvHelperTester
    {
        public static async Task Run()
        {
            // something IEnumerable<TElement>
            // TElement:
            // public properties of TElement will be written and read as csv
            // for writing: type of each property should have "get" and "set"
            // for reading: type of each property should have "get" and "set" and should be string or enum or type which has a default constructor and can "TryParse" or "Parse"

            IEnumerable<ToDo> toDoes = new ToDoList();
            toDoes.Show();

            // set encoding if you need (the default is UTF8)
            CsvSerializer.Encoding = Encoding.GetEncoding(0);

            // set separator if you need (the default is ',')
            //CsvSerializer.Separator = '\t';

            // write csv with header (recommended)
            const string csvWithHeaderFileName = "todo.withheader.csv";
            await toDoes.WriteCsvAsync(csvWithHeaderFileName);

            /*
            Result: todo.withheader.csv

Id,Title,Deadline,Done,Priority,Details,DaySpan
1,filing tax returns,2018/12/01 0:00:00,False,Middle,,0
2,report of a business trip,2017/07/12 13:13:01,False,High,"""ASAP""",3
3,expense slips,2017/07/12 13:13:01,True,Low,"book expenses: ""C# 6.0 and the .NET 4.6 Framework"",""The C# Programming""",0
4, wish list ,2017/07/12 13:13:01,False,High," 	 (1) ""milk""
 	 (2) shampoo
 	 (3) tissue ",0
             */

            IEnumerable<ToDo> newToDoes = await CsvSerializer.ReadCsvAsync<ToDo>(csvFilePathName: csvWithHeaderFileName);
            newToDoes.Show();

            // write csv without header
            const string csvWithoutHeaderFileName = "todo.withoutheader.csv";
            toDoes.WriteCsv(csvFilePathName: csvWithoutHeaderFileName, hasHeader: false);

            /*
            Result: todo.withoutheader.csv

1,filing tax returns,2018/12/01 0:00:00,False,Middle,,0
2,report of a business trip,2017/07/12 13:13:01,False,High,"""ASAP""",3
3,expense slips,2017/07/12 13:13:01,True,Low,"book expenses: ""C# 6.0 and the .NET 4.6 Framework"",""The C# Programming""",0
4, wish list ,2017/07/12 13:13:01,False,High," 	 (1) ""milk""
 	 (2) shampoo
 	 (3) tissue ",0
             */

            newToDoes = CsvSerializer.ReadCsv<ToDo>(csvFilePathName: csvWithoutHeaderFileName, hasHeader: false);
            newToDoes.Show();
        }

        static void Show<TElement>(this IEnumerable<TElement> collection)
        {
            collection.ForEach(element => Console.WriteLine(element));
            Console.WriteLine();
        }
    }

    class ToDoList : IEnumerable<ToDo> // sample data
    {
        public IEnumerator<ToDo> GetEnumerator()
        {
            yield return new ToDo { Id = 1, Title = "filing tax returns", Deadline = new DateTime(2018, 12, 1) };
            yield return new ToDo { Id = 2, Title = "report of a business trip", Detail = "\"ASAP\"", DaySpan = new DaySpan(3), Priority = Priority.High };
            yield return new ToDo { Id = 3, Title = "expense slips", Detail = "book expenses: \"C# 6.0 and the .NET 4.6 Framework\",\"The C# Programming\"", Priority = Priority.Low, Done = true };
            yield return new ToDo { Id = 4, Title = " wish list ", Detail = " \t (1) \"milk\"\n \t (2) shampoo\n \t (3) tissue ", Priority = Priority.High };
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    class ToDo // sample class
    {
        // public properties will be written and read as csv
        // for writing: type of each property should have "get" and "set"
        // for reading: type of each property should have "get" and "set" and should be string or enum or type which has a default constructor and can "TryParse" or "Parse"

        public int      Id       { get; set; }
        public string   Title    { get; set; } = "";
        public DateTime Deadline { get; set; } = DateTime.Now;
        public bool     Done     { get; set; }
        public Priority Priority { get; set; } = Priority.Middle;
        [ColumnName("Details")]
        public string   Detail   { get; set; } = ""; // change column name with [ColumnName("Details")]
        public DaySpan  DaySpan  { get; set; } // type which can't "TryParse" but "Parse"
        [CsvIgnore()]
        public string   Option   { get; set; } = ""; // ignore this property with [CsvIgnore()]
        public string   Version => "1.0"; // read only or write only property will be ignored

        public override string ToString()
            => $"Id: {Id}, Title: {Title}, Deadline: {Deadline.ToString()}, Done: {Done}, Priority: {Priority}, Detail: {Detail}, DaySpan: {DaySpan}";
    }

    enum Priority { High, Middle, Low } // sample enum

    // sample type which can't "TryParse" but "Parse"
    struct DaySpan
    {
        public int Value { get; private set; }

        public DaySpan(int value) => Value = value;
        public static DaySpan Parse(string text) => new DaySpan(int.Parse(text));
        public override string ToString() => Value.ToString();
    }
}
```


