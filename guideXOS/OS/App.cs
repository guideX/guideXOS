using guideXOS.DefaultApps;
using guideXOS.DockableWidgets;
using guideXOS.GUI;
using System;
using System.Collections.Generic;
using System.Drawing;
namespace guideXOS.OS {
    /// <summary>
    /// Kind of app entry resolved by the launch resolver.
    /// </summary>
    public enum AppKind {
        BuiltInApp,
        LegacyAlias,
        GxmApp,
        FileAssociation,
        ShellObject,
        Unknown
    }

    /// <summary>
    /// App descriptor used by the compatibility resolver.
    /// </summary>
    public class AppDescriptor {
        public string AppId { get; set; }
        public string DisplayName { get; set; }
        public string DispatchName { get; set; }
        public AppKind Kind { get; set; }
        public string[] LegacyAliases { get; set; }
        public Image Icon { get; set; }
    }

    /// <summary>
    /// Result of resolving a launch request.
    /// </summary>
    public class AppLaunchResolution {
        public bool Success { get; set; }
        public string Input { get; set; }
        public string AppId { get; set; }
        public string DisplayName { get; set; }
        public string DispatchName { get; set; }
        public AppKind ResolvedKind { get; set; }
        public string MatchedAlias { get; set; }
        public string FailureReason { get; set; }
    }

    /// <summary>
    /// Kind of shell object resolved by the compatibility registry.
    /// </summary>
    public enum ShellObjectKind {
        BuiltInApp,
        FileSystemLocation,
        DeviceVolume,
        SystemAction,
        Unknown
    }

    /// <summary>
    /// Shell object descriptor used by the compatibility registry.
    /// </summary>
    public class ShellObjectDescriptor {
        public string ShellId { get; set; }
        public string DisplayName { get; set; }
        public ShellObjectKind Kind { get; set; }
        public string AppId { get; set; }
        public string DispatchName { get; set; }
        public string Path { get; set; }
        public string DeviceName { get; set; }
        public string ActionName { get; set; }
        public string[] LegacyAliases { get; set; }
    }

    /// <summary>
    /// Result of resolving a shell object.
    /// </summary>
    public class ShellObjectResolution {
        public bool Success { get; set; }
        public string Input { get; set; }
        public string ShellId { get; set; }
        public string DisplayName { get; set; }
        public ShellObjectKind Kind { get; set; }
        public string AppId { get; set; }
        public string DispatchName { get; set; }
        public string Path { get; set; }
        public string DeviceName { get; set; }
        public string ActionName { get; set; }
        public string MatchedAlias { get; set; }
        public string FailureReason { get; set; }
    }

    /// <summary>
    /// File association descriptor for Legacy compatibility.
    /// </summary>
    public class FileAssociationDescriptor {
        public string Extension { get; set; }
        public string AppId { get; set; }
        public AppKind Kind { get; set; }
    }

    /// <summary>
    /// Result of resolving a file association.
    /// </summary>
    public class FileAssociationResolution {
        public bool Success { get; set; }
        public string Extension { get; set; }
        public string AppId { get; set; }
        public string DisplayName { get; set; }
        public string DispatchName { get; set; }
        public AppKind Kind { get; set; }
        public string FailureReason { get; set; }
    }

    /// <summary>
    /// File association registry for the app-model compatibility layer.
    /// </summary>
    public static class FileAssociationRegistry {
        private static List<FileAssociationDescriptor> _descriptors;

        public static void InitializeDefaultAssociations() {
            if (_descriptors != null) return;
            _descriptors = new List<FileAssociationDescriptor>();
            Register(".txt", "gxos.builtin.notepad", AppKind.FileAssociation);
            Register(".png", "gxos.builtin.imageviewer", AppKind.FileAssociation);
            Register(".bmp", "gxos.builtin.imageviewer", AppKind.FileAssociation);
            Register(".wav", "gxos.builtin.wavplayer", AppKind.FileAssociation);
            Register(".gxm", null, AppKind.GxmApp);
            Register(".mue", null, AppKind.GxmApp);
        }

        private static void Register(string extension, string appId, AppKind kind) {
            _descriptors.Add(new FileAssociationDescriptor {
                Extension = NormalizeExtension(extension),
                AppId = appId,
                Kind = kind
            });
        }

