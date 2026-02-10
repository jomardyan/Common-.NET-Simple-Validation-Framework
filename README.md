# C# Simple Validation Framework
[![.NET](https://github.com/jomardyan/Common-.NET-Simple-Validation-Framework/actions/workflows/dotnet.yml/badge.svg)](https://github.com/jomardyan/Common-.NET-Simple-Validation-Framework/actions/workflows/dotnet.yml)

A lightweight, flexible, and extensible validation framework for .NET applications. This framework provides a clean and intuitive way to validate various data types with customizable rules and detailed error reporting.

## Table of Contents
- [Features](#features)
- [Installation](#installation)
- [Quick Start](#quick-start)
- [Usage Examples](#usage-examples)
- [Built-in Validators](#built-in-validators)
- [Creating Custom Validators](#creating-custom-validators)
- [Validation Results](#validation-results)
- [Advanced Usage](#advanced-usage)
- [Best Practices](#best-practices)
- [Contributing](#contributing)

## Features

- ✨ Fluent API for building validation rules
- 🎯 Type-safe validation
- 📝 Detailed error reporting
- 🔧 Easily extensible
- 🏗️ Built-in validators for common scenarios
- 🔄 Chainable validation rules
- 📊 Field-specific error tracking
- 🧩 UI-agnostic for WinForms, WPF, and ASP.NET apps
- 🎨 Clean and maintainable code structure

## Installation

1. Clone the repository or copy the `ValidationFramework` namespace files into your project.
2. Add the following using statement to your code:
```csharp
using ValidationFramework;
```

## Quick Start

Here's a simple example to get you started:

```csharp
// Create a validator for an email field
var emailValidator = CommonValidators.EmailValidator();

// Validate an email
var result = emailValidator.Validate("john.doe@example.com");

if (result.IsValid)
{
    Console.WriteLine("Email is valid!");
}
else
{
    foreach (var error in result.Errors)
    {
        Console.WriteLine($"Error: {error}");
    }
}
```

## Usage Examples

### Basic Field Validation

```csharp
// Email validation
var emailValidator = CommonValidators.EmailValidator();
var emailResult = emailValidator.Validate("john.doe@example.com");

// Name validation
var nameValidator = CommonValidators.NameValidator(minLength: 2, maxLength: 50);
var nameResult = nameValidator.Validate("John");

// Phone validation
var phoneValidator = CommonValidators.PhoneValidator(requiredLength: 10);
var phoneResult = phoneValidator.Validate("1234567890");
```

### WinForms (ErrorProvider)

```csharp
var result = validator.ValidateProfile(profile);
errorProvider.SetError(emailTextBox, result.GetFirstFieldError("Email") ?? string.Empty);
```

### WPF (INotifyDataErrorInfo)

```csharp
var result = validator.ValidateProfile(profile);
var emailErrors = result.GetFieldErrors(nameof(UserProfile.Email));
```

### ASP.NET Core (ModelState)

```csharp
var result = validator.ValidateProfile(profile);
foreach (var entry in result.ToErrorDictionary())
{
    foreach (var error in entry.Value)
    {
        ModelState.AddModelError(entry.Key, error);
    }
}
```

### Complex Object Validation

```csharp
public class UserProfile
{
    public string Email { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string PhoneNumber { get; set; }
    public string Password { get; set; }
    public string Website { get; set; }
    public DateTime DateOfBirth { get; set; }
    public decimal Salary { get; set; }
}

var validator = new UserProfileValidator();
var profile = new UserProfile
{
    Email = "john.doe@example.com",
    FirstName = "John",
    LastName = "Doe",
    PhoneNumber = "1234567890",
    Password = "Secure@123",
    Website = "https://example.com",
    DateOfBirth = new DateTime(1990, 1, 1),
    Salary = 50000.00m
};

var result = validator.ValidateProfile(profile);
```

## Built-in Validators

### RequiredValidator
```csharp
var required = CommonValidators.RequiredValidator(fieldName: "DisplayName");
// Validates:
// - Required field
```

### LengthValidator
```csharp
var length = CommonValidators.LengthValidator(minLength: 3, maxLength: 20, fieldName: "DisplayName");
// Validates:
// - Minimum length (optional)
// - Maximum length (optional)
```

### RegexValidator
```csharp
var regex = CommonValidators.RegexValidator(@"^\d{3}$", "Must be 3 digits", "Code");
// Validates:
// - Regex pattern match (skips empty values)
```

### RangeValidator
```csharp
var range = CommonValidators.RangeValidator(minValue: 1, maxValue: 10, fieldName: "Rating");
// Validates:
// - Value range
```

### EmailValidator
```csharp
var emailValidator = CommonValidators.EmailValidator();
// Validates:
// - Required field
// - Email format
```

### NameValidator
```csharp
var nameValidator = CommonValidators.NameValidator(
    minLength: 2,
    maxLength: 50,
    fieldName: "FirstName"
);
// Validates:
// - Required field
// - Length constraints
// - Valid characters (letters, spaces, hyphens, apostrophes)
```

### PhoneValidator
```csharp
var phoneValidator = CommonValidators.PhoneValidator(
    requiredLength: 10,
    fieldName: "Phone"
);
// Validates:
// - Required field
// - Numeric characters only
// - Exact length
```

### PasswordValidator
```csharp
var passwordValidator = CommonValidators.PasswordValidator();
// Validates:
// - Minimum length (8 characters)
// - Contains uppercase letter
// - Contains lowercase letter
// - Contains number
// - Contains special character
```

### UrlValidator
```csharp
var urlValidator = CommonValidators.UrlValidator();
// Validates:
// - Required field
// - Valid URL format
```

### DateValidator
```csharp
var dateValidator = CommonValidators.DateValidator(
    minDate: DateTime.Now.AddYears(-120),
    maxDate: DateTime.Now.AddYears(-18)
);
// Validates:
// - Date range
```

### CurrencyValidator
```csharp
var currencyValidator = CommonValidators.CurrencyValidator(
    minValue: 0,
    maxValue: 1000000
);
// Validates:
// - Non-negative amounts
// - Maximum 2 decimal places
// - Value range
```

## Creating Custom Validators

You can create custom validators by extending the base `Validator<T>` class:

```csharp
public static Validator<string> CustomValidator()
{
    return new Validator<string>("CustomField")
        .AddRule(value => !string.IsNullOrEmpty(value), "Value is required")
        .AddRule(value => // your custom rule, "Your error message");
}
```

## Validation Results

The `ValidationResult` class provides detailed information about validation results:

```csharp
public class ValidationResult
{
    public bool IsValid { get; set; }
    public List<string> Errors { get; }
    public Dictionary<string, List<string>> FieldErrors { get; }
}
```

Helper methods are available for UI integration:

```csharp
var errors = result.GetFieldErrors("Email");
var firstError = result.GetFirstFieldError("Email");
```

### Handling Validation Results

```csharp
var result = validator.ValidateProfile(profile);

if (!result.IsValid)
{
    // Access general errors
    foreach (var error in result.Errors)
    {
        Console.WriteLine($"Error: {error}");
    }

    // Access field-specific errors
    foreach (var fieldErrors in result.FieldErrors)
    {
        Console.WriteLine($"Field: {fieldErrors.Key}");
        foreach (var error in fieldErrors.Value)
        {
            Console.WriteLine($"  - {error}");
        }
    }
}
```

## Advanced Usage

### Using ValidationBuilder

```csharp
var builder = new ValidationBuilder<UserProfile>()
    .AddValidation("Email", p => CommonValidators.EmailValidator().Validate(p.Email))
    .AddValidation("FirstName", p => CommonValidators.NameValidator().Validate(p.FirstName));

var result = builder.Validate(userProfile);
```

### Merging Validation Results

```csharp
var result1 = emailValidator.Validate(email);
var result2 = nameValidator.Validate(name);
var combinedResult = result1.Merge(result2);
```

## Best Practices

1. **Field Names**: Always provide meaningful field names for better error messages
```csharp
var validator = CommonValidators.EmailValidator(fieldName: "Work Email");
```

2. **Custom Validation Rules**: Keep rules simple and focused
```csharp
.AddRule(value => value.Length > 0, "Value is required")
```

3. **Error Messages**: Write clear, actionable error messages
```csharp
"Password must contain at least one uppercase letter"
```

4. **Validation Groups**: Organize related validations using ValidationBuilder
```csharp
var builder = new ValidationBuilder<UserProfile>()
    .AddValidation("PersonalInfo", ValidatePersonalInfo)
    .AddValidation("ContactInfo", ValidateContactInfo);
```

## Contributing

Contributions are welcome! Feel free to:
1. Fork the repository
2. Create a feature branch
3. Submit a pull request

Please ensure your code follows the existing style and includes appropriate tests.

## License

This project is licensed under the MIT License - see the LICENSE file for details.
