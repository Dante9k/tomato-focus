using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Windows.Forms;

[assembly: AssemblyTitle("朱果 · 安装程序")]
[assembly: AssemblyDescription("朱果 Tomato Focus Windows 安装程序")]
[assembly: AssemblyProduct("朱果 · Tomato Focus")]
[assembly: AssemblyCopyright("朱果 · 保留所有权利")]

internal static class Setup
{
    private static readonly string[] Files = { "Tomato.exe", "Tomato.exe.config", "README.md", "LICENSE", "CHANGELOG.md", "VALIDATION.md", "preview.png" };

    private static string ResourceText(string name)
    {
        using (var reader = new StreamReader(Assembly.GetExecutingAssembly().GetManifestResourceStream(name))) return reader.ReadToEnd().Trim();
    }

    private static byte[] Payload()
    {
        byte[] bytes;
        using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Payload.zip"))
        using (var memory = new MemoryStream()) { stream.CopyTo(memory); bytes = memory.ToArray(); }
        using (var sha = SHA256.Create())
        {
            var hash = BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-", "");
            if (!String.Equals(hash, ResourceText("Payload.sha256"), StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException("安装包校验失败，请重新下载。");
        }
        using (var zip = new ZipArchive(new MemoryStream(bytes), ZipArchiveMode.Read))
        {
            if (!zip.Entries.Select(e => e.FullName).OrderBy(n => n).SequenceEqual(Files.OrderBy(n => n))) throw new InvalidDataException("安装包文件清单无效。");
            // Read every entry to catch truncated compressed data before changing the installation.
            foreach (var entry in zip.Entries) using (var stream = entry.Open()) { stream.CopyTo(Stream.Null); }
        }
        return bytes;
    }

    [STAThread]
    private static int Main(string[] args)
    {
        try
        {
            byte[] bytes = Payload();
            string version = ResourceText("Version.txt");
            if (!System.Text.RegularExpressions.Regex.IsMatch(version, @"^\d+\.\d+\.\d+$")) throw new InvalidDataException("版本信息无效。");
            if (args.Length == 1 && args[0] == "--verify-payload") return 0;
            bool render = args.Length == 2 && args[0] == "--render-preview";
            if (args.Length != 0 && !render) return 2;
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            using (var form = CreateForm(version, bytes))
            {
                if (render)
                {
                    CreateHandles(form);
                    form.PerformLayout();
                    using (var preview = new Bitmap(form.Width, form.Height))
                    {
                        form.DrawToBitmap(preview, new Rectangle(Point.Empty, preview.Size));
                        preview.Save(Path.GetFullPath(args[1]), System.Drawing.Imaging.ImageFormat.Png);
                    }
                }
                else Application.Run(form);
            }
            return 0;
        }
        catch (Exception ex) { if (args.Length == 0) MessageBox.Show(ex.Message, "朱果安装程序", MessageBoxButtons.OK, MessageBoxIcon.Error); return 1; }
    }

    private static void CreateHandles(Control control)
    {
        // WM_PRINT needs child HWNDs even though the preview never shows a window.
        var handle = control.Handle;
        foreach (Control child in control.Controls) CreateHandles(child);
    }

    private static Form CreateForm(string version, byte[] bytes)
    {
        var ink = Color.FromArgb(36, 44, 56);
        var muted = Color.FromArgb(95, 105, 119);
        var red = Color.FromArgb(207, 59, 40);
        var form = new Form
        {
            Text = "朱果安装程序",
            Font = new Font("Microsoft YaHei UI", 10),
            AutoScaleDimensions = new SizeF(96, 96),
            AutoScaleMode = AutoScaleMode.Dpi,
            ClientSize = new Size(760, 500),
            FormBorderStyle = FormBorderStyle.FixedDialog,
            MaximizeBox = false,
            StartPosition = FormStartPosition.CenterScreen,
            BackColor = Color.White,
            ForeColor = ink,
            Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath)
        };
        var side = new Panel { Location = Point.Empty, Size = new Size(246, 500), BackColor = Color.FromArgb(255, 243, 238) };
        var logo = new PictureBox { Location = new Point(45, 49), Size = new Size(152, 152), SizeMode = PictureBoxSizeMode.Zoom, TabStop = false };
        using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Brand.Logo.png"))
        using (var image = Image.FromStream(stream)) logo.Image = new Bitmap(image);
        side.Controls.Add(logo);
        side.Controls.Add(TextLabel("朱果", 43, 221, 165, 50, 28, ink, true));
        side.Controls.Add(TextLabel("TOMATO FOCUS", 47, 279, 175, 24, 10, muted));
        side.Controls.Add(TextLabel("一颗番茄，\n一段完整的专注。", 45, 331, 176, 66, 12, muted));
        side.Controls.Add(TextLabel("WINDOWS 10 / 11 · x64", 45, 452, 195, 22, 9, muted));
        form.Controls.Add(side);
        var title = TextLabel("欢迎安装朱果", 292, 49, 425, 46, 23, ink, true);
        var description = TextLabel("把时间，留给喜欢的事。\n离线使用，无需登录。", 295, 108, 410, 62, 11, muted);
        var versionLabel = TextLabel("版本 " + version + "    /    桌面番茄钟", 295, 182, 410, 28, 10, muted);
        var separator = new Panel { Location = new Point(296, 227), Size = new Size(412, 1), BackColor = Color.FromArgb(232, 234, 238) };
        var pathLabel = TextLabel("安装位置", 295, 249, 410, 24, 10, ink, true);
        string target = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", "TomatoFocus", version);
        var path = new TextBox { Text = target, ReadOnly = true, Multiline = true, WordWrap = true, Font = new Font("Microsoft YaHei UI", 9), Location = new Point(297, 282), Size = new Size(411, 42), BackColor = Color.FromArgb(248, 249, 251), BorderStyle = BorderStyle.FixedSingle, AccessibleName = "安装位置", TabIndex = 0 };
        var desktop = new CheckBox { Text = "创建桌面快捷方式", Checked = true, Location = new Point(296, 341), AutoSize = true, TabIndex = 1 };
        var status = TextLabel("无需管理员权限，不添加开机自启。", 295, 377, 415, 40, 9, muted);
        var install = new Button { Text = "开始安装", Location = new Point(558, 431), Size = new Size(150, 43), BackColor = red, ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand, TabIndex = 2 };
        install.FlatAppearance.BorderSize = 0;
        install.FlatAppearance.MouseOverBackColor = Color.FromArgb(181, 47, 31);
        var cancel = new Button { Text = "取消", Location = new Point(445, 431), Size = new Size(95, 43), BackColor = Color.White, FlatStyle = FlatStyle.Flat, TabIndex = 3 };
        cancel.FlatAppearance.BorderColor = Color.FromArgb(220, 225, 232);
        cancel.Click += delegate { form.Close(); };
        bool installed = false;
        install.Click += delegate
        {
            if (installed) { form.Close(); return; }
            install.Enabled = false;
            try
            {
                if (Process.GetProcessesByName("Tomato").Length != 0) throw new IOException("请先从托盘退出朱果，再点击安装。安装程序不会强制关闭应用。");
                Install(bytes, target, desktop.Checked);
                installed = true;
                title.Text = "朱果已准备就绪";
                description.Text = "从开始菜单打开「朱果番茄钟」，\n开始你的下一段专注。";
                install.Text = "完成";
                desktop.Enabled = false;
                cancel.Visible = false;
                status.Text = "卸载时退出朱果，删除安装文件夹及快捷方式即可。";
            }
            catch (Exception ex) { MessageBox.Show(form, ex.Message, "安装未完成", MessageBoxButtons.OK, MessageBoxIcon.Warning); }
            finally { install.Enabled = true; }
        };
        form.Controls.AddRange(new Control[] { title, description, versionLabel, separator, pathLabel, path, desktop, status, install, cancel });
        form.AcceptButton = install;
        form.CancelButton = cancel;
        form.Disposed += delegate { logo.Image.Dispose(); form.Icon.Dispose(); };
        return form;
    }

    private static Label TextLabel(string text, int x, int y, int width, int height, float size, Color color, bool bold = false)
    {
        return new Label { Text = text, Location = new Point(x, y), Size = new Size(width, height),
            Font = new Font("Microsoft YaHei UI", size, bold ? FontStyle.Bold : FontStyle.Regular),
            ForeColor = color, BackColor = Color.Transparent };
    }

    private static void Install(byte[] bytes, string target, bool desktop)
    {
        string root = Path.GetFullPath(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", "TomatoFocus"));
        target = Path.GetFullPath(target);
        if (!target.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)) throw new IOException("安装目录无效。");
        for (var directory = new DirectoryInfo(target); directory != null; directory = directory.Parent)
            if (directory.Exists && (directory.Attributes & FileAttributes.ReparsePoint) != 0) throw new IOException("安装目录包含目录链接，请使用普通用户目录。");
        // Existing versions are immutable: never overwrite a user's different executable or files.
        if (Directory.Exists(target))
        {
            if (!Directory.GetFiles(target).Select(Path.GetFileName).OrderBy(n => n).SequenceEqual(Files.OrderBy(n => n)) || Directory.GetDirectories(target).Length != 0)
                throw new IOException("相同版本的目录已经存在且内容不同，已保留原文件。请先检查该目录。");
            using (var zip = new ZipArchive(new MemoryStream(bytes), ZipArchiveMode.Read))
            foreach (var entry in zip.Entries)
                using (var source = entry.Open())
                using (var existing = File.OpenRead(Path.Combine(target, entry.FullName)))
                using (var sha = SHA256.Create())
                    if (!sha.ComputeHash(source).SequenceEqual(sha.ComputeHash(existing))) throw new IOException("现有版本与安装包不同，未覆盖文件。");
        }
        else
        {
            Directory.CreateDirectory(root);
            string staging = Path.Combine(root, ".install-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(staging);
            try
            {
                using (var zip = new ZipArchive(new MemoryStream(bytes), ZipArchiveMode.Read))
                foreach (var entry in zip.Entries) entry.ExtractToFile(Path.Combine(staging, entry.FullName));
                Directory.Move(staging, target);
            }
            finally
            {
                if (Directory.Exists(staging))
                {
                    foreach (var name in Files) { string file = Path.Combine(staging, name); if (File.Exists(file)) File.Delete(file); }
                    Directory.Delete(staging, false);
                }
            }
        }
        Shortcut(Environment.GetFolderPath(Environment.SpecialFolder.Programs), target);
        if (desktop) Shortcut(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), target);
    }

    private static void Shortcut(string folder, string target)
    {
        Directory.CreateDirectory(folder);
        var linkPath = Path.Combine(folder, "朱果番茄钟.lnk");
        if (File.Exists(linkPath))
        {
            var backupRoot = Path.Combine(Path.GetDirectoryName(target), "shortcut-backups", DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fffffff"));
            Directory.CreateDirectory(backupRoot);
            File.Copy(linkPath, Path.Combine(backupRoot, "朱果番茄钟.lnk"));
        }
        Type shellType = Type.GetTypeFromProgID("WScript.Shell");
        object shell = Activator.CreateInstance(shellType);
        object link = null;
        try
        {
            link = shellType.InvokeMember("CreateShortcut", BindingFlags.InvokeMethod, null, shell, new object[] { linkPath });
            var type = link.GetType();
            type.InvokeMember("TargetPath", BindingFlags.SetProperty, null, link, new object[] { Path.Combine(target, "Tomato.exe") });
            type.InvokeMember("WorkingDirectory", BindingFlags.SetProperty, null, link, new object[] { target });
            type.InvokeMember("Arguments", BindingFlags.SetProperty, null, link, new object[] { "" });
            type.InvokeMember("IconLocation", BindingFlags.SetProperty, null, link, new object[] { Path.Combine(target, "Tomato.exe") + ",0" });
            type.InvokeMember("Save", BindingFlags.InvokeMethod, null, link, null);
        }
        finally
        {
            if (link != null) System.Runtime.InteropServices.Marshal.FinalReleaseComObject(link);
            System.Runtime.InteropServices.Marshal.FinalReleaseComObject(shell);
        }
    }
}