        public static FileAssociationResolution Resolve(string extension) {
            InitializeDefaultAssociations();
            string normalized = NormalizeExtension(extension);
            var result = new FileAssociationResolution {
                Extension = normalized,
                Kind = AppKind.Unknown,
                Success = false
            };

            if (string.IsNullOrEmpty(normalized)) {
                result.FailureReason = "Empty extension";
                return result;
            }

            for (int i = 0; i < _descriptors.Count; i++) {
                var d = _descriptors[i];
                if (d.Extension == normalized) {
                    if (d.Kind == AppKind.FileAssociation && !string.IsNullOrEmpty(d.AppId)) {
                        if (!AppLaunchResolver.TryGetDescriptorByAppId(d.AppId, out AppDescriptor appDescriptor)) {
                            result.FailureReason = "No matching app descriptor for association";
                            return result;
                        }

                        result.Success = true;
                        result.Extension = d.Extension;
                        result.AppId = appDescriptor.AppId;
                        result.DisplayName = appDescriptor.DisplayName;
                        result.DispatchName = appDescriptor.DispatchName;
                        result.Kind = d.Kind;
                        return result;
                    }

                    result.Success = true;
                    result.Extension = d.Extension;
                    result.AppId = d.AppId;
                    result.DisplayName = null;
                    result.DispatchName = null;
                    result.Kind = d.Kind;
                    return result;
                }
            }

            result.FailureReason = "No matching file association";
            return result;
        }

        public static FileAssociationResolution ResolvePath(string pathOrName) {
            if (string.IsNullOrEmpty(pathOrName)) {
                return new FileAssociationResolution {
                    Success = false,
                    Kind = AppKind.Unknown,
                    FailureReason = "Empty path"
                };
            }

            int dot = pathOrName.LastIndexOf('.');
            if (dot < 0 || dot >= pathOrName.Length) {
                return new FileAssociationResolution {
                    Success = false,
                    Kind = AppKind.Unknown,
                    FailureReason = "No file extension"
                };
            }

            return Resolve(pathOrName.Substring(dot));
        }

        public static bool RunSelfTest() {
            InitializeDefaultAssociations();

            int passed = 0;
            int failed = 0;
            string failure = null;

            if (ValidateAssociationDerived(".txt", "gxos.builtin.notepad", AppKind.FileAssociation, ref failure)) passed++; else failed++;
            if (ValidateAssociationDerived(".TXT", "gxos.builtin.notepad", AppKind.FileAssociation, ref failure)) passed++; else failed++;
            if (ValidateAssociationDerived(".png", "gxos.builtin.imageviewer", AppKind.FileAssociation, ref failure)) passed++; else failed++;
            if (ValidateAssociationDerived(".bmp", "gxos.builtin.imageviewer", AppKind.FileAssociation, ref failure)) passed++; else failed++;
            if (ValidateAssociationDerived(".wav", "gxos.builtin.wavplayer", AppKind.FileAssociation, ref failure)) passed++; else failed++;
            if (ValidateAssociation(".gxm", null, null, null, AppKind.GxmApp, ref failure)) passed++; else failed++;
            if (ValidateAssociation(".mue", null, null, null, AppKind.GxmApp, ref failure)) passed++; else failed++;
            if (ValidateAssociationFailure(".unknown", ref failure)) passed++; else failed++;

            TryEmitSelfTestSummary(passed, failed, failure);
            return failed == 0;
        }

        private static bool ValidateAssociation(string extension, string expectedAppId, string expectedDisplay, string expectedDispatch, AppKind expectedKind, ref string failureMessage) {
            var r = Resolve(extension);
            if (!r.Success) { if (failureMessage == null) failureMessage = "extension=" + extension + " reason=" + (r.FailureReason ?? "<none>"); return false; }
            if (r.Kind != expectedKind) { if (failureMessage == null) failureMessage = "extension=" + extension + " kind=" + r.Kind + " expected=" + expectedKind; return false; }
            if (expectedAppId != null && r.AppId != expectedAppId) { if (failureMessage == null) failureMessage = "extension=" + extension + " appId=" + (r.AppId ?? "<null>") + " expected=" + expectedAppId; return false; }
            if (expectedDisplay != null && r.DisplayName != expectedDisplay) { if (failureMessage == null) failureMessage = "extension=" + extension + " displayName=" + (r.DisplayName ?? "<null>") + " expected=" + expectedDisplay; return false; }
            if (expectedDispatch != null && r.DispatchName != expectedDispatch) { if (failureMessage == null) failureMessage = "extension=" + extension + " dispatchName=" + (r.DispatchName ?? "<null>") + " expected=" + expectedDispatch; return false; }
            return true;
        }

