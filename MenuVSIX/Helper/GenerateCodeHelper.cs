using EnvDTE;
using Microsoft.VisualStudio.Shell;
using System;
using System.IO;
using System.Linq;
using System.Reflection;

namespace MenuVSIX.Helper
{
    public class GenerateCodeHelper
    {
        /// <summary>
        /// 取得解決方案路徑
        /// </summary>
        /// <returns></returns>
        public static string GetSolutionDirectory()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            var dte = (DTE)Package.GetGlobalService(typeof(DTE));
            string path = dte?.Solution?.FullName;
            return string.IsNullOrEmpty(path) ? "" : Path.GetDirectoryName(path);
        }

        /// <summary>
        /// 取得專案路徑
        /// </summary>
        /// <returns></returns>
        public static string GetProjectDirectory()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            DTE dte = (DTE)Package.GetGlobalService(typeof(DTE));

            if (dte.SelectedItems.Count == 0)
                return null;

            foreach (SelectedItem selItem in dte.SelectedItems)
            {
                Project proj = selItem.Project;
                if (proj != null && !string.IsNullOrEmpty(proj.FullName))
                {
                    return Path.GetDirectoryName(proj.FullName);
                }
            }

            return null;
        }

        /// <summary>
        /// 取得範本檔案
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static string GetTemplate(ETemplateType type)
        {
            var assembly = Assembly.GetExecutingAssembly();

            // Namespace + 資料夾路徑 + 檔名
            string resourceName = $"MenuVSIX.Template.{type.ToString()}.txt";

            using (Stream stream = assembly.GetManifestResourceStream(resourceName))
            using (StreamReader reader = new StreamReader(stream))
            {
                return reader.ReadToEnd();
            }
        }

        /// <summary>
        /// 寫入檔案
        /// </summary>
        /// <param name="folder"></param>
        /// <param name="fileName"></param>
        /// <param name="content"></param>
        public static void WriteFile(string folder, string fileName, string content)
        {
            Directory.CreateDirectory(folder);
            string fullPath = Path.Combine(folder, fileName);

            EnvDTE.DTE dte = Package.GetGlobalService(typeof(EnvDTE.DTE)) as EnvDTE.DTE;
            var doc = dte?.Documents
                .Cast<EnvDTE.Document>()
                .FirstOrDefault(d => string.Equals(d.FullName, fullPath, StringComparison.OrdinalIgnoreCase));

            if (doc != null)
            {
                doc.Close(EnvDTE.vsSaveChanges.vsSaveChangesYes); // 關閉已開啟的檔案，確保寫入成功
                File.SetAttributes(fullPath, FileAttributes.Normal);
            }

            // 然後寫入
            File.WriteAllText(fullPath, content);
        }
    }

    /// <summary>
    /// 範本類別
    /// </summary>
    public enum ETemplateType
    {
        Controller = 1,
        Service = 2,
        IService = 3,
        BaseRepository = 4,
        CrudRepository = 5,
        IBaseRepository = 6,
        ICrudRepository = 7,
        Profile = 8,
        QueryModel = 9,
        ListModel = 10,
        ViewModel = 11,

    }
}
