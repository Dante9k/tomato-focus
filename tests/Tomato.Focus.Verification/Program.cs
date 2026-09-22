using System;
using System.IO;
using System.Windows;
using Tomato;

namespace Tomato.Tests
{
    internal static class Program
    {
        [STAThread]
        private static int Main(string[] args)
        {
            string mode = args.Length == 0 ? "--self-test" : args[0];
            if (mode == "--self-test")
                return Verification.Run();
            if (mode == "--render-preview")
                return Verification.Render();
            if (mode != "--smoke-test")
            {
                Console.Error.WriteLine("Unknown mode: " + mode);
                Console.Error.WriteLine("Usage: Tomato.Verify.exe [--self-test|--render-preview|--smoke-test]");
                return 2;
            }

            var application = new Application
            {
                ShutdownMode = ShutdownMode.OnExplicitShutdown
            };
            AppController controller = null;
            application.DispatcherUnhandledException += delegate (object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
            {
                Verification.SmokeExitCode = 1;
                File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "smoke-results.txt"), "FAIL " + e.Exception);
                if (controller != null)
                    controller.Quit();
                e.Handled = true;
            };
            try
            {
                controller = new AppController(application, Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "smoke-state.xml"));
                controller.Launch(false);
                Verification.Smoke(controller);
                application.Run();
                return Verification.SmokeExitCode;
            }
            catch (Exception exception)
            {
                Console.Error.WriteLine(exception);
                if (controller != null)
                    controller.Quit();
                return 1;
            }
        }
    }
}
