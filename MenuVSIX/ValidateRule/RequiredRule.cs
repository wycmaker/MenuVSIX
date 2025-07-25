using System.Globalization;
using System.Windows.Controls;

namespace MenuVSIX.ValidateRule
{
    public class RequiredRule : ValidationRule
    {
        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            if (value == null || string.IsNullOrWhiteSpace(value.ToString()))
            {
                return new ValidationResult(false, "此欄位為必填");
            }

            return ValidationResult.ValidResult;
        }
    }
}