        private static bool ValidateAssociationDerived(string extension, string expectedAppId, AppKind expectedKind, ref string failureMessage) {
            var r = Resolve(extension);
            if (!r.Success) { if (failureMessage == null) failureMessage = "extension=" + extension + " reason=" + (r.FailureReason ?? "<none>"); return false; }
            if (r.Kind != expectedKind) { if (failureMessage == null) failureMessage = "extension=" + extension + " kind=" + r.Kind + " expected=" + expectedKind; return false; }
            if (r.AppId != expectedAppId) { if (failureMessage == null) failureMessage = "extension=" + extension + " appId=" + (r.AppId ?? "<null>") + " expected=" + expectedAppId; return false; }
            if (!AppLaunchResolver.TryGetDescriptorByAppId(expectedAppId, out AppDescriptor descriptor)) { if (failureMessage == null) failureMessage = "extension=" + extension + " missing app descriptor"; return false; }
            if (r.DisplayName != descriptor.DisplayName) { if (failureMessage == null) failureMessage = "extension=" + extension + " displayName=" + (r.DisplayName ?? "<null>") + " expected=" + descriptor.DisplayName; return false; }
            if (r.DispatchName != descriptor.DispatchName) { if (failureMessage == null) failureMessage = "extension=" + extension + " dispatchName=" + (r.DispatchName ?? "<null>") + " expected=" + descriptor.DispatchName; return false; }
            return true;
        }

        private static bool ValidateAssociationFailure(string extension, ref string failureMessage) {
            var r = Resolve(extension);
            if (r.Success) { if (failureMessage == null) failureMessage = "extension=" + extension + " unexpectedly succeeded"; return false; }
            if (string.IsNullOrEmpty(r.FailureReason)) { if (failureMessage == null) failureMessage = "extension=" + extension + " missing failure reason"; return false; }
            return true;
        }

        private static void TryEmitSelfTestSummary(int passed, int failed, string failure) {
            if (AppLaunchResolver.EnableResolutionDiagnostics) {
                try {
                    NotificationManager.Add(new Notify("FileAssocSmoke: passed=" + passed + " failed=" + failed + (failure != null ? " " + failure : "")));
                } catch { }
            }
        }

        private static string NormalizeExtension(string extension) {
            if (string.IsNullOrEmpty(extension)) return extension;
            string value = extension;
            if (value[0] != '.') value = "." + value;
            char[] chars = new char[value.Length];
            for (int i = 0; i < value.Length; i++) {
                char c = value[i];
                if (c >= 'A' && c <= 'Z') c = (char)(c + 32);
                chars[i] = c;
            }
            return new string(chars);
        }
    }

    /// <summary>
    /// Shell object registry for Legacy compatibility.
    /// </summary>
    public static class ShellObjectRegistry {
        private static List<ShellObjectDescriptor> _descriptors;

        public static void InitializeDefaultShellObjects() {
            if (_descriptors != null) return;
            _descriptors = new List<ShellObjectDescriptor>();

            AppLaunchResolver.InitializeDefaultDescriptors();
            if (AppLaunchResolver.TryGetDescriptorByAppId("gxos.builtin.files", out AppDescriptor filesDescriptor)) {
                Register(new ShellObjectDescriptor {
                    ShellId = "gxos.shell.computerfiles",
                    DisplayName = filesDescriptor.DisplayName,
                    Kind = ShellObjectKind.BuiltInApp,
                    AppId = filesDescriptor.AppId,
                    DispatchName = filesDescriptor.DispatchName,
                    LegacyAliases = new string[] { "Computer Files" }
                });
            }

            Register(new ShellObjectDescriptor {
                ShellId = "gxos.shell.root",
                DisplayName = "Root",
                Kind = ShellObjectKind.FileSystemLocation,
                Path = "",
                LegacyAliases = new string[] { "Root" }
            });

            Register(new ShellObjectDescriptor {
                ShellId = "gxos.shell.usbdrive",
                DisplayName = "USB Drive",
                Kind = ShellObjectKind.DeviceVolume,
                DeviceName = "USB Drive",
                LegacyAliases = new string[] { "USB Drive" }
            });

            Register(new ShellObjectDescriptor {
                ShellId = "gxos.shell.installtoharddrive",
                DisplayName = "Install to Hard Drive",
                Kind = ShellObjectKind.SystemAction,
                ActionName = "HDInstaller",
                DispatchName = "HDInstaller",
                LegacyAliases = new string[] { "Install to Hard Drive" }
            });
        }

        private static void Register(ShellObjectDescriptor descriptor) {
            if (descriptor == null) return;
            _descriptors.Add(descriptor);
        }

        public static ShellObjectResolution Resolve(string input) {
            InitializeDefaultShellObjects();
            var result = new ShellObjectResolution {
                Input = input,
                Kind = ShellObjectKind.Unknown,
                Success = false
            };

            if (string.IsNullOrEmpty(input)) {
                result.FailureReason = "Empty input";
                return result;
            }

            for (int i = 0; i < _descriptors.Count; i++) {
                var d = _descriptors[i];
                if (MatchesShellAlias(d, input, out string matchedAlias)) {
                    result.Success = true;
                    result.ShellId = d.ShellId;
                    result.DisplayName = d.DisplayName;
                    result.Kind = d.Kind;
                    result.AppId = d.AppId;
                    result.DispatchName = d.DispatchName;
                    result.Path = d.Path;
                    result.DeviceName = d.Kind == ShellObjectKind.DeviceVolume ? input : d.DeviceName;
                    result.ActionName = d.ActionName;
                    result.MatchedAlias = matchedAlias;
                    return result;
                }
            }

            result.FailureReason = "No matching shell object";
            return result;
        }

