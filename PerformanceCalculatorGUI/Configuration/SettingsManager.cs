// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.IO;
using System.Reflection;
using osu.Framework;
using osu.Framework.Configuration;
using osu.Framework.Platform;

namespace PerformanceCalculatorGUI.Configuration
{
    public enum Settings
    {
        ClientId,
        ClientSecret,
        DefaultPath,
        CachePath,
        ReworkId,
        LazerPath,
    }

    public class SettingsManager : IniConfigManager<Settings>
    {
        protected override string Filename => "perfcalc.ini";

        public SettingsManager(Storage storage)
            : base(storage)
        {
        }

        protected string GuessLazerStorage() {
            DesktopGameHost host = Host.GetSuitableDesktopHost("osu", new HostOptions { PortableInstallation = false });
            foreach (string path in host.UserStoragePaths)
            {
                var storage = host.GetStorage(path);

                // if an existing data directory exists for this application, prefer it immediately.
                if (storage.ExistsDirectory("osu"))
                    return storage.GetFullPath("osu");
            }
            return string.Empty;
        }

        protected override void InitialiseDefaults()
        {
            SetDefault(Settings.ClientId, string.Empty);
            SetDefault(Settings.ClientSecret, string.Empty);
            SetDefault(Settings.DefaultPath, Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location));
            SetDefault(Settings.CachePath, Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "cache"));
            SetDefault(Settings.ReworkId, string.Empty);
            SetDefault(Settings.LazerPath, GuessLazerStorage());
        }
    }
}
