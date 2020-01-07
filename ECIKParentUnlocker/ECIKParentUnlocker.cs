using BepInEx;
using HarmonyLib;
using System;
using System.Linq;
using System.Reflection;

namespace ECIKParentUnlocker
{
    [BepInPlugin(GUID, PluginName, PluginVersion)]
    public class ECIKParentUnlocker : BaseUnityPlugin
    {
        public const string GUID = "com.getraid.ec.ikparentunlocker";
        public const string PluginName = "IK Parent Unlocker";
        public const string PluginVersion = "2.0.0";

        private void Awake()
        {
            var harmony = new Harmony(GUID);
            try
            {
                harmony.PatchAll(Assembly.GetExecutingAssembly());
            }
            catch (Exception e)
            {
                FileLog.Log($"Failed to apply patch:\n{e.ToString()}");
                return;
            }
        }
    }
}
