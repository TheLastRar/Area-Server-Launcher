using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Text;

//99% code taken from http://stackoverflow.com/questions/6808831/delete-a-mutex-from-another-process

//I've just added/adjusted code to launch the area-server
//Also fixed reference leak
namespace Win32APIs
{
    public class Win32Processes
    {
        const int CNST_SYSTEM_HANDLE_INFORMATION = 16;
        const uint STATUS_INFO_LENGTH_MISMATCH = 0xc0000004;

        public static string getObjectTypeName(Win32API.SYSTEM_HANDLE_INFORMATION shHandle, Process process)
        {
            //Get handle to process that is usable for WinAPI use.
            IntPtr m_ipProcessHwnd = Win32API.OpenProcess(Win32API.ProcessAccessFlags.All, false, process.Id);
            //Stores a local handle to whatever object we are getting the ObjectType of.
            IntPtr ipHandle = IntPtr.Zero;
            var objBasic = new Win32API.OBJECT_BASIC_INFORMATION();
            IntPtr ipBasic = IntPtr.Zero;
            var objObjectType = new Win32API.OBJECT_TYPE_INFORMATION();
            IntPtr ipObjectType = IntPtr.Zero;
            IntPtr ipObjectName = IntPtr.Zero;
            string strObjectTypeName = "";
            int nLength = 0;
            int nReturn = 0;
            IntPtr ipTemp = IntPtr.Zero;

            //Attempt to get a local handle to the object 
            if (!Win32API.DuplicateHandle(m_ipProcessHwnd, shHandle.Handle,
                                            Win32API.GetCurrentProcess(), out ipHandle,
                                            0, false, Win32API.DUPLICATE_SAME_ACCESS))
            {
                //clean up
                Win32API.CloseHandle(m_ipProcessHwnd);
                return null;
            }
            //clean up
            Win32API.CloseHandle(m_ipProcessHwnd);

            //allocate space for OBJECT_BASIC_INFORMATION
            ipBasic = Marshal.AllocHGlobal(Marshal.SizeOf(objBasic));
            //Get the object's basic information
            Win32API.NtQueryObject(ipHandle, (int)Win32API.ObjectInformationClass.ObjectBasicInformation,
                                    ipBasic, Marshal.SizeOf(objBasic), ref nLength);
            objBasic = (Win32API.OBJECT_BASIC_INFORMATION)Marshal.PtrToStructure(ipBasic, objBasic.GetType());
            Marshal.FreeHGlobal(ipBasic);

            //allocate space for OBJECT_TYPE_INFORMATION
            //Adjusting amount if needed untill call succeeds
            ipObjectType = Marshal.AllocHGlobal(objBasic.TypeInformationLength);
            nLength = objBasic.TypeInformationLength;
            while ((uint)(nReturn = Win32API.NtQueryObject(
                ipHandle, (int)Win32API.ObjectInformationClass.ObjectTypeInformation, ipObjectType,
                    nLength, ref nLength)) ==
                Win32API.STATUS_INFO_LENGTH_MISMATCH)
            {
                Marshal.FreeHGlobal(ipObjectType);
                ipObjectType = Marshal.AllocHGlobal(nLength);
            }
            
            objObjectType = (Win32API.OBJECT_TYPE_INFORMATION)Marshal.PtrToStructure(ipObjectType, objObjectType.GetType());
            //Get pointer to the object type (string) name
            if (Is64Bits())
            {
                //the fuck?
                ipTemp = new IntPtr(Convert.ToInt64(objObjectType.Name.Buffer.ToString(), 10) >> 32);
            }
            else
            {
                ipTemp = objObjectType.Name.Buffer;
            }

            strObjectTypeName = Marshal.PtrToStringUni(ipTemp, objObjectType.Name.Length >> 1);
            Marshal.FreeHGlobal(ipObjectType);

            //clean up
            Win32API.CloseHandle(ipHandle);

            return strObjectTypeName;
        }