        private static bool MatchesShellAlias(ShellObjectDescriptor descriptor, string input, out string matchedAlias) {
            matchedAlias = null;
            if (descriptor == null || descriptor.LegacyAliases == null) return false;
            for (int i = 0; i < descriptor.LegacyAliases.Length; i++) {
                string alias = descriptor.LegacyAliases[i];
                if (alias == null) continue;
                if (alias == input) {
                    matchedAlias = alias;
                    return true;
                }
                if (descriptor.Kind == ShellObjectKind.DeviceVolume && StartsWith(input, alias)) {
                    matchedAlias = alias;
                    return true;
                }
            }
            return false;
        }

        private static bool StartsWith(string value, string prefix) {
            if (value == null || prefix == null || value.Length < prefix.Length) return false;
            for (int i = 0; i < prefix.Length; i++) {
                if (value[i] != prefix[i]) return false;
            }
            return true;
        }

        public static bool RunSelfTest() {
            InitializeDefaultShellObjects();

            int passed = 0;
            int failed = 0;
            string failure = null;

            Check("Computer Files", ShellObjectKind.BuiltInApp, "gxos.builtin.files", null, null, "Computer Files", ref passed, ref failed, ref failure);
            Check("Root", ShellObjectKind.FileSystemLocation, null, "", null, "Root", ref passed, ref failed, ref failure);
            Check("Install to Hard Drive", ShellObjectKind.SystemAction, null, null, "HDInstaller", "Install to Hard Drive", ref passed, ref failed, ref failure);
            CheckFailure("Definitely Not A Real Shell Object", ref passed, ref failed, ref failure);

            TryEmitSelfTestSummary(passed, failed, failure);
            return failed == 0;

            void Check(string input, ShellObjectKind expectedKind, string expectedAppId, string expectedPath, string expectedAction, string expectedDisplay, ref int passedCount, ref int failedCount, ref string failureMessage) {
                var r = Resolve(input);
                if (!r.Success) { if (failureMessage == null) failureMessage = "input=" + input + " reason=" + (r.FailureReason ?? "<none>"); failedCount++; return; }
                if (r.Kind != expectedKind) { if (failureMessage == null) failureMessage = "input=" + input + " kind=" + r.Kind + " expected=" + expectedKind; failedCount++; return; }
                if (expectedAppId != null && r.AppId != expectedAppId) { if (failureMessage == null) failureMessage = "input=" + input + " appId=" + (r.AppId ?? "<null>") + " expected=" + expectedAppId; failedCount++; return; }
                if (expectedPath != null && r.Path != expectedPath) { if (failureMessage == null) failureMessage = "input=" + input + " path=" + (r.Path ?? "<null>") + " expected=" + expectedPath; failedCount++; return; }
                if (expectedAction != null && r.ActionName != expectedAction) { if (failureMessage == null) failureMessage = "input=" + input + " actionName=" + (r.ActionName ?? "<null>") + " expected=" + expectedAction; failedCount++; return; }
                if (string.IsNullOrEmpty(r.DisplayName)) { if (failureMessage == null) failureMessage = "input=" + input + " displayName empty"; failedCount++; return; }
                passedCount++;
            }

            void CheckFailure(string input, ref int passedCount, ref int failedCount, ref string failureMessage) {
                var r = Resolve(input);
                if (r.Success) { if (failureMessage == null) failureMessage = "input=" + input + " unexpectedly succeeded"; failedCount++; return; }
                if (string.IsNullOrEmpty(r.FailureReason)) { if (failureMessage == null) failureMessage = "input=" + input + " missing failure reason"; failedCount++; return; }
                passedCount++;
            }
        }

        private static void TryEmitSelfTestSummary(int passed, int failed, string failure) {
            if (AppLaunchResolver.EnableResolutionDiagnostics) {
                try {
                    NotificationManager.Add(new Notify("ShellObjectSmoke: passed=" + passed + " failed=" + failed + (failure != null ? " " + failure : "")));
                } catch { }
            }
        }
    }

    /// <summary>
    /// Compatibility resolver for app IDs and legacy names.
    /// </summary>
    public static class AppLaunchResolver {
        private static List<AppDescriptor> _descriptors;
        public static bool EnableResolutionDiagnostics;

