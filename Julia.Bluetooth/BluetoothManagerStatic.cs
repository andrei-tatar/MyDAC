using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Julia.Utils;

namespace Julia.Bluetooth
{
    public partial class BluetoothManager
    {
        public static string[] GetBluetoothDevices()
        {
            var allLines = ConsoleUtils.Execute("hciconfig").Output.GetLines();

            return (from line in allLines
                    select line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries)
                        into parts
                        where parts.Length >= 1 && parts[0].EndsWith(":")
                        select parts[0].TrimEnd(':')).ToArray();
        }

        private const string ErrorCantChangeScanType = "Could not change scan type";
    }
}
