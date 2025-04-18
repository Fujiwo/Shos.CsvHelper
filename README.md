# Shos.CsvHelper
[Csv (comma-separated values)](https://en.wikipedia.org/wiki/Comma-separated_values "Comma-separated values | Wikipedia") Library

* English document: [Shos.CsvHelper | Programming C# (Sho's Software)](https://wp.shos.info/2019/10/17/c-net-shos-csvhelper-simple-library-for-reading-and-writing-csv/ "Shos.CsvHelper | Programming C# (Sho's Software)")
* Japanese document: [Shos.CsvHelper | Programming C# (Sho's Software)](https://wp.shos.info/2017/07/12/post-0-2/ "Shos.CsvHelper | Programming C# (Sho's Software)")

## NuGet

You can install Shos.CsvHelper to your project with [NuGet](https://www.nuget.org) on Visual Studio.

* [NuGet Gallery | Shos.CsvHelper](https://www.nuget.org/packages/Shos.CsvHelper/)

### Package Manager

    PM>Install-Package Shos.CsvHelper -version 1.0.0.3

### .NET CLI

    >dotnet add package Shos.CsvHelper --version 1.0.0.3

### PackageReference

    <PackageReference Include="Shos.CsvHelper" Version="1.0.0.3" />

## Projects

### Shos.CsvHelper
* Csv (comma-separated values) Library
* .NET 8.0
* for .NET 8.0 or later
* Install as a NuGet Package: [NuGet Gallery | Shos.CsvHelper](https://www.nuget.org/packages/Shos.CsvHelper "NuGet Gallery | Shos.CsvHelper")

### Shos.CsvHelper.NetFramework
* Csv (comma-separated values) Library for .NET Framework
* .NET 4.8
* .NET Framework 4.8 or later
* Install as a NuGet Package: [NuGet Gallery | Shos.CsvHelper.NetFramework](https://www.nuget.org/packages/Shos.CsvHelper.NetFramework "NuGet Gallery | Shos.CsvHelper.NetFramework")

### Shos.CsvHelperSample.NetCore
* .NET Core Console Sample for Shos.CsvHelper

### Shos.CsvHelperSample.NetFramework
* .NET Framework Console Sample for Shos.CsvHelper.NetFramework

## Sample

CsvHelperSample.cs | Shos.CsvHelperSample.NetCore or Shos.CsvHelperSample.NetFramework

```C#
// .NET 8.0 or later
// .NET Framework 4.8 or later

namespace Shos.CsvHelperSample
{
    using Shos.CsvHelper;
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
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
            collection.ToList().ForEach(element => Console.WriteLine(element));
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

        public int      Id       { get; set; }                    // yes (this property will be written and read as csv)
        public string   Title    { get; set; } = "";              // yes
        public DateTime Deadline { get; set; } = DateTime.Now;    // yes
        public bool     Done     { get; set; }                    // yes
        public Priority Priority { get; set; } = Priority.Middle; // yes: user-defined enum
        [ColumnName("Details")]
        public string   Detail   { get; set; } = "";              // yes: change column name with [ColumnName("Details")]
        public DaySpan  DaySpan  { get; set; }                    // yes: user-defined type which can't "TryParse" but "Parse"
        [CsvIgnore()]
        public string   Option   { get; set; } = "";              // no : ignore this property with [CsvIgnore()]
        public string   Version => "1.0";                         // no : read only or write only property will be ignored

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

## Author Info

Fujio Kojima: a software developer in Japan
* Microsoft MVP for Development Tools - Visual C# (Jul. 2005 - Dec. 2014)
* Microsoft MVP for .NET (Jan. 2015 - Oct. 2015)
* Microsoft MVP for Visual Studio and Development Technologies (Nov. 2015 - Jun. 2018)
* Microsoft MVP for Developer Technologies (Nov. 2018 - Jun. 2025)
* [MVP Profile](https://mvp.microsoft.com/en-us/PublicProfile/21482 "MVP Profile")
* [Blog (Japanese)](http://wp.shos.info "Blog (Japanese)")
* [Web Site (Japanese)](http://www.shos.info "Web Site (Japanese)")
* [Twitter](https://twitter.com/Fujiwo)
* [Instagram](https://www.instagram.com/fujiwo/)

## License

This library is under the MIT License.