        public static void InitializeDefaultDescriptors() {
            if (_descriptors != null) return;
            _descriptors = new List<AppDescriptor>();
            RegisterBuiltIn("gxos.builtin.calculator", "Calculator", "Calculator", Icons.CalculatorIcon(32), "Calculator");
            RegisterBuiltIn("gxos.builtin.files", "Computer Files", "Computer Files", Icons.FolderIcon(32), "Computer Files", "File Explorer");
            RegisterBuiltIn("gxos.builtin.console", "Console", "Console", Icons.EditIcon(32), "Console");
            RegisterBuiltIn("gxos.builtin.devices", "Devices", "Devices", Icons.ConfigureIcon(32), "Devices");
            RegisterBuiltIn("gxos.builtin.diskmanager", "Disk Manager", "Disk Manager", Icons.DocumentIcon(32), "Disk Manager");
            RegisterBuiltIn("gxos.builtin.displayoptions", "Display Options", "Display Options", Icons.ConfigureIcon(32), "Display Options");
            RegisterBuiltIn("gxos.builtin.firewall", "Firewall", "Firewall", Icons.ConfigureIcon(32), "Firewall");
            RegisterBuiltIn("gxos.builtin.notepad", "Notepad", "Notepad", Icons.NotepadIcon(32), "Notepad");
            RegisterBuiltIn("gxos.builtin.paint", "Paint", "Paint", Icons.ImageIcon(32), "Paint");
            RegisterBuiltIn("gxos.builtin.taskmanager", "Task Manager", "Task Manager", Icons.ApplicationsIcon(32), "Task Manager");
            RegisterBuiltIn("gxos.builtin.imageviewer", "Image Viewer", "Image Viewer", Icons.ImageIcon(32), "Image Viewer");
            RegisterBuiltIn("gxos.builtin.wavplayer", "WAV Player", "WAV Player", Icons.AudioIcon(32), "WAV Player");
        }

        private static void RegisterBuiltIn(string appId, string displayName, string dispatchName, Image icon, params string[] legacyAliases) {
            _descriptors.Add(new AppDescriptor {
                AppId = appId,
                DisplayName = displayName,
                DispatchName = dispatchName,
                Kind = AppKind.BuiltInApp,
                LegacyAliases = legacyAliases,
                Icon = icon
            });
        }

        public static AppLaunchResolution Resolve(string input) {
            InitializeDefaultDescriptors();
            var result = new AppLaunchResolution {
                Input = input,
                ResolvedKind = AppKind.Unknown,
                Success = false
            };

            if (string.IsNullOrEmpty(input)) {
                result.FailureReason = "Empty input";
                return result;
            }

            if (TryGetDescriptorByAppId(input, out AppDescriptor directDescriptor)) {
                result.Success = true;
                result.AppId = directDescriptor.AppId;
                result.DisplayName = directDescriptor.DisplayName;
                result.DispatchName = directDescriptor.DispatchName;
                result.ResolvedKind = directDescriptor.Kind;
                return result;
            }

            for (int i = 0; i < _descriptors.Count; i++) {
                var d = _descriptors[i];
                if (d.LegacyAliases != null) {
                    for (int a = 0; a < d.LegacyAliases.Length; a++) {
                        if (d.LegacyAliases[a] == input) {
                            result.Success = true;
                            result.AppId = d.AppId;
                            result.DisplayName = d.DisplayName;
                            result.DispatchName = d.DispatchName;
                            result.ResolvedKind = AppKind.LegacyAlias;
                            result.MatchedAlias = d.LegacyAliases[a];
                            return result;
                        }
                    }
                }

                if (d.DisplayName == input || d.DispatchName == input) {
                    result.Success = true;
                    result.AppId = d.AppId;
                    result.DisplayName = d.DisplayName;
                    result.DispatchName = d.DispatchName;
                    result.ResolvedKind = AppKind.LegacyAlias;
                    result.MatchedAlias = input;
                    return result;
                }
            }

            result.FailureReason = "No matching app descriptor";
            return result;
        }

        public static bool TryGetDescriptorByAppId(string appId, out AppDescriptor descriptor) {
            InitializeDefaultDescriptors();
            descriptor = null;
            if (string.IsNullOrEmpty(appId)) return false;

            for (int i = 0; i < _descriptors.Count; i++) {
                var d = _descriptors[i];
                if (StringEqualsIgnoreCase(d.AppId, appId)) {
                    descriptor = d;
                    return true;
                }
            }

            return false;
        }

        private static bool StringEqualsIgnoreCase(string a, string b) {
            if (a == null || b == null || a.Length != b.Length) return false;
            for (int i = 0; i < a.Length; i++) {
                char ca = a[i];
                char cb = b[i];
                if (ca >= 'A' && ca <= 'Z') ca = (char)(ca + 32);
                if (cb >= 'A' && cb <= 'Z') cb = (char)(cb + 32);
                if (ca != cb) return false;
            }
            return true;
        }

