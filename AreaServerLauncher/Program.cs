using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Text;

namespace AreaServerThing
{
    class Program
    {
        static void Main(string[] args)
        {
            Process Aserver = Win32APIs.MutexKiller.RunProgramAndKillMutex("AREA SERVER.exe", "AREA SERVER");
            if (args.Length > 0 && args[0] == "-keepopen")
            {
                Console.WriteLine("Waiting untill Area Server is Closed");
                Aserver.WaitForExit();
            }
            System.Threading.Thread.Sleep(1000);
            //Console.ReadKey();
        }
    }
}