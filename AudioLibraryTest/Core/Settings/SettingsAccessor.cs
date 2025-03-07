namespace player.Core.Settings
{
    class SettingsAccessor<T> : BaseSettingsAccessor
    {
        string key;
        T defaultValue;

        public T Value;

        internal SettingsAccessor(SettingsService settings, string key, T defaultValue) : base(settings)
        {
            this.settings = settings;
            this.key = key;
            this.defaultValue = defaultValue;
            Invalidate();
        }

        /// <summary>
        /// Gets the last value, and invalidates the fast cache
        /// </summary>
        /// <returns></returns>
        public T Invalidate()
        {
            Value = settings.GetSettingAs(key, defaultValue);
            return Value;
        }

        public void Set(T value)
        {
            settings.SetSetting(key, value);
        }

        /// <summary>
        /// Saves the value of LastValue
        /// </summary>
        public override void Save()
        {
            settings.SetSetting(key, Value);
        }
    }

    abstract class BaseSettingsAccessor
    {
        protected SettingsService settings;

        protected BaseSettingsAccessor(SettingsService service)
        {
            settings = service;
        }

        public abstract void Save();
    }
}