        public static bool RunSelfTest() {
            InitializeDefaultDescriptors();

            int passed = 0;
            int failed = 0;
            string failure = null;

            Check("gxos.builtin.notepad", "gxos.builtin.notepad", "Notepad", "Notepad", AppKind.BuiltInApp, false, ref passed, ref failed, ref failure);
            Check("gxos.builtin.calculator", "gxos.builtin.calculator", "Calculator", "Calculator", AppKind.BuiltInApp, false, ref passed, ref failed, ref failure);
            Check("gxos.builtin.files", "gxos.builtin.files", "Computer Files", "Computer Files", AppKind.BuiltInApp, false, ref passed, ref failed, ref failure);
            Check("gxos.builtin.console", "gxos.builtin.console", "Console", "Console", AppKind.BuiltInApp, false, ref passed, ref failed, ref failure);
            Check("gxos.builtin.taskmanager", "gxos.builtin.taskmanager", "Task Manager", "Task Manager", AppKind.BuiltInApp, false, ref passed, ref failed, ref failure);
            Check("gxos.builtin.diskmanager", "gxos.builtin.diskmanager", "Disk Manager", "Disk Manager", AppKind.BuiltInApp, false, ref passed, ref failed, ref failure);
            Check("gxos.builtin.imageviewer", "gxos.builtin.imageviewer", "Image Viewer", "Image Viewer", AppKind.BuiltInApp, false, ref passed, ref failed, ref failure);
            Check("gxos.builtin.wavplayer", "gxos.builtin.wavplayer", "WAV Player", "WAV Player", AppKind.BuiltInApp, false, ref passed, ref failed, ref failure);

            Check("Notepad", "gxos.builtin.notepad", "Notepad", "Notepad", AppKind.LegacyAlias, true, ref passed, ref failed, ref failure);
            Check("Calculator", "gxos.builtin.calculator", "Calculator", "Calculator", AppKind.LegacyAlias, true, ref passed, ref failed, ref failure);
            Check("Computer Files", "gxos.builtin.files", "Computer Files", "Computer Files", AppKind.LegacyAlias, true, ref passed, ref failed, ref failure);
            Check("Console", "gxos.builtin.console", "Console", "Console", AppKind.LegacyAlias, true, ref passed, ref failed, ref failure);
            Check("Task Manager", "gxos.builtin.taskmanager", "Task Manager", "Task Manager", AppKind.LegacyAlias, true, ref passed, ref failed, ref failure);
            Check("Disk Manager", "gxos.builtin.diskmanager", "Disk Manager", "Disk Manager", AppKind.LegacyAlias, true, ref passed, ref failed, ref failure);
            Check("Image Viewer", "gxos.builtin.imageviewer", "Image Viewer", "Image Viewer", AppKind.LegacyAlias, true, ref passed, ref failed, ref failure);
            Check("WAV Player", "gxos.builtin.wavplayer", "WAV Player", "WAV Player", AppKind.LegacyAlias, true, ref passed, ref failed, ref failure);

            CheckFailure("Definitely Not A Real App", ref passed, ref failed, ref failure);
            CheckFailure("gxos.builtin.notreal", ref passed, ref failed, ref failure);

            if (failure != null) {
                TryEmitSelfTestSummary(passed, failed, failure);
                return false;
            }

            TryEmitSelfTestSummary(passed, failed, null);
            return true;

            void Check(string input, string expectedId, string expectedDisplay, string expectedDispatch, AppKind expectedKind, bool expectAlias, ref int passedCount, ref int failedCount, ref string failureMessage) {
                var r = Resolve(input);
                if (!r.Success) { if (failureMessage == null) failureMessage = "input=" + input + " reason=" + (r.FailureReason ?? "<none>"); failedCount++; return; }
                if (r.AppId != expectedId) { if (failureMessage == null) failureMessage = "input=" + input + " appId=" + (r.AppId ?? "<null>") + " expected=" + expectedId; failedCount++; return; }
                if (r.DispatchName != expectedDispatch) { if (failureMessage == null) failureMessage = "input=" + input + " dispatchName=" + (r.DispatchName ?? "<null>") + " expected=" + expectedDispatch; failedCount++; return; }
                if (string.IsNullOrEmpty(r.DisplayName)) { if (failureMessage == null) failureMessage = "input=" + input + " displayName empty"; failedCount++; return; }
                if (r.ResolvedKind != expectedKind && !(expectedKind == AppKind.LegacyAlias && r.ResolvedKind == AppKind.BuiltInApp)) { if (failureMessage == null) failureMessage = "input=" + input + " kind=" + r.ResolvedKind + " expected=" + expectedKind; failedCount++; return; }
                if (expectAlias && string.IsNullOrEmpty(r.MatchedAlias)) { if (failureMessage == null) failureMessage = "input=" + input + " alias missing"; failedCount++; return; }
                passedCount++;
            }

            void CheckFailure(string input, ref int passedCount, ref int failedCount, ref string failureMessage) {
                var r = Resolve(input);
                if (r.Success) { if (failureMessage == null) failureMessage = "input=" + input + " unexpectedly succeeded"; failedCount++; return; }
                if (string.IsNullOrEmpty(r.FailureReason)) { if (failureMessage == null) failureMessage = "input=" + input + " missing failure reason"; failedCount++; return; }
                passedCount++;
            }
        }

