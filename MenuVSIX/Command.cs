using System;
using System.ComponentModel.Design;
using System.IO;
using System.Text.RegularExpressions;
using EnvDTE;
using MenuVSIX.Helper;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Task = System.Threading.Tasks.Task;

namespace MenuVSIX
{
    /// <summary>
    /// Command handler
    /// </summary>
    internal sealed class Command
    {
        /// <summary>
        /// Command ID.
        /// </summary>
        public const int CommandId = 0x0100;

        /// <summary>
        /// Command menu group (command set GUID).
        /// </summary>
        public static readonly Guid CommandSet = new Guid("8bd7d1a0-c67e-441b-9857-1987d3025498");

        /// <summary>
        /// VS Package that provides this command, not null.
        /// </summary>
        private readonly AsyncPackage package;

        /// <summary>
        /// Initializes a new instance of the <see cref="Command"/> class.
        /// Adds our command handlers for menu (commands must exist in the command table file)
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        /// <param name="commandService">Command service to add command to, not null.</param>
        private Command(AsyncPackage package, OleMenuCommandService commandService)
        {
            this.package = package ?? throw new ArgumentNullException(nameof(package));
            commandService = commandService ?? throw new ArgumentNullException(nameof(commandService));

            var menuCommandID = new CommandID(CommandSet, CommandId);
            var menuItem = new MenuCommand(this.Start, menuCommandID);
            commandService.AddCommand(menuItem);
        }

        /// <summary>
        /// Gets the instance of the command.
        /// </summary>
        public static Command Instance
        {
            get;
            private set;
        }

        private FormModel _formModel;

        /// <summary>
        /// Initializes the singleton instance of the command.
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        public static async Task InitializeAsync(AsyncPackage package)
        {
            // Switch to the main thread - the call to AddCommand in Command's constructor requires
            // the UI thread.
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);

            OleMenuCommandService commandService = await package.GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;
            Instance = new Command(package, commandService);
        }