        public static string getObjectName(Win32API.SYSTEM_HANDLE_INFORMATION shHandle, Process process)
        {
            //Get handle to process that is usable for WinAPI use.
            IntPtr m_ipProcessHwnd = Win32API.OpenProcess(Win32API.ProcessAccessFlags.All, false, process.Id);
            //Stores a local handle to whatever object we are getting the name of.
            IntPtr ipHandle = IntPtr.Zero;
            var objBasic = new Win32API.OBJECT_BASIC_INFORMATION();
            IntPtr ipBasic = IntPtr.Zero;
            IntPtr ipObjectType = IntPtr.Zero;
            var objObjectName = new Win32API.OBJECT_NAME_INFORMATION();
            IntPtr ipObjectName = IntPtr.Zero;
            string strObjectName = "";
            int nLength = 0;
            int nReturn = 0;
            IntPtr ipTemp = IntPtr.Zero;

            //Attempt to get a local handle to the object
            if (!Win32API.DuplicateHandle(m_ipProcessHwnd, shHandle.Handle, Win32API.GetCurrentProcess(),
                                            out ipHandle, 0, false, Win32API.DUPLICATE_SAME_ACCESS))
            {
                //clean up
                Win32API.CloseHandle(m_ipProcessHwnd);
                return null;
            }
            //clean up
            Win32API.CloseHandle(m_ipProcessHwnd);

            //allocate space for OBJECT_BASIC_INFORMATION
            ipBasic = Marshal.AllocHGlobal(Marshal.SizeOf(objBasic));
            //Get the object's basic information
            Win32API.NtQueryObject(ipHandle, (int)Win32API.ObjectInformationClass.ObjectBasicInformation,
                                    ipBasic, Marshal.SizeOf(objBasic), ref nLength);
            objBasic = (Win32API.OBJECT_BASIC_INFORMATION)Marshal.PtrToStructure(ipBasic, objBasic.GetType());
            Marshal.FreeHGlobal(ipBasic);


            nLength = objBasic.NameInformationLength;
            //allocate space for ObjectInformationClass
            ipObjectName = Marshal.AllocHGlobal(nLength);
            //attempt to retrive ObjectInformationClass, adjusting allocated space as needed
            while ((uint)(nReturn = Win32API.NtQueryObject(
                        ipHandle, (int)Win32API.ObjectInformationClass.ObjectNameInformation,
                        ipObjectName, nLength, ref nLength))
                    == Win32API.STATUS_INFO_LENGTH_MISMATCH)
            {
                Marshal.FreeHGlobal(ipObjectName);
                ipObjectName = Marshal.AllocHGlobal(nLength);
            }
            objObjectName = (Win32API.OBJECT_NAME_INFORMATION)Marshal.PtrToStructure(ipObjectName, objObjectName.GetType());

            //Get pointer to the object (string) name
            if (Is64Bits())
            {
                //the fuck?
                ipTemp = new IntPtr(Convert.ToInt64(objObjectName.Name.Buffer.ToString(), 10) >> 32);
            }
            else
            {
                ipTemp = objObjectName.Name.Buffer;
            }

            if (ipTemp != IntPtr.Zero) //Check if object has a name
            {

                byte[] baTemp2 = new byte[nLength];
                try
                {
                    Marshal.Copy(ipTemp, baTemp2, 0, nLength);

                    //Get name from pointer
                    strObjectName = Marshal.PtrToStringUni(Is64Bits() ?
                                                            new IntPtr(ipTemp.ToInt64()) :
                                                            new IntPtr(ipTemp.ToInt32()));
                    return strObjectName;
                }
                catch (AccessViolationException)
                {
                    return null;
                }
                finally
                {
                    Marshal.FreeHGlobal(ipObjectName);
                    Win32API.CloseHandle(ipHandle);
                }
            }
            return null;
        }

