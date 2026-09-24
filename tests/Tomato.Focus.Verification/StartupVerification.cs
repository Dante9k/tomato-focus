using System;
using System.IO;
using Tomato;

namespace Tomato.Tests
{
    internal static class StartupVerification
    {
        internal sealed class FakeRegistration : IStartupRegistration
        {
            public string Value;
            public int Writes;
            public bool Denied;
            public string Read()
            {
                return Value;
            }

            public void Write(string command)
            {
                Writes++;
                if (Denied)
                    throw new UnauthorizedAccessException("Isolated startup test");
                Value = command;
            }
        }

        static void Require(bool condition, string message)
        {
            if (!condition)
                throw new InvalidOperationException(message);
        }

        public static void Run()
        {
            string exe = Path.Combine(Path.GetTempPath(), "Tommi 中文 path", "Tomato.exe");
            var preferences = new Preferences();
            var backend = new FakeRegistration();
            var startup = new LoginStartup(preferences, backend, exe);
            startup.Initialize();
            Require(preferences.LaunchAtLogin && preferences.LoginStartupInitialized && startup.Configured, "Startup defaults on once");
            Require(backend.Value == "\"" + exe + "\" --startup", "Spaces and Unicode paths are quoted");
            startup.Initialize();
            Require(backend.Writes == 1, "Repeated launches do not rewrite startup registration");
            string upgrade = Path.Combine(Path.GetTempPath(), "Tommi upgrade", "Tomato.exe");
            new LoginStartup(preferences, backend, upgrade).Initialize();
            Require(backend.Writes == 2 && backend.Value == LoginStartup.Command(upgrade), "Upgrade updates the existing command");
            Require(startup.SetEnabled(false) && backend.Value == null && !preferences.LaunchAtLogin, "Disable removes only our registration");
            int writes = backend.Writes;
            new LoginStartup(preferences, backend, upgrade).Initialize();
            Require(backend.Writes == writes && backend.Value == null, "Disabled preference survives an upgrade");
            Require(startup.SetEnabled(true), "Explicit re-enable succeeds");
            backend.Value = null;
            writes = backend.Writes;
            startup.Initialize();
            Require(!startup.Configured && !preferences.LaunchAtLogin && backend.Writes == writes, "External removal is respected");
            backend.Denied = true;
            Require(!startup.SetEnabled(true) && !preferences.LaunchAtLogin && startup.Error != null, "Failure leaves preference unchanged and exposes retry feedback");
            backend.Denied = false;
            Require(startup.SetEnabled(true) && startup.Error == null, "Explicit retry recovers");
            backend.Denied = true;
            Require(!startup.SetEnabled(false) && preferences.LaunchAtLogin && startup.Configured, "Failed disable retains the registered state");
            var denied = new FakeRegistration
            {
                Denied = true
            };
            var first = new LoginStartup(new Preferences(), denied, exe);
            first.Initialize();
            first.Initialize();
            Require(denied.Writes == 1, "A blocked initial request is not retried on every launch");
            bool tooLong = false;
            try
            {
                LoginStartup.Command(Path.Combine(Path.GetTempPath(), new string ('a', 250), "Tomato.exe"));
            }
            catch (IOException)
            {
                tooLong = true;
            }

            Require(tooLong, "Run commands exceeding Windows' limit are rejected");
        }
    }
}
