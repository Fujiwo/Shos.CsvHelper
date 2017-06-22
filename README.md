# Shos.CsvHelper
Csv (comma-separated values) Library

## Projects

### Shos.CsvHelper
* Csv (comma-separated values) Library
* .NET Standard Library
* .NET Standard 1.3 or later
* for .NET Network 4.6 or later, .NET Core 1.1 or later

### Shos.CsvHelper.NetFramework
* Csv (comma-separated values) Library for .NET Framework
* .NET Framework 4.5.2 or later

### Shos.CsvHelperSample.NetCore
* .NET Core Console Sample for Shos.CsvHelper

### Shos.CsvHelperSample.NetFramework
* .NET Core Console Sample for Shos.CsvHelper.NetFramework

## Sample

CsvHelperSample.cs | Shos.CsvHelperSample.NetCore or Shos.CsvHelperSample.NetFramework

```C#
namespace CsvHelperSample.NetFramework
{
    using Shos.CsvHelper;
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.IO;
    using System.Threading.Tasks;

    enum Priority { High, Middle, Low }

    class ToDo
    {
        // public properties will be write and read as csv
        // each type should be string or enum or type which can "TryParse"

        public int      Id       { get; set; }
        public string   Title    { get; set; } = "";
        public DateTime Deadline { get; set; } = DateTime.Now;
        public bool     Done     { get; set; }
        public Priority Priority { get; set; } = Priority.Middle;
        public string   Detail   { get; set; } = "";
        [CsvIgnore()]
        public string   Option   { get; set; } = ""; // ignore this property with [CsvIgnore()]
        public string   Version => "1.0"; // read only or write only property will be ignored

        public override string ToString()
            => $"Id: {Id}, Title: {Title}, Deadline: {Deadline.ToString()}, Done: {Done}, Priority: {Priority}, Detail: {Detail}";
    }

    class ToDoList : IEnumerable<ToDo>
    {
        public IEnumerator<ToDo> GetEnumerator()
        {
            yield return new ToDo { Id = 1, Title = "filing tax returns", Deadline = new DateTime(2018, 12, 1) };
            yield return new ToDo { Id = 2, Title = "report of a business trip", Detail = "\"ASAP\"", Priority = Priority.High };
            yield return new ToDo { Id = 3, Title = "expense slips", Detail = "book expenses: \"C# 6.0 and the .NET 4.6 Framework\",\"The C# Programming\"", Priority = Priority.Low, Done = true };
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    class CsvHelperTester
    {
        public static async Task Run()
        {
            const string csvFileName = "todo.csv";

            var toDoes = new ToDoList();
            toDoes.ForEach(Console.WriteLine);
            using (var stream = new FileStream(csvFileName, FileMode.Create))
                await toDoes.WriteCsvAsync(stream);

            /*
            Result: todo.csv

            Id,Title,Deadline,Done,Priority,Detail
            1,filing tax returns,2018/12/01 0:00:00,False,Middle,
            2,report of a business trip,2017/06/22 16:16:58,False,High,"""ASAP"""
            3,expense slips,2017/06/22 16:16:58,True,Low,"book expenses: ""C# 6.0 and the .NET 4.6 Framework"",""The C# Programming"""
             */

            IEnumerable<ToDo> newToDoes;
            using (var stream = new FileStream(csvFileName, FileMode.Open))
                newToDoes = await stream.ReadCsvAsync<ToDo>();
            newToDoes.ForEach(Console.WriteLine);
        }
    }

    class Program
    {
        static void Main() => CsvHelperTester.Run().Wait();
    }
}
```