        public static List<Win32API.SYSTEM_HANDLE_INFORMATION>
        GetHandles(Process process = null, string IN_strObjectTypeName = null, string IN_strObjectName = null)
        {
            uint nStatus;
            int nHandleInfoSize = 0x10000;
            IntPtr ipHandlePointer = Marshal.AllocHGlobal(nHandleInfoSize);
            int nLength = 0;
            IntPtr ipHandle = IntPtr.Zero;

            //undocumented (ish) function that can change from OS to OS...
            //Adjust size of allocated buffer untill it is the correct size
            //to store SYSTEM_HANDLE_INFORMATION (at which point we then get said information)
            while ((nStatus = Win32API.NtQuerySystemInformation(CNST_SYSTEM_HANDLE_INFORMATION, ipHandlePointer,
                                                                nHandleInfoSize, ref nLength)) ==
                    STATUS_INFO_LENGTH_MISMATCH)
            {
                nHandleInfoSize = nLength;
                Marshal.FreeHGlobal(ipHandlePointer);
                ipHandlePointer = Marshal.AllocHGlobal(nLength);
            }

            byte[] baTemp = new byte[nLength]; //Is this used?
            Marshal.Copy(ipHandlePointer, baTemp, 0, nLength);

            
            long lHandleCount = 0;
            if (Is64Bits())
            {
                //Get HandleCount (stored as the 1st number in the structure)
                lHandleCount = Marshal.ReadInt64(ipHandlePointer);
                //Then get a pointer to 1st SYSTEM_HANDLE_INFORMATION in the list
                ipHandle = new IntPtr(ipHandlePointer.ToInt64() + 8);
            }
            else
            {
                //Get HandleCount (stored as the 1st number in the structure)
                lHandleCount = Marshal.ReadInt32(ipHandlePointer);
                //Then get a pointer to 1st SYSTEM_HANDLE_INFORMATION in the list
                ipHandle = new IntPtr(ipHandlePointer.ToInt32() + 4);
            }

            Win32API.SYSTEM_HANDLE_INFORMATION shHandle;
            List<Win32API.SYSTEM_HANDLE_INFORMATION> lstHandles = new List<Win32API.SYSTEM_HANDLE_INFORMATION>();

            //Iterate over all SYSTEM_HANDLE_INFORMATION pointers
            for (long lIndex = 0; lIndex < lHandleCount; lIndex++)
            {
                shHandle = new Win32API.SYSTEM_HANDLE_INFORMATION();
                if (Is64Bits())
                {
                    //Read data into a SYSTEM_HANDLE_INFORMATION structure
                    shHandle = (Win32API.SYSTEM_HANDLE_INFORMATION)Marshal.PtrToStructure(ipHandle, shHandle.GetType());
                    //Set pointer to next entry in list
                    ipHandle = new IntPtr(ipHandle.ToInt64() + Marshal.SizeOf(shHandle) + 8);
                }
                else
                {
                    //Read data into a SYSTEM_HANDLE_INFORMATION structure
                    ipHandle = new IntPtr(ipHandle.ToInt64() + Marshal.SizeOf(shHandle));
                    //Set pointer to next entry in list
                    shHandle = (Win32API.SYSTEM_HANDLE_INFORMATION)Marshal.PtrToStructure(ipHandle, shHandle.GetType());
                }

                //Filter out any handles not relating to the specified process
                if (process != null)
                {
                    if (shHandle.ProcessID != process.Id) continue;
                }

                //filter out any handles that do not point to an object of the specified type
                string strObjectTypeName = "";
                if (IN_strObjectTypeName != null)
                {
                    strObjectTypeName = getObjectTypeName(shHandle, Process.GetProcessById(shHandle.ProcessID));
                    if (strObjectTypeName != IN_strObjectTypeName) continue;
                }

                //filter out any handles that do not point to an object of the specified name
                string strObjectName = "";
                if (IN_strObjectName != null)
                {
                    strObjectName = getObjectName(shHandle, Process.GetProcessById(shHandle.ProcessID));
                    if (strObjectName != IN_strObjectName) continue;
                }

                string strObjectTypeName2 = getObjectTypeName(shHandle, Process.GetProcessById(shHandle.ProcessID));
                string strObjectName2 = getObjectName(shHandle, Process.GetProcessById(shHandle.ProcessID));
                Console.WriteLine("{0}   {1}   {2}", shHandle.ProcessID, strObjectTypeName2, strObjectName2);

                //Add to the list
                lstHandles.Add(shHandle);
            }
            return lstHandles;
        }

        public static bool Is64Bits()
        {
            return Marshal.SizeOf(typeof(IntPtr)) == 8 ? true : false;
        }
    }

    public class MutexKiller
    {
        public static Process RunProgramAndKillMutex(string ProgramPath, string MutexName)
        {
            ProcessStartInfo pStarti = new ProcessStartInfo(ProgramPath);
            Process process = Process.Start(pStarti);
            try
            {
                process.WaitForInputIdle();
                //Get all handles for the type + oject name we want form a given process
                List<Win32API.SYSTEM_HANDLE_INFORMATION> handles = Win32Processes.GetHandles(process, "Mutant", "\\Sessions\\1\\BaseNamedObjects\\" + MutexName);
                Console.WriteLine(handles.Count);
                if (handles.Count == 0) throw new System.ArgumentException("NoMutex", "original");
                foreach (Win32API.SYSTEM_HANDLE_INFORMATION handle in handles)
                {
                    IntPtr ipHandle = IntPtr.Zero;
                    if (!Win32API.DuplicateHandle(Process.GetProcessById(handle.ProcessID).Handle, handle.Handle, Win32API.GetCurrentProcess(), out ipHandle, 0, false, Win32API.DUPLICATE_CLOSE_SOURCE))
                        Console.WriteLine("DuplicateHandle() failed, error = {0}", Marshal.GetLastWin32Error());

                    Win32API.CloseHandle(ipHandle);
                    Console.WriteLine("Mutex was killed");

                }
            }
            catch (IndexOutOfRangeException)
            {
                Console.WriteLine("The process name '{0}' is not currently running", "Area Server");
            }
            catch (ArgumentException)
            {
                Console.WriteLine("The Mutex '{0}' was not found in the process '{1}'", MutexName, "Area Server");
            }
            return process;
        }
    }
}