        private static void TryEmitSelfTestSummary(int passed, int failed, string failure) {
            if (EnableResolutionDiagnostics) {
                try {
                    NotificationManager.Add(new Notify("AppModelSmoke: passed=" + passed + " failed=" + failed + (failure != null ? " " + failure : "")));
                } catch { }
            }
        }
    }

    /// <summary>
    /// App
    /// </summary>
    public class App {
        #region "private variables"
        /// <summary>
        /// Name
        /// </summary>
        private string _name { get; set; }
        /// <summary>
        /// Icon
        /// </summary>
        private Image _icon { get; set; }
        /// <summary>
        /// App Object
        /// </summary>
        private Object _appObject { get; set; }
        #endregion
        #region "public variables"
        /// <summary>
        /// App
        /// </summary>
        /// <param name="name"></param>
        public App(string name, Image icon) { _name = name; _icon = icon; }
        /// <summary>
        /// Name
        /// </summary>
        public string Name {
            get {
                return _name;
            }
        }
        /// <summary>
        /// Icon
        /// </summary>
        public Image Icon {
            get {
                return _icon;
            }
        }
        /// <summary>
        /// App Object
        /// </summary>
        public Object AppObject {
            get {
                return _appObject;
            }
            set {
                _appObject = value;
            }
        }
        #endregion
    }
    /// <summary>
    /// App Collection
    /// </summary>
    public class AppCollection {
        #region "private variables"
        /// <summary>
        /// Apps
        /// </summary>
        private List<App> _apps;
        #endregion
        #region "public variables"
        /// <summary>
        /// App Collection
        /// </summary>
        public AppCollection() {
            _apps = new List<App>();
            LoadDefaultApps();
        }
        /// <summary>
        /// Load Default Apps
        /// </summary>
        private void LoadDefaultApps() {
            AppLaunchResolver.InitializeDefaultDescriptors();
            _apps.Add(new App("Calculator", Icons.CalculatorIcon(32)));
            _apps.Add(new App("Computer Files", Icons.FolderIcon(32)));
            _apps.Add(new App("Console", Icons.EditIcon(32)));
            _apps.Add(new App("Devices", Icons.ConfigureIcon(32)));
            _apps.Add(new App("Disk Manager", Icons.DocumentIcon(32)));
            _apps.Add(new App("Display Options", Icons.ConfigureIcon(32)));
            _apps.Add(new App("Firewall", Icons.ConfigureIcon(32)));
            _apps.Add(new App("Notepad", Icons.NotepadIcon(32)));
            _apps.Add(new App("Paint", Icons.ImageIcon(32)));
            _apps.Add(new App("Task Manager", Icons.ApplicationsIcon(32)));
            _apps.Add(new App("Image Viewer", Icons.ImageIcon(32)));
            _apps.Add(new App("WAV Player", Icons.AudioIcon(32)));
            //_apps.Add(new App("Clock", Icons.CalendarIcon(32)));
            //_apps.Add(new App("Monitor", Icons.DocumentIcon(32)));
            //_apps.Add(new App("Lock", Icons.LockIcon(32)));
            //_apps.Add(new App("nexIRC", Icons.ChatIcon(32)));
            //_apps.Add(new App("IRCNetworks", Icons.NetworkIcon(32)));
            //_apps.Add(new App("GUISamples", Icons.ApplicationsIcon(32)));
            //_apps.Add(new App("OnScreenKeyboard", Icons.EditIcon(32)));
            //_apps.Add(new App("WebBrowser", Icons.NetworkIcon(32)));
            //_apps.Add(new App("Welcome", Icons.ApplicationsIcon(32)));
            // GXM apps from filesystem
            //_apps.Add(new App("Hello Demo", Icons.ApplicationsIcon(32)));
            //_apps.Add(new App("Minimal Demo", Icons.ApplicationsIcon(32)));
        }
        /// <summary>
        /// Load
        /// </summary>
        /// <param name="name"></param>
        public bool Load(string name) {
            var b = false;
            guideXOS.GUI.NotificationManager.Add(new Notify("Loading App: " + name));
            var resolution = AppLaunchResolver.Resolve(name);
            string dispatchName = resolution.Success ? resolution.DispatchName : name;
            if (AppLaunchResolver.EnableResolutionDiagnostics) {
                try {
                    guideXOS.GUI.NotificationManager.Add(new Notify("input=" + name + " resolvedAppId=" + (resolution.AppId ?? "") + " resolvedKind=" + resolution.ResolvedKind + " dispatchName=" + (dispatchName ?? "") + " success=" + resolution.Success));
                } catch { }
            }
            for (int i = 0; i < _apps.Count; i++) {
                if (_apps[i].Name == dispatchName) {
                    switch (dispatchName) {
                        case "Devices": _apps[i].AppObject = new Devices(400, 300); b = true; break;
                        case "Lock": Lockscreen.Run(); b = true; break;
                        case "Calculator": _apps[i].AppObject = new Calculator(300, 500); b = true; break;
                        case "Monitor": _apps[i].AppObject = new Monitor(); b = true; break;
                        case "Clock": _apps[i].AppObject = new Clock(650, 500); b = true; break;
                        case "Paint": _apps[i].AppObject = new Paint(500, 200); b = true; break;
                        case "Notepad": _apps[i].AppObject = new Notepad(360, 200); b = true; break;
                        case "Console": 
                            if (Program.FConsole == null) Program.FConsole = new FConsole(160, 120); 
                            _apps[i].AppObject = Program.FConsole; b = true; break;
                        case "Task Manager": _apps[i].AppObject = new TaskManager(500, 500); b = true; break;
                        case "nexIRC": _apps[i].AppObject = new nexIRC(260, 220); b = true; break;
                        case "IRC Networks": _apps[i].AppObject = new IRCNetworks(300, 240); b = true; break;
                        case "GUI Samples": _apps[i].AppObject = new GUISamples(220, 260); b = true; break;
                        case "Computer Files": _apps[i].AppObject = new ComputerFiles(300, 200); b = true; break;
                        case "Disk Manager": _apps[i].AppObject = new DiskManager(400, 300); b = true; break;
                        case "Display Options": _apps[i].AppObject = new DisplayOptions(200, 150, 800, 600); b = true; break;
                        case "Firewall": _apps[i].AppObject = new FirewallWindow(300, 200); b = true; break;
                        case "Image Viewer": 
                            if (Desktop.imageViewer != null) {
                                Desktop.imageViewer.Visible = true;
                                WindowManager.MoveToEnd(Desktop.imageViewer);
                                _apps[i].AppObject = Desktop.imageViewer;
                                b = true;
                            }
                            break;
                        case "On Screen Keyboard": _apps[i].AppObject = new OnScreenKeyboard(300, 100); b = true; break;
                        case "WAV Player": 
                            if (Desktop.wavplayer != null) {
                                Desktop.wavplayer.Visible = true;
                                WindowManager.MoveToEnd(Desktop.wavplayer);
                                _apps[i].AppObject = Desktop.wavplayer;
                                b = true;
                            }
                            break;
                        case "Web Browser": _apps[i].AppObject = new WebBrowser(200, 150); b = true; break;
                        case "Welcome": _apps[i].AppObject = new Welcome(300, 200); b = true; break;
                        // GXM apps
                        case "Hello Demo":
                            b = LaunchGXMFromFile("Programs/hello.gxm", _apps[i].Icon);
                            break;
                        case "Minimal Demo":
                            b = LaunchGXMFromFile("Programs/minimal.gxm", _apps[i].Icon);
                            break;
                    }
                    if (b) {
                        // record recents
                        RecentManager.AddProgram(dispatchName, _apps[i].Icon);
                        // apply taskbar icon if window
                        if (_apps[i].AppObject is guideXOS.GUI.Window w) {
                            w.TaskbarIcon = _apps[i].Icon;
                            w.ShowInTaskbar = true;
                        }
                    }
                }
            }
            return b;
        }
        /// <summary>
        /// Launch GXM app from file
        /// </summary>
        /// <param name="path">Path to GXM file</param>
        /// <param name="icon">Icon to use for recent items</param>
        /// <returns>True if successfully launched</returns>
        private bool LaunchGXMFromFile(string path, Image icon) {
            byte[] buffer = guideXOS.FS.File.ReadAllBytes(path);
            if (buffer == null) {
                guideXOS.GUI.NotificationManager.Add(new Notify($"File not found: {path}"));
                return false;
            }
            
            string err;
            bool ok = guideXOS.Misc.GXMLoader.TryExecute(buffer, out err);
            if (ok) {
                guideXOS.GUI.RecentManager.AddProgram(path, icon);
            } else {
                guideXOS.GUI.NotificationManager.Add(new Notify($"Failed: {err}"));
            }
            buffer.Dispose();
            return ok;
        }
        /// <summary>
        /// Add
        /// </summary>
        /// <param name="app"></param>
        public void Add(App app) {
            _apps.Add(app);
        }
        /// <summary>
        /// Length
        /// </summary>
        public int Length {
            get {
                return _apps.Count;
            }
        }
        /// <summary>
        /// Name
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public string Name(int id) { return _apps[id].Name; }
        /// <summary>
        /// Icon
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public Image Icon(int id) { return _apps[id].Icon; }
        #endregion
    }
}