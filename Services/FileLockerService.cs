using System.Diagnostics;
using System.Runtime.InteropServices;
using EcoUtils.Services.Interfaces;

namespace EcoUtils.Services;

public class FileLockerService : IFileLockerService
{
    public IReadOnlyList<(int ProcessId, string ProcessName)> ObterProcessosTravando(string caminhoArquivo)
    {
        var resultado = new List<(int, string)>();

        int rv = NativeMethods.RmStartSession(out uint sessionHandle, 0, Guid.NewGuid().ToString());
        if (rv != 0) return resultado;

        try
        {
            string[] recursos = [caminhoArquivo];
            rv = NativeMethods.RmRegisterResources(sessionHandle, (uint)recursos.Length, recursos, 0, null, 0, null);
            if (rv != 0) return resultado;

            uint pnProcInfoNeeded = 0;
            uint pnProcInfo       = 0;
            uint rebootReasons    = NativeMethods.RmRebootReasonNone;

            rv = NativeMethods.RmGetList(sessionHandle, out pnProcInfoNeeded, ref pnProcInfo, null, ref rebootReasons);

            if (rv == NativeMethods.ErrorMoreData && pnProcInfoNeeded > 0)
            {
                var processInfo = new NativeMethods.RM_PROCESS_INFO[pnProcInfoNeeded];
                pnProcInfo = pnProcInfoNeeded;

                rv = NativeMethods.RmGetList(sessionHandle, out pnProcInfoNeeded, ref pnProcInfo, processInfo, ref rebootReasons);

                if (rv == 0)
                {
                    for (int i = 0; i < pnProcInfo; i++)
                    {
                        int pid = (int)processInfo[i].Process.dwProcessId;
                        try
                        {
                            var proc = Process.GetProcessById(pid);
                            resultado.Add((pid, proc.ProcessName));
                        }
                        catch { /* processo já encerrado entre a consulta e a leitura */ }
                    }
                }
            }
        }
        finally
        {
            NativeMethods.RmEndSession(sessionHandle);
        }

        return resultado;
    }

    public void EncerrarProcesso(int processId)
    {
        try
        {
            var proc = Process.GetProcessById(processId);
            proc.Kill(entireProcessTree: true);
            proc.WaitForExit(3000);
        }
        catch { /* processo já encerrado ou permissão negada — ignorado */ }
    }

    private static class NativeMethods
    {
        public const uint RmRebootReasonNone = 0;
        public const int  ErrorMoreData      = 234;

        [StructLayout(LayoutKind.Sequential)]
        public struct RM_UNIQUE_PROCESS
        {
            public uint dwProcessId;
            public System.Runtime.InteropServices.ComTypes.FILETIME ProcessStartTime;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct RM_PROCESS_INFO
        {
            public RM_UNIQUE_PROCESS Process;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
            public string strAppName;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
            public string strServiceShortName;

            public uint ApplicationType;
            public uint AppStatus;
            public uint TSSessionId;

            [MarshalAs(UnmanagedType.Bool)]
            public bool bRestartable;
        }

        [DllImport("rstrtmgr.dll", CharSet = CharSet.Unicode)]
        public static extern int RmStartSession(
            out uint pSessionHandle,
            int      dwSessionFlags,
            string   strSessionKey);

        [DllImport("rstrtmgr.dll")]
        public static extern int RmEndSession(uint pSessionHandle);

        [DllImport("rstrtmgr.dll", CharSet = CharSet.Unicode)]
        public static extern int RmRegisterResources(
            uint             pSessionHandle,
            uint             nFiles,
            string[]         rgsFilenames,
            uint             nApplications,
            [In] RM_UNIQUE_PROCESS[]? rgApplications,
            uint             nServices,
            string[]?        rgsServiceNames);

        [DllImport("rstrtmgr.dll")]
        public static extern int RmGetList(
            uint                         dwSessionHandle,
            out uint                     pnProcInfoNeeded,
            ref uint                     pnProcInfo,
            [In, Out] RM_PROCESS_INFO[]? rgAffectedApps,
            ref uint                     lpdwRebootReasons);
    }
}
