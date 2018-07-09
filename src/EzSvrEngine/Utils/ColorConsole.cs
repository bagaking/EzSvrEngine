using System;

namespace EzSvrEngine.Utils {

    public static class ColorConsole {
        public static void WriteLine<T>(ConsoleColor c, T value) {
            var color = Console.ForegroundColor;
            Console.ForegroundColor = c;
            Console.WriteLine(value);
            Console.ForegroundColor = color;
        }
    }
}
