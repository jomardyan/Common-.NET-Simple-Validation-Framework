using System;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Linq;

namespace ValidationFramework
{
    public class ValidationResult
    {
        public bool IsValid { get; set; }
        public List<string> Errors { get; } = new List<string>();
        public Dictionary<string, List<string>> FieldErrors { get; } = new Dictionary<string, List<string>>();

        public void AddFieldError(string fieldName, string error)
        {
            if (!FieldErrors.ContainsKey(fieldName))
            {
                FieldErrors[fieldName] = new List<string>();
            }
            FieldErrors[fieldName].Add(error);
            Errors.Add($"{fieldName}: {error}");
        }

        public void AddError(string error)
        {
            if (!string.IsNullOrWhiteSpace(error))
            {
                Errors.Add(error);
            }
        }

        public IReadOnlyList<string> GetFieldErrors(string fieldName)
        {
            return FieldErrors.TryGetValue(fieldName, out var errors)
                ? errors
                : Array.Empty<string>();
        }

        public string? GetFirstFieldError(string fieldName)
        {
            return FieldErrors.TryGetValue(fieldName, out var errors) && errors.Count > 0
                ? errors[0]
                : null;
        }

        public Dictionary<string, string[]> ToErrorDictionary()
        {
            return FieldErrors.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.ToArray());
        }
    }

    public interface IValidator<T>
    {
        ValidationResult Validate(T value);
    }

    public class ValidationRule<T>
    {
        private readonly Func<T, bool> _rule;
        private readonly string _errorMessage;

        public ValidationRule(Func<T, bool> rule, string errorMessage)
        {
            _rule = rule;
            _errorMessage = errorMessage;
        }

        public (bool isValid, string error) Validate(T value)
        {
            return (_rule(value), _errorMessage);
        }
    }

    public class Validator<T> : IValidator<T>
    {
        private readonly List<ValidationRule<T>> _rules = new List<ValidationRule<T>>();
        private readonly string? _fieldName;

        public Validator(string? fieldName = null)
        {
            _fieldName = fieldName;
        }

        public Validator<T> AddRule(Func<T, bool> rule, string errorMessage)
        {
            _rules.Add(new ValidationRule<T>(rule, errorMessage));
            return this;
        }

        public ValidationResult Validate(T value)
        {
            var result = new ValidationResult { IsValid = true };

            foreach (var rule in _rules)
            {
                var (isValid, error) = rule.Validate(value);
                if (!isValid)
                {
                    result.IsValid = false;
                    if (!string.IsNullOrEmpty(_fieldName))
                    {
                        result.AddFieldError(_fieldName, error);
                    }
                    else
                    {
                        result.Errors.Add(error);
                    }
                }
            }

            return result;
        }
    }

    public static class ValidationExtensions
    {
        public static ValidationResult Merge(this ValidationResult first, ValidationResult second)
        {
            var result = new ValidationResult { IsValid = first.IsValid && second.IsValid };
            result.Errors.AddRange(first.Errors);
            result.Errors.AddRange(second.Errors);
            
            foreach (var kvp in first.FieldErrors.Concat(second.FieldErrors))
            {
                if (!result.FieldErrors.ContainsKey(kvp.Key))
                {
                    result.FieldErrors[kvp.Key] = new List<string>();
                }
                result.FieldErrors[kvp.Key].AddRange(kvp.Value);
            }
            
            return result;
        }

        public static ValidationResult MergeAll(this IEnumerable<ValidationResult> results)
        {
            if (results == null)
            {
                throw new ArgumentNullException(nameof(results));
            }

            var merged = new ValidationResult { IsValid = true };

            foreach (var result in results)
            {
                merged = merged.Merge(result);
            }

            return merged;
        }
    }

    public static class CommonValidators
    {
        public static Validator<string> RequiredValidator(string fieldName = "Value")
        {
            return new Validator<string>(fieldName)
                .AddRule(value => !string.IsNullOrWhiteSpace(value), $"{fieldName} is required");
        }

        public static Validator<string> LengthValidator(
            int minLength = 0,
            int? maxLength = null,
            string fieldName = "Value")
        {
            var validator = new Validator<string>(fieldName);

            if (minLength > 0)
            {
                validator.AddRule(value => !string.IsNullOrWhiteSpace(value), $"{fieldName} is required");
                validator.AddRule(value => string.IsNullOrWhiteSpace(value) || value.Length >= minLength,
                    $"{fieldName} must be at least {minLength} characters long");
            }

            if (maxLength.HasValue)
            {
                validator.AddRule(value => string.IsNullOrWhiteSpace(value) || value.Length <= maxLength.Value,
                    $"{fieldName} cannot exceed {maxLength.Value} characters");
            }

            return validator;
        }

        public static Validator<string> RegexValidator(
            string pattern,
            string errorMessage,
            string fieldName = "Value")
        {
            var regex = new Regex(pattern, RegexOptions.Compiled | RegexOptions.CultureInvariant);
            return new Validator<string>(fieldName)
                .AddRule(value => string.IsNullOrWhiteSpace(value) || regex.IsMatch(value), errorMessage);
        }

        public static Validator<T> RangeValidator<T>(
            T? minValue = null,
            T? maxValue = null,
            string fieldName = "Value")
            where T : struct, IComparable<T>
        {
            var validator = new Validator<T>(fieldName);

            if (minValue.HasValue)
            {
                validator.AddRule(value => value.CompareTo(minValue.Value) >= 0,
                    $"{fieldName} must be at least {minValue.Value}");
            }

            if (maxValue.HasValue)
            {
                validator.AddRule(value => value.CompareTo(maxValue.Value) <= 0,
                    $"{fieldName} cannot exceed {maxValue.Value}");
            }

            return validator;
        }

        public static Validator<string> EmailValidator(string fieldName = "Email")
        {
            return new Validator<string>(fieldName)
                .AddRule(email => !string.IsNullOrWhiteSpace(email), "Email is required")
                .AddRule(email => string.IsNullOrWhiteSpace(email)
                    || Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"),
                    "Invalid email format");
        }

        public static Validator<string> NameValidator(int minLength = 2, int maxLength = 50, string fieldName = "Name")
        {
            return new Validator<string>(fieldName)
                .AddRule(name => !string.IsNullOrWhiteSpace(name), "Name is required")
                .AddRule(name => string.IsNullOrWhiteSpace(name) || name.Length >= minLength,
                    $"Name must be at least {minLength} characters long")
                .AddRule(name => string.IsNullOrWhiteSpace(name) || name.Length <= maxLength,
                    $"Name cannot exceed {maxLength} characters")
                .AddRule(name => string.IsNullOrWhiteSpace(name)
                    || Regex.IsMatch(name, @"^[a-zA-Z\s-']+$"),
                    "Name can only contain letters, spaces, hyphens, and apostrophes");
        }

        public static Validator<string> PhoneValidator(int requiredLength = 10, string fieldName = "Phone")
        {
            return new Validator<string>(fieldName)
                .AddRule(phone => !string.IsNullOrWhiteSpace(phone), "Phone number is required")
                .AddRule(phone => string.IsNullOrWhiteSpace(phone) || Regex.IsMatch(phone, @"^\d+$"),
                    "Phone number can only contain digits")
                .AddRule(phone => string.IsNullOrWhiteSpace(phone) || phone.Length == requiredLength,
                    $"Phone number must be exactly {requiredLength} digits");
        }

        public static Validator<string> PasswordValidator(string fieldName = "Password")
        {
            return new Validator<string>(fieldName)
                .AddRule(pwd => !string.IsNullOrWhiteSpace(pwd), "Password is required")
                .AddRule(pwd => string.IsNullOrWhiteSpace(pwd) || pwd.Length >= 8,
                    "Password must be at least 8 characters long")
                .AddRule(pwd => string.IsNullOrWhiteSpace(pwd) || Regex.IsMatch(pwd, "[A-Z]"),
                    "Password must contain at least one uppercase letter")
                .AddRule(pwd => string.IsNullOrWhiteSpace(pwd) || Regex.IsMatch(pwd, "[a-z]"),
                    "Password must contain at least one lowercase letter")
                .AddRule(pwd => string.IsNullOrWhiteSpace(pwd) || Regex.IsMatch(pwd, "[0-9]"),
                    "Password must contain at least one number")
                .AddRule(pwd => string.IsNullOrWhiteSpace(pwd) || Regex.IsMatch(pwd, "[^a-zA-Z0-9]"),
                    "Password must contain at least one special character");
        }

        public static Validator<string> UrlValidator(string fieldName = "URL")
        {
            return new Validator<string>(fieldName)
                .AddRule(url => !string.IsNullOrWhiteSpace(url), "URL is required")
                .AddRule(url => string.IsNullOrWhiteSpace(url) || Uri.TryCreate(url, UriKind.Absolute, out _),
                    "Invalid URL format");
        }

        public static Validator<DateTime> DateValidator(
            DateTime? minDate = null, 
            DateTime? maxDate = null, 
            string fieldName = "Date")
        {
            var validator = new Validator<DateTime>(fieldName);
            
            if (minDate.HasValue)
            {
                validator.AddRule(date => date >= minDate.Value, 
                    $"Date must be on or after {minDate.Value:d}");
            }
            
            if (maxDate.HasValue)
            {
                validator.AddRule(date => date <= maxDate.Value, 
                    $"Date must be on or before {maxDate.Value:d}");
            }
            
            return validator;
        }

        public static Validator<decimal> CurrencyValidator(
            decimal? minValue = null,
            decimal? maxValue = null,
            string fieldName = "Amount")
        {
            var validator = new Validator<decimal>(fieldName)
                .AddRule(amount => amount >= 0, "Amount cannot be negative")
                .AddRule(amount => decimal.Round(amount, 2) == amount, "Amount cannot have more than 2 decimal places");

            if (minValue.HasValue)
            {
                validator.AddRule(amount => amount >= minValue.Value,
                    $"Amount must be at least {minValue.Value:C}");
            }

            if (maxValue.HasValue)
            {
                validator.AddRule(amount => amount <= maxValue.Value,
                    $"Amount cannot exceed {maxValue.Value:C}");
            }

            return validator;
        }
    }

    public class ValidationBuilder<T> where T : class
    {
        private readonly Dictionary<string, Func<T, ValidationResult>> _validations 
            = new Dictionary<string, Func<T, ValidationResult>>();

        public ValidationBuilder<T> AddValidation(string propertyName, Func<T, ValidationResult> validation)
        {
            _validations[propertyName] = validation;
            return this;
        }

        public ValidationResult Validate(T entity)
        {
            var result = new ValidationResult { IsValid = true };

            foreach (var validation in _validations)
            {
                var propertyResult = validation.Value(entity);
                if (propertyResult.Errors.Count > 0 && propertyResult.FieldErrors.Count == 0)
                {
                    var errors = propertyResult.Errors.ToArray();
                    propertyResult.Errors.Clear();
                    foreach (var error in errors)
                    {
                        propertyResult.AddFieldError(validation.Key, error);
                    }
                }
                result = result.Merge(propertyResult);
            }

            return result;
        }
    }

    // Example model class with additional fields
    public class UserProfile
    {
        public string Email { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string Website { get; set; } = string.Empty;
        public DateTime DateOfBirth { get; set; }
        public decimal Salary { get; set; }
    }

    public class UserProfileValidator
    {
        private readonly ValidationBuilder<UserProfile> _validator;

        public UserProfileValidator()
        {
            _validator = new ValidationBuilder<UserProfile>()
                .AddValidation("Email", p => CommonValidators.EmailValidator().Validate(p.Email))
                .AddValidation("FirstName", p => CommonValidators.NameValidator(fieldName: "FirstName").Validate(p.FirstName))
                .AddValidation("LastName", p => CommonValidators.NameValidator(fieldName: "LastName").Validate(p.LastName))
                .AddValidation("PhoneNumber", p => CommonValidators.PhoneValidator().Validate(p.PhoneNumber))
                .AddValidation("Password", p => CommonValidators.PasswordValidator().Validate(p.Password))
                .AddValidation("Website", p => CommonValidators.UrlValidator().Validate(p.Website))
                .AddValidation("DateOfBirth", p => CommonValidators.DateValidator(
                    minDate: DateTime.Now.AddYears(-120),
                    maxDate: DateTime.Now.AddYears(-18),
                    fieldName: "DateOfBirth").Validate(p.DateOfBirth))
                .AddValidation("Salary", p => CommonValidators.CurrencyValidator(
                    minValue: 0,
                    maxValue: 1000000,
                    fieldName: "Salary").Validate(p.Salary));
        }

        public ValidationResult ValidateProfile(UserProfile profile)
        {
            return _validator.Validate(profile);
        }
    }
}
