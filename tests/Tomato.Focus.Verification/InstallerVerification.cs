using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text;
using System.Windows.Forms;

namespace Tomato.Tests
{
    internal static class InstallerVerification
    {
        private static Type setup;
        private static object Call(string name, params object[] args)
        {
            try
            {
                return setup.GetMethod(name, BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, args);
            }
            catch (TargetInvocationException error)
            {
                throw error.InnerException;
            }
        }

        private static void Require(bool condition, string message)
        {
            if (!condition)
                throw new InvalidOperationException(message);
        }

        private static void Reject(Action action)
        {
            try
            {
                action();
            }
            catch (IOException)
            {
                return;
            }
            catch (UnauthorizedAccessException)
            {
                return;
            }

            throw new InvalidOperationException("An invalid installation was accepted.");
        }

        private static void VerifyFiles(byte[] bytes, string target)
        {
            using (var zip = new ZipArchive(new MemoryStream(bytes), ZipArchiveMode.Read))
            {
                Require(Directory.GetFiles(target).Select(Path.GetFileName).OrderBy(n => n).SequenceEqual(zip.Entries.Select(e => e.FullName).OrderBy(n => n)), "Installed file list differs.");
                foreach (var entry in zip.Entries)
                    using (var source = entry.Open())
                    using (var actual = File.OpenRead(Path.Combine(target, entry.FullName)))
                    using (var sha = SHA256.Create())
                        Require(sha.ComputeHash(source).SequenceEqual(sha.ComputeHash(actual)), "Installed bytes differ: " + entry.FullName);
            }
        }

        private static void VerifyShortcut(string folder, string target)
        {
            Call("Shortcut", folder, target);
            string path = Path.Combine(folder, "朱果番茄钟.lnk");
            byte[] original = File.ReadAllBytes(path);
            Call("Shortcut", folder, target);
            string backupRoot = Path.Combine(Path.GetDirectoryName(target), "shortcut-backups");
            Require(Directory.GetFiles(backupRoot, "*.lnk", SearchOption.AllDirectories).Any(file => File.ReadAllBytes(file).SequenceEqual(original)), "Existing shortcut was not backed up.");
            Type shellType = Type.GetTypeFromProgID("WScript.Shell");
            object shell = Activator.CreateInstance(shellType);
            object link = null;
            try
            {
                link = shellType.InvokeMember("CreateShortcut", BindingFlags.InvokeMethod, null, shell, new object[] { path });
                string destination = (string)link.GetType().InvokeMember("TargetPath", BindingFlags.GetProperty, null, link, null);
                string working = (string)link.GetType().InvokeMember("WorkingDirectory", BindingFlags.GetProperty, null, link, null);
                Require(String.Equals(destination, Path.Combine(target, "Tomato.exe"), StringComparison.OrdinalIgnoreCase), "Shortcut points to the default path instead of the selected path.");
                Require(String.Equals(working, target, StringComparison.OrdinalIgnoreCase), "Shortcut working directory differs.");
            }
            finally
            {
                if (link != null)
                    Marshal.FinalReleaseComObject(link);
                Marshal.FinalReleaseComObject(shell);
            }
        }

