using FullSerializer;
using player.Core.Service;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Log = player.Core.Logging.Logger;

namespace player.Core.Settings
{
    class SettingsService : IService
    {
        public static string SettingsFileName = "settings";
        private Dictionary<string, object> settingsDict;
        private Dictionary<string, List<SettingUpdatedDelegate>> settingListenerDelegates = new Dictionary<string, List<SettingUpdatedDelegate>>();
        private List<BaseSettingsAccessor> accessors = new List<BaseSettingsAccessor>();

        private string settingsDirectory;
        private string settingsFile;

        public string ServiceName { get { return "Settings Service"; } }

        public delegate void SettingUpdatedDelegate(string Key, object Value);

        public event EventHandler OnSettingsReloaded;

        internal SettingsService()
        {
            settingsDirectory = Path.Combine(Environment.CurrentDirectory, "settings");
            settingsFile = GetFileRelativeToSettings($"{SettingsFileName}.json");
            DirectoryInfo SettingsDir = new DirectoryInfo(settingsDirectory);
            if (!SettingsDir.Exists)
            {
                SettingsDir.Create();
            }
            Load();
        }

        public void Initialize() { }
        public void Cleanup() { }

        public void ReloadSettingsFile(string settingsFileName)
        {
            SettingsFileName = settingsFileName;
            settingsFile = GetFileRelativeToSettings($"{SettingsFileName}.json");
            DirectoryInfo SettingsDir = new DirectoryInfo(settingsDirectory);
            if (!SettingsDir.Exists)
            {
                SettingsDir.Create();
            }
            Load();
            MainThreadDelegator.InvokeOn(InvocationTarget.AfterRender, () =>
            {
                OnSettingsReloaded?.Invoke(this, EventArgs.Empty);
                Log.Log($"Settings file changed to {settingsFileName}");
            });
        }

        public SettingsAccessor<T> GetAccessor<T>(string key, T defaultValue)
        {
            var newAccessor = new SettingsAccessor<T>(this, key, defaultValue);
            accessors.Add(newAccessor);
            return newAccessor;
        }

        public string GetFileRelativeToSettings(string file)
        {
            return Path.Combine(settingsDirectory, file);
        }

        public void AddSettingShouldNofity(string SettingName, SettingUpdatedDelegate SettingUpdatedHandler)
        {
            if (!settingListenerDelegates.ContainsKey(SettingName))
            {
                List<SettingUpdatedDelegate> DelegatesList = new List<SettingUpdatedDelegate>();
                DelegatesList.Add(SettingUpdatedHandler);
                settingListenerDelegates.Add(SettingName, DelegatesList);
            }
            else
            {
                settingListenerDelegates[SettingName].Add(SettingUpdatedHandler);
            }
        }

        public void RemoveNotifyHandler(string SettingName, SettingUpdatedDelegate Handler)
        {
            if (settingListenerDelegates.ContainsKey(SettingName))
            {
                List<SettingUpdatedDelegate> DelegateList = settingListenerDelegates[SettingName];
                if (DelegateList.Contains(Handler)) DelegateList.Remove(Handler);
            }
        }

        public bool DoesSettingExist(string Key)
        {
            return settingsDict.ContainsKey(Key);
        }

        public void SetSetting(string key, object value)
        {
            if (DoesSettingExist(key))
            {
                settingsDict[key] = value;
            }
            else
            {
                settingsDict.Add(key, value);
            }
        }

        public void SetSettingDefault(string Key, object Value)
        {
            if (DoesSettingExist(Key)) return;
            SetSetting(Key, Value);
        }

        public T GetSettingAs<T>(string Key, T DefaultValue)
        {
            return (T)GetSetting(Key, DefaultValue);
        }

        public object GetSetting(string Key, object DefaultValue, bool autoCreateSetting = true)
        {
            if (!DoesSettingExist(Key))
            {
                if (autoCreateSetting)
                {
                    SetSetting(Key, DefaultValue);
                }
                return DefaultValue;
            }
            return settingsDict[Key];
        }

        private void Load()
        {
            if (File.Exists(settingsFile))
            {
                string fileContents = File.ReadAllText(settingsFile);
                try
                {
                    settingsDict = JsonToObject<Dictionary<string, object>>(fileContents);
                }
                catch (Exception e)
                {
                    Log.Log("Unable to deserialize settings! Exception: {0}", e.ToString());
                    settingsDict = new Dictionary<string, object>();
                }
            }
            else
            {
                settingsDict = new Dictionary<string, object>();
            }
        }

        public void Save()
        {
            foreach (var accessor in accessors)
            {
                accessor.Save();
            }
            string serializedDict;
            try
            {
                serializedDict = ObjectToJson(settingsDict);
            }
            catch (Exception e)
            {
                Log.Log("Unable to serialize settings dict! Loss of settings may occur! Exception: {0}", e.ToString());
                return;
            }

            if (File.Exists(settingsFile))
            {
                File.Delete(settingsFile);
            }

            File.WriteAllText(settingsFile, serializedDict);
        }

        public string[] GetAllSettings()
        {
            string[] dictKeys = settingsDict.Keys.ToArray(); //Make a copy of the keys
            return dictKeys;
        }

        private void NotifySettingChanged(string key, object value)
        {
            if (settingListenerDelegates.ContainsKey(key))
            {
                settingListenerDelegates[key].ForEach((obj) =>
                {
                    try
                    {
                        obj(key, value);
                    }
                    catch (Exception e)
                    {
                        Log.Log("A delegate for setting `{0}` has thrown exception: {1}", key, e.ToString());
                    }
                });//Call all delegates and swallow exceptions
            }
        }

        internal static T JsonToObject<T>(string jsonString)
        {
            T obj = default(T);

            fsSerializer serializer = new fsSerializer();
            fsData data = fsJsonParser.Parse(jsonString);
            fsResult result = serializer.TryDeserialize<T>(data, ref obj);
            if (!result.Succeeded)
            {
                throw result.AsException;
            }
            return obj;
        }

        internal static string ObjectToJson(object objectToConvert)
        {
            fsSerializer serializer = new fsSerializer();
            fsData dataInst;
            fsResult result = serializer.TrySerialize(objectToConvert, out dataInst);
            if (!result.Succeeded)
            {
                throw result.AsException;
            }
            else
            {
                return fsJsonPrinter.CompressedJson(dataInst);
            }
        }
    }
}
