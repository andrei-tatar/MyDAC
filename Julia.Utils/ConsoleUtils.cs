using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Julia.Utils
{
    public static class ConsoleUtils
    {
        private const char ArgsSeparator = ' ';

        private static OsType? _osType;
        public static OsType OsType
        {
            [MethodImpl(MethodImplOptions.Synchronized)]
            get
            {
                if (_osType == null)
                {
                    var result = Execute("cmd", "/c ver");
                    if (result.Output.Contains("Windows"))
                        _osType = OsType.Windows;
                    else
                    {
                        result = Execute("whoami");
                        if (result.Exception == null && string.IsNullOrEmpty(result.Error))
                            _osType = OsType.Linux;
                        else
                            _osType = OsType.Unknown;
                    }
                }
                return _osType.Value;
            }
        }

        public static ConsoleResult Execute(string command, params object[] args)
        {
            var task = ExecuteAsync(command, args);
            task.Wait();
            return task.Result;
        }

        public static Task<ConsoleResult> ExecuteAsync(string command, params object[] args)
        {
            return Task<ConsoleResult>.Factory.StartNew(
                () =>
                {
                    var result = new ConsoleResult();

                    try
                    {
                        var stringArgs = args.Select(a => a.ToString()).ToArray();
                        var argumentsString = stringArgs.Length == 0
                                                  ? string.Empty
                                                  : (stringArgs.Length == 1
                                                         ? stringArgs[0]
                                                         : stringArgs.Aggregate((a, b) => a + ArgsSeparator + b));

                        var processStartInfo =
                            new ProcessStartInfo(command, argumentsString)
                                {
                                    RedirectStandardOutput = true,
                                    RedirectStandardError = true,
                                    WindowStyle = ProcessWindowStyle.Hidden,
                                    CreateNoWindow = true,
                                    UseShellExecute = false,
                                    ErrorDialog = false,
                                };

                        var process = Process.Start(processStartInfo);

                        using (var myOutput = process.StandardOutput)
                        {
                            result.Output = myOutput.ReadToEnd();
                        }
                        using (var myError = process.StandardError)
                        {
                            result.Error = myError.ReadToEnd();
                        }
                        process.WaitForExit();
                    }
                    catch (Exception ex)
                    {
                        result.Exception = ex;
                    }

                    return result;
                });
        }
    }
}
