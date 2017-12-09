using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace Calendar
{
    public class FieldValidation : ValidationRule
    {
        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            if (value == null || !(value is string)) return new ValidationResult(false, "Wrong input");
            string input = (string)value;
            Regex r = new Regex("^[0-9]*$");
            if (!(r.IsMatch(input)))
                return new ValidationResult(false, "Input should contain only numbers");
            return new ValidationResult(true, null);
        }
    }
}
