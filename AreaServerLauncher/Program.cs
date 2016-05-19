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
            Process Aserver = null;
            if (args.Length > 0 && args[0] == "-keepopen")
            {
                bool cleanExit = false;
                do
                {
                    Aserver = Win32APIs.MutexKiller.RunProgramAndKillMutex("AREASERVER.exe", "AREA SERVER", true);
                    Console.WriteLine("Waiting untill Area Server is Closed");
                    Aserver.WaitForExit();
                    //0 = normal
                    //1 = killed by task manager
                    //-1= Aserver.Kill()
                    //255 = crash
                    Console.WriteLine("ExitCode: " + Aserver.ExitCode.ToString());
                    if (Aserver.ExitCode != 0)
                    {
                        Console.WriteLine("Detected unexpeted exit, relaunching");
                    } else
                    {
                        cleanExit = true;
                    }
                } while (cleanExit==false);
            }
            else
            {
                Aserver = Win32APIs.MutexKiller.RunProgramAndKillMutex("AREASERVER.exe", "AREA SERVER");
            }
            System.Threading.Thread.Sleep(1000);
            //Console.ReadKey();

        }
    }
}
