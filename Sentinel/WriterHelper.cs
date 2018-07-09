namespace Sentinel
{
    using System;
    using System.Threading;

    public static class WriterHelper
    {
        public static void Writeheader()
        {
            Console.SetWindowSize(80, 30);

            Console.WriteLine(@"***********************************************************");
            Console.WriteLine(@"************   S.  E.  N.  T.  I.  N.  E.  L.  ************");
            Console.WriteLine(@"************   h   n   e   r   n   i   n   a   ************");
            Console.WriteLine(@"************   o   t   t   a       c   v   y   ************");
            Console.WriteLine(@"************   w   i   w   f       e   i   o   ************");
            Console.WriteLine(@"************       r   o   f           r   u   ************");
            Console.WriteLine(@"************       e   r   i           o   t   ************");
            Console.WriteLine(@"************           k   c           n       ************");
            Console.WriteLine(@"************                           m       ************");
            Console.WriteLine(@"************                           e       ************");
            Console.WriteLine(@"************                           n       ************");
            Console.WriteLine(@"************                           t       ************");
            Console.WriteLine(@"***********************************************************");
        }

        public static void WriteInputError(string input, int line, string errorMessage)
        {            
            Console.WriteLine(errorMessage);
            Thread.Sleep(1500);
            Console.SetCursorPosition(0, line);
            Console.WriteLine(" ".PadRight(input.Length));
            Console.WriteLine(" ".PadRight(errorMessage.Length));
        }

        public static void WriteInfo(int line, int column = 0, string text = "")
        {
            Console.SetCursorPosition(column, line);
            Console.WriteLine(" ".PadRight(50));
            Console.SetCursorPosition(column, line);
            Console.WriteLine(text);
        }
    }
}
