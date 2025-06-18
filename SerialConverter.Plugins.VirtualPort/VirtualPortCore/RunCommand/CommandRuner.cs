using System.Diagnostics;

namespace SerialConverter.Plugins.VirtualSerial.VirtualPortCore.RunCommand
{
    public class CommandRuner
    {
       private static string Basepath = System.AppDomain.CurrentDomain.BaseDirectory;
       /// <summary>
       /// 运行命令
       /// </summary>
       /// <param name="args"></param>
       /// <param name="MaxWaitTime"></param>
       /// <returns></returns>
       /// <exception cref="ApplicationException"></exception>
        public static string[] RunCommandWithProcess(string args,double MaxWaitTime = 60)
        {
            string path = Basepath;
            if (Environment.Is64BitOperatingSystem) path = Path.Combine(path, "com0com", "amd64");
            else path = Path.Combine(path, "com0com", "i386");
            if (!Directory.Exists(path)) throw new DirectoryNotFoundException();
            string setupc = Path.Combine(path, "setupc.exe");
            if (!File.Exists(setupc)) throw new FileNotFoundException("未找到com0com执行文件setupc.exe");
            Process proc = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    WorkingDirectory = path,
                    FileName = setupc,
                    Arguments = args,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true,
                }
            };
            proc.Start();
            while (!proc.HasExited) 
            {
                MaxWaitTime -= 0.2;
                if(MaxWaitTime>0) Thread.Sleep(200);
                else
                {
                    proc.Kill();
                    throw new Exception("执行命令超时!");
                }
            }

            if (proc.ExitCode != 0)
                throw new ApplicationException($"Exit code of {proc.ExitCode} received when running '{setupc} {args}'");
            var ret = new List<string>();
            while (!proc.StandardOutput.EndOfStream)
            {
                string? line = proc.StandardOutput.ReadLine();
                if(!string.IsNullOrEmpty(line)) ret.Add(line);
            }
            return ret.ToArray();
        }
    }
}
