﻿using System;

namespace _01_ClearScreen
{
    class Program
    {
        static void Main(string[] args)
        {
            using (var App = new Game())
                App.Run();
            var i = 0;

            Console.WriteLine(i-=-1);
        }
    }
}
