using MenuVSIX.Helper;
using Microsoft.VisualStudio.Shell;
using Newtonsoft.Json;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace MenuVSIX
{
    /// <summary>
    /// InputWindow.xaml 的互動邏輯
    /// </summary>
    public partial class InputWindow : Window
    {
        private readonly string _collectionName = "MenuVSIX.All";

        public FormModel Model { get; set; }


        public InputWindow()
        {
            InitializeComponent();

            ThreadHelper.ThrowIfNotOnUIThread();
            Model = SettingHelper.LoadObject(_collectionName, "FormModel", new FormModel());
            Model.IsView = false;
            Model.IsSub = true;
            Model.IsController = true;
            Model.IsService = true;
            this.DataContext = Model;
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            bool isValid = ValidateAllFields(this);

            if (!isValid)
            {
                MessageBox.Show("請填寫所有必填欄位");
                return;
            }

            SettingHelper.SaveObject(_collectionName, "FormModel", Model);

            DialogResult = true;
            Close();
        }

        /// <summary>
        /// 驗證必填欄位
        /// </summary>
        /// <param name="parent"></param>
        /// <returns></returns>
        private bool ValidateAllFields(DependencyObject parent)
        {
            bool isValid = true;

            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);

                if (child is TextBox tb)
                {
                    var be = tb.GetBindingExpression(TextBox.TextProperty);
                    if (be != null)
                    {
                        be.UpdateSource(); // 觸發驗證
                        if (Validation.GetHasError(tb))
                        {
                            isValid = false;
                        }
                    }
                }
                else
                {
                    if (!ValidateAllFields(child))
                    {
                        isValid = false;
                    }
                }
            }

            return isValid;
        }
    }

    public class FormModel : INotifyPropertyChanged, IDataErrorInfo
    {
        private bool _updating = false;

        private string _apiPath;
        public string ApiPath
        {
            get => _apiPath;
            set { _apiPath = value; OnPropertyChanged(nameof(ApiPath)); }
        }

        private string _corePath;
        public string CorePath
        {
            get => _corePath;
            set { _corePath = value; OnPropertyChanged(nameof(CorePath)); }
        }

        private string _dataPath;
        public string DataPath
        {
            get => _dataPath;
            set { _dataPath = value; OnPropertyChanged(nameof(DataPath)); }
        }

        private string _contextName;
        public string ContextName
        {
            get => _contextName;
            set { _contextName = value; OnPropertyChanged(nameof(ContextName)); }
        }

        private string _tableName;
        [JsonIgnore]
        public string TableName
        {
            get => _tableName;
            set { 
                _tableName = value; 
                OnPropertyChanged(nameof(TableName));

                QueryModel = value + "QueryModel";

                ListModel = value + "ListModel";

                ViewModel = value + "ViewModel";
            }
        }

        private string _queryModel;
        [JsonIgnore]
        public string QueryModel
        {
            get => _queryModel;
            set { _queryModel = value; OnPropertyChanged(nameof(QueryModel)); }
        }

        private string _listModel;
        [JsonIgnore]
        public string ListModel
        {
            get => _listModel;
            set { _listModel = value; OnPropertyChanged(nameof(ListModel)); }
        }

        private string _viewModel;
        [JsonIgnore]
        public string ViewModel
        {
            get => _viewModel;
            set { _viewModel = value; OnPropertyChanged(nameof(ViewModel)); }
        }

        private bool _isRecord;
        public bool IsRecord
        {
            get => _isRecord;
            set { _isRecord = value; OnPropertyChanged(nameof(IsRecord)); }
        }

        private bool _isView;
        [JsonIgnore]
        public bool IsView
        {
            get => _isView;
            set { _isView = value; OnPropertyChanged(nameof(IsView)); }
        }

        private bool _isSub;
        [JsonIgnore]
        public bool IsSub
        {
            get => _isSub;
            set { 
                _isSub = value; 
                OnPropertyChanged(nameof(IsSub));

                if (_updating) return;

                _updating = true;
                if (!value)
                {
                    IsService = false;
                    IsController = false;
                }
                _updating = false;
            }
        }

        private bool _isController;
        [JsonIgnore]
        public bool IsController
        {
            get => _isController;
            set { 
                _isController = value; 
                OnPropertyChanged(nameof(IsController));

                if (_updating) return;

                _updating = true;
                
                if (value)
                {
                    IsSub = true;
                    IsService = true;
                }
                _updating = false;
            }
        }

        private bool _isService;
        [JsonIgnore]
        public bool IsService
        {
            get => _isService;
            set { 
                _isService = value; 
                OnPropertyChanged(nameof(IsService));

                if (_updating) return;

                _updating = true;
                if (!value) IsController = false;
                if (value) IsSub = true;
                _updating = false;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string prop) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));

        private static readonly string[] _requiredFields = new[]
        {
            nameof(ApiPath),
            nameof(CorePath),
            nameof(DataPath),
            nameof(ContextName),
            nameof(TableName),
            //nameof(QueryModel),
            //nameof(ListModel),
            //nameof(ViewModel)
        };

        public string this[string columnName]
        {
            get
            {
                if (_requiredFields.Contains(columnName))
                {
                    var property = GetType().GetProperty(columnName);
                    var value = property?.GetValue(this) as string;
                    if (string.IsNullOrWhiteSpace(value))
                        return "必填";
                }
                return null;
            }
        }

        public string Error => null;
    }
}
