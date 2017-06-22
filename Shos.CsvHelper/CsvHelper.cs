﻿// .NET Standard 1.3 or later
//     for .NET Network 4.6 or later, .NET Core 1.1 or later

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

    public static class EnumerableHelper
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
            var valueList = values.ToList();
            keys.ForEach((index, key) => dictionary[key] = valueList[index]);
            return dictionary;
        }

        public static IEnumerable<TElement> ToEnumerable<TElement>(this IEnumerable collection)
        {
            foreach (var element in collection)
                yield return (TElement)element;
        }
    }

    [AttributeUsage(AttributeTargets.Property)]
    public class CsvIgnoreAttribute : Attribute
    {}

    public static class CsvBuilder
    {
        const char separator       = ',' ;
        const char doubleQuoration = '\"';
        const char newLine         = '\n';
        const char carriageReturn  = '\r';

        public static string ToCsv<TElement>(this IEnumerable<TElement> collection)
        {
            var properties    = typeof(TElement).GetValidProperties();
            var stringBuilder = new StringBuilder();
            stringBuilder.AppendLine(properties.Select(property => property.Name.ToCsv()), separator);
            collection.ForEach(element => stringBuilder.AppendCsv(element, properties));
            return stringBuilder.ToString();
        }

        // for string or enum or types which can "TryParse"
        public static IEnumerable<TElement> FromCsv<TElement>(this string csv)
            where TElement : new()
        {
            var lines         = csv.Split(newLine).Where(line => line.Length > 0);
            var propertyNames = lines.FirstOrDefault()?.SplitCsv();
            return propertyNames == null
                   ? null
                   : lines.Skip(1).Select(line => line.FromCsv<TElement>(propertyNames, typeof(TElement).GetValidProperties()));
        }

        static IEnumerable<PropertyInfo> GetValidProperties(this Type type)
            => type.GetRuntimeProperties().Where(IsValidProperty);

        static bool IsValidProperty(this PropertyInfo property)
            => property.CanRead && property.CanWrite && property.GetCustomAttributes(typeof(CsvIgnoreAttribute)).Count() == 0;

        static void AppendCsv<TElement>(this StringBuilder stringBuilder, TElement element, IEnumerable<PropertyInfo> properties)
            => stringBuilder.AppendLine(properties.Select(property => property.GetValue(element).ToCsv()), separator);

        static string ToCsv(this object item) => item.ToString().ToCsv();

        static string ToCsv(this string text)
        {
            var csv = text.Replace(new string(doubleQuoration, 1), new string(doubleQuoration, 2));
            if (csv.Contains(separator) || csv.Contains(doubleQuoration))
                csv = doubleQuoration + csv + doubleQuoration;
            return csv;
        }

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

        static TItem FromCsv<TItem>(this string csv, IEnumerable<string> propertyNames, IEnumerable<PropertyInfo> properties)
            where TItem : new()
        {
            var csvTable = propertyNames.ToDictionary(csv.SplitCsv());
            var item     = new TItem();
            properties.ForEach(
                property => {
                    if (csvTable.TryGetValue(property.Name, out var propertyCsv)) {
                        var value = propertyCsv.ToValue(property.PropertyType);
                        if (value != null)
                            property.SetValue(item, value);
                    }
                }
            );
            return item;
        }

        // return value: string or enum or types which can "TryParse"
        // return null : other types
        static object ToValue(this string text, Type type)
        {
            if (type.FullName == "System.String")
                return text;
            if (type.GetTypeInfo().IsEnum)
                return text.EnumToValue(type);

            var method     = type.GetRuntimeMethod("TryParse", new Type[] { typeof(string), type.MakeByRefType() });
            var parameters = new object[] { text, null };
            return method != null && (bool)method.Invoke(null, parameters) ? parameters[1] : null;
        }

        static object EnumToValue(this string text, Type type)
            => Enum.GetValues(type).ToEnumerable<object>().FirstOrDefault(val => Enum.GetName(type, val) == text);

        static IEnumerable<string> SplitCsv(this string csv)
        {
            var stringBuilder = new StringBuilder();
            var reader        = new CsvValueReader();
            foreach (var character in csv) {
                if (character == newLine || character == carriageReturn)
                    break;
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
                switch (character) {
                    case separator:
                        return ToText(stringBuilder, ref reader);
                    case doubleQuoration:
                        reader = new CsvValueInDoubleQuotationReader();
                        break;
                    default:
                        stringBuilder.Append(character);
                        break;
                }
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
                switch (character) {
                    case separator:
                        if (readingDoubleQuotation)
                            return ToText(stringBuilder, ref reader);
                        stringBuilder.Append(character);
                        break;
                    case doubleQuoration:
                        if (readingDoubleQuotation)
                            stringBuilder.Append(character);
                        readingDoubleQuotation = !readingDoubleQuotation;
                        break;
                    default:
                        stringBuilder.Append(character);
                        break;
                }
                return null;
            }
        }
    }

    public static class CsvSerializer
    {
        public static Encoding Encoding { get; set; } = Encoding.UTF8;

        public static void WriteCsv<TElement>(this IEnumerable<TElement> collection, Stream stream)
        {
            using (var writer = new StreamWriter(stream, Encoding))
                writer.Write(collection.ToCsv());
        }

        public static async Task WriteCsvAsync<TElement>(this IEnumerable<TElement> collection, Stream stream)
        {
            using (var writer = new StreamWriter(stream, Encoding))
                await writer.WriteAsync(collection.ToCsv());
        }

        public static IEnumerable<TElement> ReadCsv<TElement>(this Stream stream)
            where TElement : new()
        {
            using (var reader = new StreamReader(stream, Encoding))
                return reader.ReadToEnd().FromCsv<TElement>();
        }

        public static async Task<IEnumerable<TElement>> ReadCsvAsync<TElement>(this Stream stream)
            where TElement : new()
        {
            using (var reader = new StreamReader(stream, Encoding))
                return (await reader.ReadToEndAsync()).FromCsv<TElement>();
        }
    }
}
