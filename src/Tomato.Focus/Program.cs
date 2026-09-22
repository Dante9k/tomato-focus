using System;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Threading;

namespace Tomato
{
    internal static class Program
    {
        [STAThread]
        private static int Main()
        {
            bool firstInstance;
            using (var mutex = new Mutex(true, "Local\\TomatoFocus.Desktop", out firstInstance))
            {
                if (!firstInstance)
                {
                    MessageBox.Show("朱果已经在运行。请双击任务栏右下角的番茄图标。", "朱果");
                    return 0;
                }

                var application = new Application
                {
                    ShutdownMode = ShutdownMode.OnExplicitShutdown
                };
                AppController controller = null;
                application.DispatcherUnhandledException += delegate (object sender, DispatcherUnhandledExceptionEventArgs e)
                {
                    AppController.Log(e.Exception);
                    MessageBox.Show("朱果遇到问题，已记录错误。请重新打开软件。", "朱果");
                    if (controller != null)
                        controller.Quit();
                    e.Handled = true;
                };
                try
                {
                    string statePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "TomatoFocus", "state.xml");
                    controller = new AppController(application, statePath);
                    controller.Launch(true);
                    application.Run();
                    return 0;
                }
                catch (Exception exception)
                {
                    AppController.Log(exception);
                    MessageBox.Show("启动失败：" + exception.Message, "朱果");
                    return 1;
                }
            }
        }
    }
}