        public static int Run(string installer)
        {
            string root = Path.Combine(Path.GetTempPath(), "TomatoFocus-install-test-" + Guid.NewGuid().ToString("N"));
            string report = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "installer-results.txt");
            var log = new StringBuilder();
            try
            {
                setup = Assembly.LoadFrom(Path.GetFullPath(installer)).GetType("Setup", true);
                byte[] bytes = (byte[])Call("Payload");
                Directory.CreateDirectory(root);
                foreach (string input in new[]
                {
                    "",
                    "relative",
                    "C:relative",
                    "\\relative",
                    "C:\\",
                    "\\\\server\\share",
                    "C:\\bad?name",
                    "C:\\NUL",
                    "C:\\bad.\\app",
                    "C:\\" + new string ('x', 260)
                }

                )
                    Reject(delegate
                    {
                        Call("NormalizeTarget", input);
                    });
                string fileTarget = Path.Combine(root, "existing-file");
                File.WriteAllText(fileTarget, "keep");
                Reject(delegate
                {
                    Call("InstallFiles", bytes, fileTarget);
                });
                Reject(delegate
                {
                    Call("InstallFiles", bytes, Path.Combine(fileTarget, "child"));
                });
                Require(File.ReadAllText(fileTarget) == "keep", "Existing file was modified.");
                log.AppendLine("PASS invalid, relative, root, network, device, long and file paths rejected");
                string custom = Path.Combine(root, "自定义 目录", "Tomato Focus");
                Call("InstallFiles", bytes, custom);
                VerifyFiles(bytes, custom);
                Call("InstallFiles", bytes, custom);
                VerifyFiles(bytes, custom);
                string empty = Path.Combine(root, "existing empty");
                Directory.CreateDirectory(empty);
                Call("InstallFiles", bytes, empty);
                VerifyFiles(bytes, empty);
                Require(!Directory.GetDirectories(root, ".tomato-install-*", SearchOption.AllDirectories).Any(), "Staging folder was left behind.");
                log.AppendLine("PASS custom Unicode/space path, new and empty folders, exact files and repeat install");
                string sentinel = Path.Combine(custom, "user-file.txt");
                File.WriteAllText(sentinel, "preserve user content");
                Reject(delegate
                {
                    Call("InstallFiles", bytes, custom);
                });
                Require(File.ReadAllText(sentinel) == "preserve user content", "User content was overwritten.");
                File.AppendAllText(Path.Combine(empty, "README.md"), "user edit");
                Reject(delegate
                {
                    Call("InstallFiles", bytes, empty);
                });
                Require(File.ReadAllText(Path.Combine(empty, "README.md")).EndsWith("user edit"), "Modified installation was overwritten.");
                log.AppendLine("PASS unrelated files and modified existing installations preserved");
                string denied = Path.Combine(root, "unwritable");
                Directory.CreateDirectory(denied);
                var restricted = Directory.GetAccessControl(denied, AccessControlSections.Access);
                byte[] original = restricted.GetSecurityDescriptorBinaryForm();
                restricted.AddAccessRule(new FileSystemAccessRule(WindowsIdentity.GetCurrent().User, FileSystemRights.CreateDirectories, AccessControlType.Deny));
                try
                {
                    Directory.SetAccessControl(denied, restricted);
                    Reject(delegate
                    {
                        Call("InstallFiles", bytes, Path.Combine(denied, "app"));
                    });
                    Require(!Directory.EnumerateFileSystemEntries(denied).Any(), "Failed write created partial installation.");
                }
                finally
                {
                    var restored = new DirectorySecurity();
                    restored.SetSecurityDescriptorBinaryForm(original, AccessControlSections.Access);
                    Directory.SetAccessControl(denied, restored);
                }

                log.AppendLine("PASS unwritable selected directory fails without a partial installation");
                string selected = Path.Combine(root, "from edited UI");
                string received = null;
                Action<string, bool> install = delegate (string destination, bool desktop)
                {
                    received = destination;
                    Call("InstallFiles", bytes, destination);
                    VerifyShortcut(Path.Combine(root, "isolated shortcuts"), destination);
                };
                Func<bool> running = delegate
                {
                    return false;
                };
                using (var form = (Form)Call("CreateInstallForm", "test", install, running))
                {
                    var path = (TextBox)form.Controls.Find("InstallationPath", true).Single();
                    var browse = (Button)form.Controls.Find("BrowseFolder", true).Single();
                    var button = (Button)form.Controls.Find("InstallButton", true).Single();
                    Require(!path.ReadOnly && browse.Enabled, "Destination cannot be changed.");
                    path.Text = selected;
                    typeof(Button).GetMethod("OnClick", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(button, new object[] { EventArgs.Empty });
                    Require(received == selected, "Install action ignored edited destination.");
                    Require(path.ReadOnly && !browse.Enabled && button.Text == "完成", "Successful form state is incorrect.");
                }

                VerifyFiles(bytes, selected);
                log.AppendLine("PASS actual install-button handler uses edited path; shortcuts and backups verified in isolation");
                File.WriteAllText(report, log.ToString());
                return 0;
            }
            catch (Exception error)
            {
                File.WriteAllText(report, log + "FAIL " + error);
                return 1;
            }
            finally
            {
                // This unique directory contains only fixtures created by this invocation.
                if (Directory.Exists(root))
                    Directory.Delete(root, true);
            }
        }
    }
}
