using System.Collections.Generic;
using guideXOS.GUI;
using guideXOS.Kernel.Drivers;
using guideXOS.DefaultApps;

namespace guideXOS.OS {
    internal enum FirewallMode { Normal, BlockAll, Disabled, Autolearn }

    internal static class Firewall {
        private static List<string> _exceptions = new List<string>(64);
        private static List<string> _pendingAlerts = new List<string>(16); // program|action
        public static FirewallMode Mode = FirewallMode.Normal;
        public static FirewallWindow Window;

        private static bool ListHas(List<string> list, string value) { for (int i = 0; i < list.Count; i++) if (list[i] == value) return true; return false; }

        public static void Initialize() { if (Window == null) { Window = new FirewallWindow(220, 140); Window.Visible = false; } }
        public static string[] Exceptions { get { var arr = new string[_exceptions.Count]; for (int i = 0; i < _exceptions.Count; i++) arr[i] = _exceptions[i]; return arr; } }
        public static void AddException(string name) { if (!ListHas(_exceptions, name)) _exceptions.Add(name); }
        public static bool IsException(string name) { return ListHas(_exceptions, name); }
        public static void ClearAlerts() { _pendingAlerts.Clear(); }
        public static bool Check(string program, string action) {
            if (Mode == FirewallMode.Disabled) return true;
            if (Mode == FirewallMode.BlockAll) { QueueAlert(program, action); return false; }
            if (IsException(program)) return true;
            if (Mode == FirewallMode.Autolearn) { AddException(program); return true; }
            QueueAlert(program, action); return false;
        }
        private static void QueueAlert(string program, string action) { string key = program + "|" + action; if (!ListHas(_pendingAlerts, key)) { _pendingAlerts.Add(key); ShowAlert(program, action); } }
        private static void ShowAlert(string program, string action) { var alert = new FirewallAlert(program, action); WindowManager.MoveToEnd(alert); alert.Visible = true; }
        public static void RemoveAlert(string program, string action) { string key = program + "|" + action; for (int i = 0; i < _pendingAlerts.Count; i++) { if (_pendingAlerts[i] == key) { _pendingAlerts.RemoveAt(i); break; } } }
    }
}
