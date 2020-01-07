using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECIKParentUnlocker
{
    public static class HarmonyUtils
    {
        [Conditional("DEBUG")]
        public static void DebugLog(string message)
        {
            HarmonyLib.FileLog.Log(message);
        }
    }
}
