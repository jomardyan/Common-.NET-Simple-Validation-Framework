using System;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace ValidationFramework
{
    /// <summary>
    /// Represents the result of a validation operation, including success status and error details.
    /// </summary>
    public class ValidationResult
    {
        /// <summary>
        /// Gets or sets a value indicating whether the validation was successful.
        /// </summary>
        public bool IsValid { get; set; }

        /// <summary>
        /// Gets the list of all validation errors.
        /// </summary>
        public List<string> Errors { get; } = new List<string>();

        /// <summary>
        /// Gets the dictionary of field-specific validation errors.
        /// </summary>
        public Dictionary<string, List<string>> FieldErrors { get; } = new Dictionary<string, List<string>>();

        /// <summary>
        /// Adds an error message associated with a specific field.
        /// </summary>
        /// <param name="fieldName">The name of the field with the error.</param>
        /// <param name="error">The error message.</param>
        public void AddFieldError(string fieldName, string error)
        {
            if (!FieldErrors.ContainsKey(fieldName))
            {
                FieldErrors[fieldName] = new List<string>();
            }
            FieldErrors[fieldName].Add(error);
            Errors.Add($"{fieldName}: {error}");
        }

        /// <summary>
        /// Adds a general validation error message.
        /// </summary>
        /// <param name="error">The error message to add.</param>
        public void AddError(string error)
        {
            if (!string.IsNullOrWhiteSpace(error))
            {
                Errors.Add(error);
            }
        }

        /// <summary>
        /// Retrieves all error messages for a specific field.
        /// </summary>
        /// <param name="fieldName">The name of the field.</param>
        /// <returns>A read-only list of error messages for the field, or an empty list if none exist.</returns>
        public IReadOnlyList<string> GetFieldErrors(string fieldName)
        {
            return FieldErrors.TryGetValue(fieldName, out var errors)
                ? errors
                : Array.Empty<string>();
        }

        /// <summary>
        /// Retrieves the first error message for a specific field.
        /// </summary>
        /// <param name="fieldName">The name of the field.</param>
        /// <returns>The first error message for the field, or null if none exist.</returns>
        public string? GetFirstFieldError(string fieldName)
        {
            return FieldErrors.TryGetValue(fieldName, out var errors) && errors.Count > 0
                ? errors[0]
                : null;
        }

        /// <summary>
        /// Converts field errors to a dictionary format suitable for model state or other UI frameworks.
        /// </summary>
        /// <returns>A dictionary mapping field names to arrays of error messages.</returns>
        public Dictionary<string, string[]> ToErrorDictionary()
        {
            return FieldErrors.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.ToArray());
        }
    }

    /// <summary>
    /// Defines a contract for validators that can validate values of type <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The type of value to validate.</typeparam>
    public interface IValidator<T>
    {
        /// <summary>
        /// Validates the specified value.
        /// </summary>
        /// <param name="value">The value to validate.</param>
        /// <returns>A <see cref="ValidationResult"/> indicating whether the validation succeeded.</returns>
        ValidationResult Validate(T value);
    }

    /// <summary>
    /// Represents a single validation rule with a condition and error message.
    /// </summary>
    /// <typeparam name="T">The type of value to validate.</typeparam>
    public class ValidationRule<T>
    {
        private readonly Func<T, bool> _rule;
        private readonly string _errorMessage;

        /// <summary>
        /// Initializes a new instance of the <see cref="ValidationRule{T}"/> class.
        /// </summary>
        /// <param name="rule">The function that defines the validation condition.</param>
        /// <param name="errorMessage">The error message to return if validation fails.</param>
        public ValidationRule(Func<T, bool> rule, string errorMessage)
        {
            _rule = rule;
            _errorMessage = errorMessage;
        }

        /// <summary>
        /// Validates the specified value against this rule.
        /// </summary>
        /// <param name="value">The value to validate.</param>
        /// <returns>A tuple containing the validation result and error message.</returns>
        public (bool isValid, string error) Validate(T value)
        {
            return (_rule(value), _errorMessage);
        }
    }

    /// <summary>
    /// A generic validator that supports fluent API for building validation rules.
    /// </summary>
    /// <typeparam name="T">The type of value to validate.</typeparam>
    public class Validator<T> : IValidator<T>
    {
        private readonly List<ValidationRule<T>> _rules = new List<ValidationRule<T>>();
        private readonly string? _fieldName;

        /// <summary>
        /// Initializes a new instance of the <see cref="Validator{T}"/> class.
        /// </summary>
        /// <param name="fieldName">Optional field name for error reporting.</param>
        public Validator(string? fieldName = null)
        {
            _fieldName = fieldName;
        }

        /// <summary>
        /// Adds a validation rule to this validator.
        /// </summary>
        /// <param name="rule">The function that defines the validation condition.</param>
        /// <param name="errorMessage">The error message to return if validation fails.</param>
        /// <returns>This validator instance for method chaining.</returns>
        public Validator<T> AddRule(Func<T, bool> rule, string errorMessage)
        {
            _rules.Add(new ValidationRule<T>(rule, errorMessage));
            return this;
        }

        /// <summary>
        /// Validates the specified value against all rules in this validator.
        /// </summary>
        /// <param name="value">The value to validate.</param>
        /// <returns>A <see cref="ValidationResult"/> containing all validation errors.</returns>
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

    /// <summary>
    /// Provides extension methods for working with validation results.
    /// </summary>
    public static class ValidationExtensions
    {
        /// <summary>
        /// Merges two validation results into a single result.
        /// </summary>
        /// <param name="first">The first validation result.</param>
        /// <param name="second">The second validation result.</param>
        /// <returns>A merged <see cref="ValidationResult"/> containing errors from both results.</returns>
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

        /// <summary>
        /// Merges multiple validation results into a single result.
        /// </summary>
        /// <param name="results">The collection of validation results to merge.</param>
        /// <returns>A merged <see cref="ValidationResult"/> containing errors from all results.</returns>
        /// <exception cref="ArgumentNullException">Thrown when results is null.</exception>
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

    /// <summary>
    /// Provides pre-built validators for common validation scenarios.
    /// </summary>
    public static class CommonValidators
    {
        /// <summary>
        /// Creates a validator that ensures a string value is not null, empty, or whitespace.
        /// </summary>
        /// <param name="fieldName">The name of the field being validated.</param>
        /// <returns>A configured <see cref="Validator{String}"/> instance.</returns>
        public static Validator<string> RequiredValidator(string fieldName = "Value")
        {
            return new Validator<string>(fieldName)
                .AddRule(value => !string.IsNullOrWhiteSpace(value), $"{fieldName} is required");
        }

        /// <summary>
        /// Creates a validator that checks string length constraints.
        /// </summary>
        /// <param name="minLength">The minimum required length. Values with length less than this will fail validation.</param>
        /// <param name="maxLength">The maximum allowed length. If null, no maximum is enforced.</param>
        /// <param name="fieldName">The name of the field being validated.</param>
        /// <returns>A configured <see cref="Validator{String}"/> instance.</returns>
        public static Validator<string> LengthValidator(
            int minLength = 0,
            int? maxLength = null,
            string fieldName = "Value")
        {
            var validator = new Validator<string>(fieldName);

            if (minLength > 0)
            {
                validator.AddRule(value => !string.IsNullOrWhiteSpace(value) && value.Length >= minLength,
                    $"{fieldName} must be at least {minLength} characters long");
            }

            if (maxLength.HasValue)
            {
                validator.AddRule(value => string.IsNullOrWhiteSpace(value) || value.Length <= maxLength.Value,
                    $"{fieldName} cannot exceed {maxLength.Value} characters");
            }

            return validator;
        }

        /// <summary>
        /// Creates a validator that matches strings against a regular expression pattern.
        /// Empty or whitespace values are considered valid and skip pattern matching.
        /// </summary>
        /// <param name="pattern">The regular expression pattern to match.</param>
        /// <param name="errorMessage">The error message to display if validation fails.</param>
        /// <param name="fieldName">The name of the field being validated.</param>
        /// <returns>A configured <see cref="Validator{String}"/> instance.</returns>
        public static Validator<string> RegexValidator(
            string pattern,
            string errorMessage,
            string fieldName = "Value")
        {
            return new Validator<string>(fieldName)
                .AddRule(value => string.IsNullOrWhiteSpace(value)
                    || Regex.IsMatch(value, pattern, RegexOptions.CultureInvariant),
                    errorMessage);
        }

        /// <summary>
        /// Creates a validator that checks if a value falls within a specified range.
        /// </summary>
        /// <typeparam name="T">The type of value to validate (must be comparable).</typeparam>
        /// <param name="minValue">The minimum allowed value. If null, no minimum is enforced.</param>
        /// <param name="maxValue">The maximum allowed value. If null, no maximum is enforced.</param>
        /// <param name="fieldName">The name of the field being validated.</param>
        /// <returns>A configured <see cref="Validator{T}"/> instance.</returns>
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

        /// <summary>
        /// Creates a validator that validates email addresses using a basic regex pattern.
        /// </summary>
        /// <param name="fieldName">The name of the field being validated.</param>
        /// <returns>A configured <see cref="Validator{String}"/> instance.</returns>
        public static Validator<string> EmailValidator(string fieldName = "Email")
        {
            return new Validator<string>(fieldName)
                .AddRule(email => !string.IsNullOrWhiteSpace(email), "Email is required")
                .AddRule(email => string.IsNullOrWhiteSpace(email)
                    || Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"),
                    "Invalid email format");
        }

        /// <summary>
        /// Creates a validator for names with length and character constraints.
        /// Allows letters, spaces, hyphens, and apostrophes.
        /// </summary>
        /// <param name="minLength">The minimum required length.</param>
        /// <param name="maxLength">The maximum allowed length.</param>
        /// <param name="fieldName">The name of the field being validated.</param>
        /// <returns>A configured <see cref="Validator{String}"/> instance.</returns>
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

        /// <summary>
        /// Creates a validator for phone numbers that must contain only digits and have an exact length.
        /// </summary>
        /// <param name="requiredLength">The exact number of digits required.</param>
        /// <param name="fieldName">The name of the field being validated.</param>
        /// <returns>A configured <see cref="Validator{String}"/> instance.</returns>
        public static Validator<string> PhoneValidator(int requiredLength = 10, string fieldName = "Phone")
        {
            return new Validator<string>(fieldName)
                .AddRule(phone => !string.IsNullOrWhiteSpace(phone), "Phone number is required")
                .AddRule(phone => string.IsNullOrWhiteSpace(phone) || Regex.IsMatch(phone, @"^\d+$"),
                    "Phone number can only contain digits")
                .AddRule(phone => string.IsNullOrWhiteSpace(phone) || phone.Length == requiredLength,
                    $"Phone number must be exactly {requiredLength} digits");
        }

        /// <summary>
        /// Creates a validator for passwords with strength requirements.
        /// Requires at least 8 characters, one uppercase, one lowercase, one digit, and one special character.
        /// </summary>
        /// <param name="fieldName">The name of the field being validated.</param>
        /// <returns>A configured <see cref="Validator{String}"/> instance.</returns>
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

        /// <summary>
        /// Creates a validator for URLs using Uri.TryCreate for validation.
        /// </summary>
        /// <param name="fieldName">The name of the field being validated.</param>
        /// <returns>A configured <see cref="Validator{String}"/> instance.</returns>
        public static Validator<string> UrlValidator(string fieldName = "URL")
        {
            return new Validator<string>(fieldName)
                .AddRule(url => !string.IsNullOrWhiteSpace(url), "URL is required")
                .AddRule(url => string.IsNullOrWhiteSpace(url) || Uri.TryCreate(url, UriKind.Absolute, out _),
                    "Invalid URL format");
        }

        /// <summary>
        /// Creates a validator for dates with optional min and max date constraints.
        /// </summary>
        /// <param name="minDate">The minimum allowed date. If null, no minimum is enforced.</param>
        /// <param name="maxDate">The maximum allowed date. If null, no maximum is enforced.</param>
        /// <param name="fieldName">The name of the field being validated.</param>
        /// <returns>A configured <see cref="Validator{DateTime}"/> instance.</returns>
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

        /// <summary>
        /// Creates a validator for currency amounts with range and decimal place constraints.
        /// Ensures non-negative values with maximum 2 decimal places.
        /// </summary>
        /// <param name="minValue">The minimum allowed value. If null, only non-negative constraint applies.</param>
        /// <param name="maxValue">The maximum allowed value. If null, no maximum is enforced.</param>
        /// <param name="fieldName">The name of the field being validated.</param>
        /// <returns>A configured <see cref="Validator{Decimal}"/> instance.</returns>
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

        /// <summary>
        /// Creates a validator for US ZIP codes (5-digit or 9-digit format).
        /// Supports formats: 12345 or 12345-6789
        /// </summary>
        /// <param name="fieldName">The name of the field being validated.</param>
        /// <returns>A configured <see cref="Validator{String}"/> instance.</returns>
        public static Validator<string> ZipCodeValidator(string fieldName = "ZipCode")
        {
            return new Validator<string>(fieldName)
                .AddRule(zip => !string.IsNullOrWhiteSpace(zip), "ZIP code is required")
                .AddRule(zip => string.IsNullOrWhiteSpace(zip)
                    || Regex.IsMatch(zip, @"^\d{5}(-\d{4})?$", RegexOptions.CultureInvariant),
                    "ZIP code must be in format 12345 or 12345-6789");
        }

        /// <summary>
        /// Creates a validator for alphanumeric strings (letters and numbers only).
        /// </summary>
        /// <param name="minLength">The minimum required length.</param>
        /// <param name="maxLength">The maximum allowed length. If null, no maximum is enforced.</param>
        /// <param name="fieldName">The name of the field being validated.</param>
        /// <returns>A configured <see cref="Validator{String}"/> instance.</returns>
        public static Validator<string> AlphanumericValidator(
            int minLength = 1,
            int? maxLength = null,
            string fieldName = "Value")
        {
            var validator = new Validator<string>(fieldName)
                .AddRule(value => !string.IsNullOrWhiteSpace(value), $"{fieldName} is required")
                .AddRule(value => string.IsNullOrWhiteSpace(value)
                    || Regex.IsMatch(value, @"^[a-zA-Z0-9]+$", RegexOptions.CultureInvariant),
                    $"{fieldName} can only contain letters and numbers");

            if (minLength > 0)
            {
                validator.AddRule(value => !string.IsNullOrWhiteSpace(value) && value.Length >= minLength,
                    $"{fieldName} must be at least {minLength} characters long");
            }

            if (maxLength.HasValue)
            {
                validator.AddRule(value => string.IsNullOrWhiteSpace(value) || value.Length <= maxLength.Value,
                    $"{fieldName} cannot exceed {maxLength.Value} characters");
            }

            return validator;
        }

        /// <summary>
        /// Creates a validator for IPv4 addresses.
        /// </summary>
        /// <param name="fieldName">The name of the field being validated.</param>
        /// <returns>A configured <see cref="Validator{String}"/> instance.</returns>
        public static Validator<string> IpAddressValidator(string fieldName = "IpAddress")
        {
            return new Validator<string>(fieldName)
                .AddRule(ip => !string.IsNullOrWhiteSpace(ip), "IP address is required")
                .AddRule(ip => string.IsNullOrWhiteSpace(ip)
                    || System.Net.IPAddress.TryParse(ip, out _),
                    "Invalid IP address format");
        }

        /// <summary>
        /// Creates a validator for usernames with common constraints.
        /// Allows letters, numbers, underscores, and hyphens. Must start with a letter.
        /// </summary>
        /// <param name="minLength">The minimum required length.</param>
        /// <param name="maxLength">The maximum allowed length.</param>
        /// <param name="fieldName">The name of the field being validated.</param>
        /// <returns>A configured <see cref="Validator{String}"/> instance.</returns>
        public static Validator<string> UsernameValidator(
            int minLength = 3,
            int maxLength = 20,
            string fieldName = "Username")
        {
            return new Validator<string>(fieldName)
                .AddRule(username => !string.IsNullOrWhiteSpace(username), "Username is required")
                .AddRule(username => string.IsNullOrWhiteSpace(username) || username.Length >= minLength,
                    $"Username must be at least {minLength} characters long")
                .AddRule(username => string.IsNullOrWhiteSpace(username) || username.Length <= maxLength,
                    $"Username cannot exceed {maxLength} characters")
                .AddRule(username => string.IsNullOrWhiteSpace(username)
                    || Regex.IsMatch(username, @"^[a-zA-Z][a-zA-Z0-9_-]*$", RegexOptions.CultureInvariant),
                    "Username must start with a letter and can only contain letters, numbers, underscores, and hyphens");
        }

        /// <summary>
        /// Creates a validator for credit card numbers using the Luhn algorithm.
        /// Validates card number format and checksum.
        /// </summary>
        /// <param name="fieldName">The name of the field being validated.</param>
        /// <returns>A configured <see cref="Validator{String}"/> instance.</returns>
        public static Validator<string> CreditCardValidator(string fieldName = "CreditCard")
        {
            return new Validator<string>(fieldName)
                .AddRule(card => !string.IsNullOrWhiteSpace(card), "Credit card number is required")
                .AddRule(card => string.IsNullOrWhiteSpace(card)
                    || Regex.IsMatch(card.Replace(" ", "").Replace("-", ""), @"^\d{13,19}$", RegexOptions.CultureInvariant),
                    "Credit card number must be 13-19 digits")
                .AddRule(card => string.IsNullOrWhiteSpace(card) || IsValidLuhn(card.Replace(" ", "").Replace("-", "")),
                    "Invalid credit card number");
        }

        /// <summary>
        /// Creates a validator for Canadian postal codes (format: A1A 1A1).
        /// </summary>
        /// <param name="fieldName">The name of the field being validated.</param>
        /// <returns>A configured <see cref="Validator{String}"/> instance.</returns>
        public static Validator<string> PostalCodeValidator(string fieldName = "PostalCode")
        {
            return new Validator<string>(fieldName)
                .AddRule(postal => !string.IsNullOrWhiteSpace(postal), "Postal code is required")
                .AddRule(postal => string.IsNullOrWhiteSpace(postal)
                    || Regex.IsMatch(postal, @"^[A-Za-z]\d[A-Za-z][ -]?\d[A-Za-z]\d$", RegexOptions.CultureInvariant),
                    "Postal code must be in format A1A 1A1");
        }

        /// <summary>
        /// Creates a validator for hexadecimal color codes (e.g., #RRGGBB or #RGB).
        /// </summary>
        /// <param name="fieldName">The name of the field being validated.</param>
        /// <returns>A configured <see cref="Validator{String}"/> instance.</returns>
        public static Validator<string> HexColorValidator(string fieldName = "Color")
        {
            return new Validator<string>(fieldName)
                .AddRule(color => !string.IsNullOrWhiteSpace(color), "Color is required")
                .AddRule(color => string.IsNullOrWhiteSpace(color)
                    || Regex.IsMatch(color, @"^#([A-Fa-f0-9]{6}|[A-Fa-f0-9]{3})$", RegexOptions.CultureInvariant),
                    "Color must be a valid hex code (e.g., #RRGGBB or #RGB)");
        }

        /// <summary>
        /// Helper method to validate credit card numbers using the Luhn algorithm.
        /// </summary>
        private static bool IsValidLuhn(string cardNumber)
        {
            if (string.IsNullOrWhiteSpace(cardNumber) || !cardNumber.All(char.IsDigit))
                return false;

            int sum = 0;
            bool alternate = false;

            for (int i = cardNumber.Length - 1; i >= 0; i--)
            {
                int digit = cardNumber[i] - '0';

                if (alternate)
                {
                    digit *= 2;
                    if (digit > 9)
                        digit -= 9;
                }

                sum += digit;
                alternate = !alternate;
            }

            return sum % 10 == 0;
        }
    }

    /// <summary>
    /// Provides an advanced builder for validating complex objects with multiple properties.
    /// </summary>
    /// <typeparam name="T">The type of object to validate.</typeparam>
    public class ValidationBuilder<T> where T : class
    {
        private readonly Dictionary<string, Func<T, ValidationResult>> _validations
            = new Dictionary<string, Func<T, ValidationResult>>();

        /// <summary>
        /// Adds a validation function for a specific property.
        /// </summary>
        /// <param name="propertyName">The name of the property to validate.</param>
        /// <param name="validation">The validation function to apply to the property.</param>
        /// <returns>This builder instance for method chaining.</returns>
        public ValidationBuilder<T> AddValidation(string propertyName, Func<T, ValidationResult> validation)
        {
            _validations[propertyName] = validation;
            return this;
        }

        /// <summary>
        /// Validates the specified entity against all registered property validations.
        /// </summary>
        /// <param name="entity">The entity to validate.</param>
        /// <returns>A <see cref="ValidationResult"/> containing all validation errors.</returns>
        public ValidationResult Validate(T entity)
        {
            var result = new ValidationResult { IsValid = true };

            foreach (var validation in _validations)
            {
                var propertyResult = validation.Value(entity);
                if (propertyResult.Errors.Count > 0 && propertyResult.FieldErrors.Count == 0)
                {
                    var scopedResult = new ValidationResult { IsValid = propertyResult.IsValid };
                    foreach (var error in propertyResult.Errors)
                    {
                        scopedResult.AddFieldError(validation.Key, error);
                    }
                    propertyResult = scopedResult;
                }
                result = result.Merge(propertyResult);
            }

            return result;
        }
    }

    /// <summary>
    /// Example model class demonstrating validation framework usage.
    /// </summary>
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

    /// <summary>
    /// Example validator class demonstrating complex object validation using ValidationBuilder.
    /// </summary>
    public class UserProfileValidator
    {
        private readonly ValidationBuilder<UserProfile> _validator;

        /// <summary>
        /// Initializes a new instance of the <see cref="UserProfileValidator"/> class with pre-configured validations.
        /// </summary>
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

        /// <summary>
        /// Validates a user profile against all configured validation rules.
        /// </summary>
        /// <param name="profile">The user profile to validate.</param>
        /// <returns>A <see cref="ValidationResult"/> containing any validation errors.</returns>
        public ValidationResult ValidateProfile(UserProfile profile)
        {
            return _validator.Validate(profile);
        }
    }
}
