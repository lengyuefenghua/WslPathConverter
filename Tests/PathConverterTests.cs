using System;

namespace WslPathConverter.Tests
{
    internal static class PathConverterTests
    {
        public static void Run(Func<string, string> converter)
        {
            AssertEqual("/mnt/c/Users/me/project", PathConverter.ConvertClipboardText("C:\\Users\\me\\project", converter));
            AssertEqual("/mnt/d/work/app", PathConverter.ConvertClipboardText("D:\\work\\app", converter));
            AssertEqual("/mnt/c/Program Files/app", PathConverter.ConvertClipboardText("\"C:\\Program Files\\app\"", converter));
            AssertEqual("plain text", PathConverter.ConvertClipboardText("plain text", converter));
        }

        private static void AssertEqual(string expected, string actual)
        {
            if (!string.Equals(expected, actual, StringComparison.Ordinal))
                throw new InvalidOperationException("Expected '" + expected + "', got '" + actual + "'.");
        }
    }
}
