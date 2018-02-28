using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Disconf.Net.Test
{
    class Program
    {
        static void Main(string[] args)
        {
            Reload:

            try
            {
                DisconfMgr.Init();
                Console.WriteLine();
            }
            catch (Exception ex)
            {
                Logger.Error("Disconf.Net.Test", ex);
            }

            var keys = Console.ReadKey();
            if (keys.Key != ConsoleKey.Escape)
            {
                goto Reload;
            }
        }
    }
}
