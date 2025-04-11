using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EyePatch
{
    internal class ConsoleWriter
    {
        public static void WriteError(string text)
        {
            WriteColoredText(text, ConsoleColor.Red);
        }

        public static void WriteSuccess(string text)
        {
            WriteColoredText(text, ConsoleColor.Green);
        }

        public static void WriteVerbose(string text)
        {
            WriteColoredText(text, ConsoleColor.Gray);
        }

        public static void WriteWarning(string text)
        {
            WriteColoredText(text, ConsoleColor.Yellow);
        }

        public static void WriteInfo(string text)
        {
            WriteColoredText(text, ConsoleColor.White);
        }

        public static void WriteNewLine()
        {
            Console.WriteLine("");
        }

        private static void WriteColoredText(string text, ConsoleColor color)
        {
            Console.ForegroundColor = color;
            Console.WriteLine(text);
            Console.ResetColor();
        }

    }
}
