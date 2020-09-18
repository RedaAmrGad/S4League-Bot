using System;
using System.Diagnostics;
using System.IO;

namespace S4Bot
{
	internal class Program
	{
		internal static object Discord;

		private static void Main(string[] args)
		{
			using (Bot bot = new Bot())
			{
                bot.RunAsync().Wait();
            }
           
        }
	}
}
