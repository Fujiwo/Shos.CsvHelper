using System;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace ReadToEndPerformanceTest
{
    class StopwatchViewer : IDisposable
    {
        Stopwatch stopwatch = new Stopwatch();
        public Action<string> WriteLine = Console.WriteLine;
        public StopwatchViewer() => stopwatch.Start();
        public void Dispose()
        {
            stopwatch.Stop();
            ShowResult();
        }
        void ShowResult()
            => WriteLine?.Invoke($"({stopwatch.ElapsedMilliseconds / 1000.0}s.)");
    }

    class Program
    {
        const string  path       = @"test.txt";
        const int     length     = 10000000;
        const int     partLength = 100;
        static Random random     = new Random();

        static void Main(string[] args)
        {
            Console.WriteLine(nameof(WriteTest));
            WriteTest();
            Console.WriteLine(nameof(ReadTest1));
            var text1 = ReadTest1();
            Console.WriteLine($"Length: {text1.Length}, Text: {text1.Substring(0, partLength)}");
            Console.WriteLine(nameof(ReadTest2));
            var text2 = ReadTest2();
            Console.WriteLine($"Length: {text2.Length}, Text: {text2.Substring(0, partLength)}");
            Console.WriteLine(nameof(ReadTest3));
            var text3 = ReadTest3();
            Console.WriteLine($"Length: {text3.Length}, Text: {text3.Substring(0, partLength)}");
        }

        static void WriteTest()
        {
            var text = RandomString(length);
            using var writer = new StreamWriter(path);
            using var stopwatchViewer = new StopwatchViewer();
            writer.WriteLine(text);
            Console.WriteLine(text.Substring(0, partLength));
        }

        static string ReadTest1()
        {
            using var reader          = new StreamReader(path);
            using var stopwatchViewer = new StopwatchViewer();
            var text = reader.ReadToEnd();
            return text;
        }

        static string ReadTest2()
        {
            using var reader          = new StreamReader(path);
            using var stopwatchViewer = new StopwatchViewer();

            var stringBuilder         = new StringBuilder();
            for (; reader.Peek() >= 0; stringBuilder.Append("\n"))
                stringBuilder.Append(reader.ReadLine());
            return stringBuilder.ToString();
        }

        static string ReadTest3()
        {
            using var reader          = new StreamReader(path);
            using var stopwatchViewer = new StopwatchViewer();

            var stringBuilder         = new StringBuilder();
            while (reader.Peek() >= 0)
                stringBuilder.Append((char)reader.Read());
            return stringBuilder.ToString();
        }

        static string RandomString(int length)
        {
            var stringBuilder = new StringBuilder();
            for (var count = 0; count < length; count++) {
                var character = RandomCharacter();
                if (character == 0x7f)
                    stringBuilder.Append("\n");
                else
                    stringBuilder.Append(character);
            }
            return stringBuilder.ToString();
        }

        static char RandomCharacter() => (char)random.Next((int)' ', 0x7f + 1);
    }
}
