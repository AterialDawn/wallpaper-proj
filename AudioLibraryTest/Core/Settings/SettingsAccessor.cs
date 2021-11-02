using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace player.Core.Settings
{
    class SettingsAccessor<T>
    {
        SettingsService settings;
        string key;
        T defaultValue;

        internal SettingsAccessor(SettingsService settings, string key, T defaultValue)
        {
            this.settings = settings;
            this.key = key;
            this.defaultValue = defaultValue;
        }

        public T Get()
        {
            return settings.GetSettingAs<T>(key, defaultValue);
        }

        public void Set(T value)
        {
            settings.SetSetting(key, value);
        }
    }
}
