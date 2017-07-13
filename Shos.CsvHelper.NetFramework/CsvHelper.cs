// Csv(comma-separated values) Library
//
// .NET Standard 1.3 or later
//     for .NET Network 4.6 or later, .NET Core 1.1 or later
//
// .NET Framework 4.5.2 or later

namespace Shos.CsvHelper
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Text;
    using System.Threading.Tasks;

    static class EnumerableHelper
    {
        public static void ForEach<TElement>(this IEnumerable<TElement> collection, Action<TElement> action)
        {
            foreach (var element in collection)
                action(element);
        }

        public static void ForEach<TElement>(this IEnumerable<TElement> collection, Action<int, TElement> action)
        {
            var index = 0;
            foreach (var element in collection)
                action(index++, element);
        }

        public static Dictionary<TKey, TValue> ToDictionary<TKey, TValue>(this IEnumerable<TKey> keys, IEnumerable<TValue> values)
        {
            var dictionary = new Dictionary<TKey, TValue>();
            var valueList  = values.ToList();
            keys.ForEach((index, key) => dictionary[key] = valueList[index]);
            return dictionary;
        }

        public static IEnumerable<TElement> ToEnumerable<TElement>(this IEnumerable collection)
        {
            foreach (var element in collection)
                yield return (TElement)element;
        }

        public static void ForEachObject(this IEnumerable collection, Action<object> action)
        {
            foreach (var element in collection)
                action(element);
        }

        public static object FirstObjectOrDefault(this IEnumerable collection)
        {
            var enumerator = collection.GetEnumerator();
            return enumerator.MoveNext() ? enumerator.Current : null;
        }
    }

    [AttributeUsage(AttributeTargets.Property)]
    public class CsvIgnoreAttribute : Attribute
    {}

    [AttributeUsage(AttributeTargets.Property)]
    public class ColumnNameAttribute : Attribute
    {
        public string Value { get; private set; }
        public ColumnNameAttribute(string value) => Value = value;
    }

    static class CsvBuilder
    {
        public static char Separator { get; set; }  = comma;

        const char comma           = ',' ;
        const char doubleQuoration = '\"';
        const char newLine         = '\n';
        const char carriageReturn  = '\r';

        // header is recommended
        public static string ToCsv<TElement>(this IEnumerable<TElement> collection, bool hasHeader = true)
        {
            var properties    = typeof(TElement).GetValidProperties();
            var stringBuilder = new StringBuilder();
            if (hasHeader)
                stringBuilder.AppendLine(properties.Select(property => property.ColumnName().ToCsv()), Separator);
            collection.ForEach(element => stringBuilder.AppendCsv(element, properties));
            return stringBuilder.ToString();
        }

        // header is recommended
        public static string ObjectsToCsv(this IEnumerable collection, bool hasHeader = true)
        {
            var firstElement = collection.FirstObjectOrDefault();
            if (firstElement == null)
                return "";

            var properties    = firstElement.GetType().GetValidProperties();
            var stringBuilder = new StringBuilder();
            if (hasHeader)
                stringBuilder.AppendLine(properties.Select(property => property.ColumnName().ToCsv()), Separator);
            collection.ForEachObject(element => stringBuilder.AppendCsv(element, properties));
            return stringBuilder.ToString();
        }

        public static IEnumerable<TElement> FromCsv<TElement>(this string csv, bool hasHeader = true)
            where TElement : new()
        {
            var lines = csv.SplitToLines();
            if (hasHeader) {
                var columnNames = lines.FirstOrDefault()?.SplitCsv();
                return columnNames == null
                       ? null
                       : lines.Skip(1).Select(line => line.FromCsv<TElement>(columnNames, typeof(TElement).GetValidProperties()));
            }
            return lines.Select(line => line.FromCsv<TElement>(typeof(TElement).GetValidProperties()));
        }

        static IEnumerable<string> SplitToLines(this string csv)
        {
            bool readingDoubleQuotation = false;
            var stringBuilder           = new StringBuilder();
            foreach (var character in csv) {
                if (character == doubleQuoration) {
                    readingDoubleQuotation = !readingDoubleQuotation;
                } else if (character == newLine || character == carriageReturn) {
                    if (!readingDoubleQuotation) {
                        var line = stringBuilder.ToString();
                        stringBuilder.Clear();
                        if (line.Length > 0)
                            yield return line;
                        continue;
                    }
                }
                stringBuilder.Append(character);
            }
            var lastLine = stringBuilder.ToString();
            if (lastLine.Length > 0)
                yield return lastLine;
        }

        static IEnumerable<PropertyInfo> GetValidProperties(this Type type)
            => type.GetRuntimeProperties().Where(IsValid);

        static bool IsValid(this PropertyInfo property)
            => property.CanRead && property.CanWrite && property.GetCustomAttributes(typeof(CsvIgnoreAttribute)).Count() == 0;

        static string ColumnName(this PropertyInfo property)
            => ((ColumnNameAttribute)(property.GetCustomAttributes(typeof(ColumnNameAttribute)).SingleOrDefault()))?.Value ?? property.Name;

        static void AppendCsv<TElement>(this StringBuilder stringBuilder, TElement element, IEnumerable<PropertyInfo> properties)
            => stringBuilder.AppendLine(properties.Select(property => property.GetValue(element).ToCsv()), Separator);

        static string ToCsv(this object item) => item.ToString().ToCsv();

        static string ToCsv(this string text)
        {
            var csv = text.Replace(new string(doubleQuoration, 1), new string(doubleQuoration, 2));
            return csv.NeedsDoubleQuorations()
                   ? doubleQuoration + csv + doubleQuoration
                   : csv;
        }

        static bool NeedsDoubleQuorations(this string text)
            => text.Any(character => character == Separator || character == comma || character == doubleQuoration || character == newLine || character == carriageReturn);

        static void AppendLine(this StringBuilder stringBuilder, IEnumerable<string> texts, char separator)
        {
            texts.ForEach(
                (index, text) => {
                    if (index != 0)
                        stringBuilder.Append(separator);
                    stringBuilder.Append(text);
                }
            );
            stringBuilder.Append(newLine);
        }

        // for Csv with header
        // type of each property should have "get" and "set" and should be string or enum or type which has a default constructor and can "TryParse" or "Parse"
        static TItem FromCsv<TItem>(this string csv, IEnumerable<string> columnNames, IEnumerable<PropertyInfo> properties)
            where TItem : new()
        {
            var csvTable = columnNames.ToDictionary(csv.SplitCsv());
            var item     = new TItem();
            properties.ForEach(
                property => {
                    if (csvTable.TryGetValue(property.ColumnName(), out var columnCsv)) {
                        var value = columnCsv.ToValue(property.PropertyType);
                        if (value != null)
                            property.SetValue(item, value);
                    }
                }
            );
            return item;
        }

        // for Csv without header
        // type of each property should have "get" and "set" and should be string or enum or type which has a default constructor and can "TryParse" or "Parse"
        static TItem FromCsv<TItem>(this string csv, IEnumerable<PropertyInfo> properties)
            where TItem : new()
        {
            var columnCsvs = csv.SplitCsv().ToList();
            var item       = new TItem();
            properties.ForEach(
                (index, property) => {
                    if (index < columnCsvs.Count) {
                        var value = columnCsvs[index].ToValue(property.PropertyType);
                        if (value != null)
                            property.SetValue(item, value);
                    }
                }
            );
            return item;
        }

        // return value: string or enum or types which can "TryParse" or "Parse"
        // return null : other types
        static object ToValue(this string text, Type type)
        {
            if (type.FullName == "System.String")
                return text;
            if (type.GetTypeInfo().IsEnum)
                return text.EnumToValue(type);
            return type.DoParse(text);
        }

        static object EnumToValue(this string text, Type type)
            => Enum.GetValues(type).ToEnumerable<object>().FirstOrDefault(val => Enum.GetName(type, val) == text);

        static object DoParse(this Type type, string text)
        {
            object value;
            return type.TryParse(text, out value) || type.Parse(text, out value) ? value : null;
        }

        static bool TryParse(this Type type, string text, out object value)
        {
            var method     = type.GetRuntimeMethod("TryParse", new Type[] { typeof(string), type.MakeByRefType() });
            var parameters = new object[] { text, null };
            if (method != null && (bool)method.Invoke(null, parameters)) {
                value = parameters[1];
                return true;
            }
            value = null;
            return false;
        }

        static bool Parse(this Type type, string text, out object value)
        {
            var method    = type.GetRuntimeMethod("Parse", new Type[] { typeof(string) });
            var parameter = new object[] { text };
            if (method != null) {
                try {
                    value = method.Invoke(null, parameter);
                    return true;
                } catch {
                }
            }
            value = null;
            return false;
        }

        static IEnumerable<string> SplitCsv(this string csv)
        {
            var stringBuilder = new StringBuilder();
            var reader        = new CsvValueReader();
            foreach (var character in csv) {
                var itemText = reader.Read(stringBuilder, character, ref reader);
                if (itemText != null)
                    yield return itemText;
            }
            yield return stringBuilder.ToString();
        }

        class CsvValueReader
        {
            public virtual string Read(StringBuilder stringBuilder, char character, ref CsvValueReader reader)
            {
                if (character == Separator      )
                    return ToText(stringBuilder, ref reader);
                if (character == doubleQuoration)
                    reader = new CsvValueInDoubleQuotationReader();
                else
                    stringBuilder.Append(character);
                return null;
            }

            protected static string ToText(StringBuilder stringBuilder, ref CsvValueReader reader)
            {
                var text = stringBuilder.ToString();
                stringBuilder.Clear();
                reader   = new CsvValueReader();
                return text;
            }
        }

        class CsvValueInDoubleQuotationReader : CsvValueReader
        {
            bool readingDoubleQuotation = false;

            public override string Read(StringBuilder stringBuilder, char character, ref CsvValueReader reader)
            {
                if        (character == Separator      ) {
                    if (readingDoubleQuotation)
                        return ToText(stringBuilder, ref reader);
                    stringBuilder.Append(character);
                } else if (character == doubleQuoration) {
                    if (readingDoubleQuotation)
                        stringBuilder.Append(character);
                    readingDoubleQuotation = !readingDoubleQuotation;
                } else {
                    stringBuilder.Append(character);
                }
                return null;
            }
        }
    }

    public static class CsvSerializer
    {
        public static Encoding Encoding { get; set; } = Encoding.UTF8;

        public static char Separator {
            get => CsvBuilder.Separator        ;
            set => CsvBuilder.Separator = value;
        }

        // write IEnumerable<TElement> to a csv file
        // header is recommended
        public static void WriteCsv<TElement>(this IEnumerable<TElement> collection, Stream stream, bool hasHeader = true)
        {
            using (var writer = new StreamWriter(stream, Encoding))
                writer.Write(collection.ToCsv(hasHeader));
        }

        // write IEnumerable to a csv file
        // header is recommended
        public static void WriteCsv(this IEnumerable collection, Stream stream, bool hasHeader = true)
        {
            using (var writer = new StreamWriter(stream, Encoding))
                writer.Write(collection.ObjectsToCsv(hasHeader));
        }

        // write IEnumerable<TElement> to a csv file asynchronously
        // header is recommended
        public static async Task WriteCsvAsync<TElement>(this IEnumerable<TElement> collection, Stream stream, bool hasHeader = true)
        {
            using (var writer = new StreamWriter(stream, Encoding))
                await writer.WriteAsync(collection.ToCsv(hasHeader));
        }

        // write IEnumerable to a csv file asynchronously
        // header is recommended
        public static async Task WriteCsvAsync(this IEnumerable collection, Stream stream, bool hasHeader = true)
        {
            using (var writer = new StreamWriter(stream, Encoding))
                await writer.WriteAsync(collection.ObjectsToCsv(hasHeader));
        }

        // write IEnumerable<TElement> to a csv file
        // header is recommended
        public static void WriteCsv<TElement>(this IEnumerable<TElement> collection, string csvFilePathName, bool hasHeader = true)
        {
            using (var stream = new FileStream(csvFilePathName, FileMode.Create))
                collection.WriteCsv(stream, hasHeader);
        }

        // write IEnumerable to a csv file
        // header is recommended
        public static void WriteCsv(this IEnumerable collection, string csvFilePathName, bool hasHeader = true)
        {
            using (var stream = new FileStream(csvFilePathName, FileMode.Create))
                collection.WriteCsv(stream, hasHeader);
        }

        // write IEnumerable<TElement> to a csv file asynchronously
        // header is recommended
        public static async Task WriteCsvAsync<TElement>(this IEnumerable<TElement> collection, string csvFilePathName, bool hasHeader = true)
        {
            using (var stream = new FileStream(csvFilePathName, FileMode.Create))
                await collection.WriteCsvAsync(stream, hasHeader);
        }

        // write IEnumerable to a csv file asynchronously
        // header is recommended
        public static async Task WriteCsvAsync(this IEnumerable collection, string csvFilePathName, bool hasHeader = true)
        {
            using (var stream = new FileStream(csvFilePathName, FileMode.Create))
                await collection.WriteCsvAsync(stream, hasHeader);
        }

        // read IEnumerable<TElement> from a csv file
        // TElement:
        // public properties of TElement will be written and read as csv
        // for writing: type of each property should have "get" and "set"
        // for reading: type of each property should have "get" and "set" and should be string or enum or type which has a default constructor and can "TryParse" or "Parse"
        public static IEnumerable<TElement> ReadCsv<TElement>(this Stream stream, bool hasHeader = true)
            where TElement : new()
        {
            using (var reader = new StreamReader(stream, Encoding))
                return reader.ReadToEnd().FromCsv<TElement>(hasHeader);
        }

        // read IEnumerable<TElement> from a csv file asynchronously
        // TElement:
        // public properties of TElement will be written and read as csv
        // for writing: type of each property should have "get" and "set"
        // for reading: type of each property should have "get" and "set" and should be string or enum or type which has a default constructor and can "TryParse" or "Parse"
        public static async Task<IEnumerable<TElement>> ReadCsvAsync<TElement>(this Stream stream, bool hasHeader = true)
            where TElement : new()
        {
            using (var reader = new StreamReader(stream, Encoding))
                return (await reader.ReadToEndAsync()).FromCsv<TElement>(hasHeader);
        }

        // read IEnumerable<TElement> from a csv file
        // TElement:
        // public properties of TElement will be written and read as csv
        // for writing: type of each property should have "get" and "set"
        // for reading: type of each property should have "get" and "set" and should be string or enum or type which has a default constructor and can "TryParse" or "Parse"
        public static IEnumerable<TElement> ReadCsv<TElement>(string csvFilePathName, bool hasHeader = true)
            where TElement : new()
        {
            using (var stream = new FileStream(csvFilePathName, FileMode.Open))
                return stream.ReadCsv<TElement>(hasHeader);
        }

        // read IEnumerable<TElement> from a csv file asynchronously
        // TElement:
        // public properties of TElement will be written and read as csv
        // for writing: type of each property should have "get" and "set"
        // for reading: type of each property should have "get" and "set" and should be string or enum or type which has a default constructor and can "TryParse" or "Parse"
        public static async Task<IEnumerable<TElement>> ReadCsvAsync<TElement>(string csvFilePathName, bool hasHeader = true)
            where TElement : new()
        {
            using (var stream = new FileStream(csvFilePathName, FileMode.Open))
                return await stream.ReadCsvAsync<TElement>(hasHeader);
        }
    }
}
