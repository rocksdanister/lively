using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Controls;

namespace livelywpf.Helpers.MVVM
{
    class OnlyDigitsValidationRule : ValidationRule
    {
        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            var validationResult = new ValidationResult(true, null);

            if (value != null)
            {
                if (!string.IsNullOrEmpty(value.ToString()))
                {
                    var regex = new Regex("[^0-9.-]+"); //regex that matches disallowed text
                    var parsingOk = !regex.IsMatch(value.ToString());
                    if (!parsingOk)
                    {
                        validationResult = new ValidationResult(false, "Illegal Characters, Please Enter Numeric Value");
                    }
                }
            }

            return validationResult;
        }
    }
}