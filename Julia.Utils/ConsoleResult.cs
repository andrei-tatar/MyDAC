using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Julia.Utils
{
    public class ConsoleResult
    {
        public string Output { get; internal set; }
        public string Error { get; internal set; }
        public Exception Exception { get; internal set; }

        public ConsoleResult()
        {
            Output = "";
            Error = "";
        }

        public override string ToString()
        {
            var result = "";

            if (!string.IsNullOrWhiteSpace(Output))
                result += "Output:" + Environment.NewLine + Output + Environment.NewLine;
            if (!string.IsNullOrWhiteSpace(Error))
                result += "Error:" + Environment.NewLine + Error + Environment.NewLine;
            if (Exception != null)
                result += "Exception:" + Environment.NewLine + Exception;

            return result;
        }
    }
}