        private void Start(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var window = new InputWindow();
            var result = window.ShowDialog(); // 注意：必須在 UI 執行緒中

            if (result != true) return;

            _formModel = (window.DataContext as FormModel);

            var solutionPath = GenerateCodeHelper.GetSolutionDirectory();
            var template = "";
            var content = "";

            if (_formModel.IsController)
            {
                // 產生Controller
                template = GenerateCodeHelper.GetTemplate(ETemplateType.Controller);
                content = GenerateContent(template);
                GenerateCodeHelper.WriteFile(Path.Combine(solutionPath, _formModel.ApiPath, "Controllers"), $"{_formModel.TableName}Controller.cs", content);
            }

            if (_formModel.IsService)
            {
                // 產生 core 專案相關檔案
                template = GenerateCodeHelper.GetTemplate(ETemplateType.ListModel);
                content = GenerateContent(template);
                GenerateCodeHelper.WriteFile(Path.Combine(solutionPath, _formModel.CorePath, "Models", _formModel.TableName), $"{_formModel.ListModel}.cs", content);

                template = GenerateCodeHelper.GetTemplate(ETemplateType.ViewModel);
                content = GenerateContent(template);
                GenerateCodeHelper.WriteFile(Path.Combine(solutionPath, _formModel.CorePath, "Models", _formModel.TableName), $"{_formModel.ViewModel}.cs", content);

                template = GenerateCodeHelper.GetTemplate(ETemplateType.Profile);
                content = GenerateContent(template);
                GenerateCodeHelper.WriteFile(Path.Combine(solutionPath, _formModel.CorePath, "Profiles"), $"{_formModel.TableName}Profile.cs", content);

                template = GenerateCodeHelper.GetTemplate(ETemplateType.IService);
                content = GenerateContent(template);
                GenerateCodeHelper.WriteFile(Path.Combine(solutionPath, _formModel.CorePath, "Services", "Interface"), $"I{_formModel.TableName}Service.cs", content);

                template = GenerateCodeHelper.GetTemplate(ETemplateType.Service);
                content = GenerateContent(template);
                GenerateCodeHelper.WriteFile(Path.Combine(solutionPath, _formModel.CorePath, "Services", "Implement"), $"{_formModel.TableName}Service.cs", content);
            }


            // 產生 data 專案相關檔案
            if (_formModel.IsSub)
            {
                template = GenerateCodeHelper.GetTemplate(ETemplateType.QueryModel);
                content = GenerateContent(template, _formModel.IsSub);
                GenerateCodeHelper.WriteFile(Path.Combine(solutionPath, _formModel.DataPath, "Queries"), $"{_formModel.QueryModel}.cs", content);
            }

            if(_formModel.IsView)
            {
                template = GenerateCodeHelper.GetTemplate(ETemplateType.IBaseRepository);
                content = GenerateContent(template, _formModel.IsSub);
                GenerateCodeHelper.WriteFile(Path.Combine(solutionPath, _formModel.DataPath, "Repositories", "Interface"), $"I{_formModel.TableName}Repository.cs", content);

                template = GenerateCodeHelper.GetTemplate(ETemplateType.BaseRepository);
                content = GenerateContent(template, _formModel.IsSub);
                GenerateCodeHelper.WriteFile(Path.Combine(solutionPath, _formModel.DataPath, "Repositories", "Implement"), $"{_formModel.TableName}Repository.cs", content);
            }
            else
            {
                template = GenerateCodeHelper.GetTemplate(ETemplateType.ICrudRepository);
                content = GenerateContent(template, _formModel.IsSub);
                GenerateCodeHelper.WriteFile(Path.Combine(solutionPath, _formModel.DataPath, "Repositories", "Interface"), $"I{_formModel.TableName}Repository.cs", content);

                template = GenerateCodeHelper.GetTemplate(ETemplateType.CrudRepository);
                content = GenerateContent(template, _formModel.IsSub);
                GenerateCodeHelper.WriteFile(Path.Combine(solutionPath, _formModel.DataPath, "Repositories", "Implement"), $"{_formModel.TableName}Repository.cs", content);
            }

            VsShellUtilities.ShowMessageBox(
                this.package,
                "模板產生成功",
                "",
                OLEMSGICON.OLEMSGICON_INFO,
                OLEMSGBUTTON.OLEMSGBUTTON_OK,
                OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
        }

        /// <summary>
        /// 產生模板檔案
        /// </summary>
        /// <param name="template">範本內容</param>
        /// <param name="isSub">產生獨立查詢方法</param>
        /// <returns></returns>
        private string GenerateContent(string template, bool isSub = true)
        {
            string pattern = @"^(?<indent>[ \t]*)@if\s+isSub\s*\r?\n" +   // 取得整行前面縮排
                             @"(?<ifblock>[\s\S]*?)" +
                             @"^[ \t]*@else\s*\r?\n" +
                             @"(?<elseblock>[\s\S]*?)" +
                             @"^[ \t]*@endif\s*\r?\n?";

            template = Regex.Replace(template, pattern, match =>
            {
                string ifBlock = match.Groups["ifblock"].Value;
                string elseBlock = match.Groups["elseblock"].Value;

                string selectedBlock = isSub ? ifBlock : elseBlock;
                return selectedBlock + "\t";
            }, RegexOptions.Multiline);

            return template
                    .Replace("@apiPath", _formModel.ApiPath)
                    .Replace("@corePath", _formModel.CorePath)
                    .Replace("@dataPath", _formModel.DataPath)
                    .Replace("@contextName", _formModel.ContextName)
                    .Replace("@tableName", _formModel.TableName)
                    .Replace("@queryModel", _formModel.QueryModel)
                    .Replace("@listModel", _formModel.ListModel)
                    .Replace("@viewModel", _formModel.ViewModel);
        }
    }
}
