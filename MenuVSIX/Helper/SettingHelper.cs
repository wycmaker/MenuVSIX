using Microsoft.VisualStudio.Settings;
using Microsoft.VisualStudio.Shell.Settings;
using Microsoft.VisualStudio.Shell;
using Newtonsoft.Json;

namespace MenuVSIX.Helper
{
    public class SettingHelper
    {
        /// <summary>
        /// 儲存一個值到指定 collection 與 key
        /// </summary>
        public static void SaveSetting(string collectionName, string key, string value)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var store = GetWritableStore();
            if (!store.CollectionExists(collectionName))
            {
                store.CreateCollection(collectionName);
            }
            store.SetString(collectionName, key, value);
        }

        /// <summary>
        /// 從指定 collection 讀取值
        /// </summary>
        public static string LoadSetting(string collectionName, string key, string defaultValue = "")
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var store = GetWritableStore();
            if (store.CollectionExists(collectionName) && store.PropertyExists(collectionName, key))
            {
                return store.GetString(collectionName, key);
            }
            return defaultValue;
        }

        /// <summary>
        /// 儲存物件
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="collection"></param>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public static void SaveObject<T>(string collection, string key, T value)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            var store = GetWritableStore();

            if (!store.CollectionExists(collection))
            {
                store.CreateCollection(collection);
            }

            string json = JsonConvert.SerializeObject(value);
            store.SetString(collection, key, json);
        }

        /// <summary>
        /// 載入物件
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="collection"></param>
        /// <param name="key"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public static T LoadObject<T>(string collection, string key, T defaultValue = default)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            var store = GetWritableStore();

            if (store.CollectionExists(collection) && store.PropertyExists(collection, key))
            {
                string json = store.GetString(collection, key);
                try
                {
                    return JsonConvert.DeserializeObject<T>(json);
                }
                catch
                {
                    return defaultValue;
                }
            }

            return defaultValue;
        }

        private static WritableSettingsStore GetWritableStore()
        {
            var shellSettingsManager = new ShellSettingsManager(ServiceProvider.GlobalProvider);
            return shellSettingsManager.GetWritableSettingsStore(SettingsScope.UserSettings);
        }
    }
}
