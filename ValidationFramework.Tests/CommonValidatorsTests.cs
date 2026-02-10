using ValidationFramework;
using Xunit;

namespace ValidationFramework.Tests
{
    public class CommonValidatorsTests
    {
        [Theory]
        [InlineData("test@example.com", true)]
        [InlineData("user@domain.co.uk", true)]
        [InlineData("invalid", false)]
        [InlineData("@example.com", false)]
        [InlineData("", false)]
        public void EmailValidator_ShouldValidateCorrectly(string email, bool expectedValid)
        {
            // Arrange
            var validator = CommonValidators.EmailValidator();

            // Act
            var result = validator.Validate(email);

            // Assert
            Assert.Equal(expectedValid, result.IsValid);
        }

        [Theory]
        [InlineData("John", true)]
        [InlineData("Mary-Jane", true)]
        [InlineData("O'Brien", true)]
        [InlineData("", false)]
        [InlineData("J", false)] // Too short
        [InlineData("John123", false)] // Contains numbers
        public void NameValidator_ShouldValidateCorrectly(string name, bool expectedValid)
        {
            // Arrange
            var validator = CommonValidators.NameValidator();

            // Act
            var result = validator.Validate(name);

            // Assert
            Assert.Equal(expectedValid, result.IsValid);
        }

        [Theory]
        [InlineData("1234567890", true)]
        [InlineData("123456789", false)] // Too short
        [InlineData("12345678901", false)] // Too long
        [InlineData("12345abcde", false)] // Contains letters
        [InlineData("", false)]
        public void PhoneValidator_ShouldValidateCorrectly(string phone, bool expectedValid)
        {
            // Arrange
            var validator = CommonValidators.PhoneValidator(requiredLength: 10);

            // Act
            var result = validator.Validate(phone);

            // Assert
            Assert.Equal(expectedValid, result.IsValid);
        }

        [Theory]
        [InlineData("Password1!", true)]
        [InlineData("Pass1!", false)] // Too short
        [InlineData("password1!", false)] // No uppercase
        [InlineData("PASSWORD1!", false)] // No lowercase
        [InlineData("Password!", false)] // No number
        [InlineData("Password1", false)] // No special char
        [InlineData("", false)]
        public void PasswordValidator_ShouldValidateCorrectly(string password, bool expectedValid)
        {
            // Arrange
            var validator = CommonValidators.PasswordValidator();

            // Act
            var result = validator.Validate(password);

            // Assert
            Assert.Equal(expectedValid, result.IsValid);
        }

        [Theory]
        [InlineData("https://example.com", true)]
        [InlineData("http://test.org", true)]
        [InlineData("ftp://files.test.com", true)]
        [InlineData("not-a-url", false)]
        [InlineData("", false)]
        public void UrlValidator_ShouldValidateCorrectly(string url, bool expectedValid)
        {
            // Arrange
            var validator = CommonValidators.UrlValidator();

            // Act
            var result = validator.Validate(url);

            // Assert
            Assert.Equal(expectedValid, result.IsValid);
        }

        [Theory]
        [InlineData("test", true)]
        [InlineData("", false)]
        [InlineData("   ", false)]
        public void RequiredValidator_ShouldValidateCorrectly(string value, bool expectedValid)
        {
            // Arrange
            var validator = CommonValidators.RequiredValidator();

            // Act
            var result = validator.Validate(value!);

            // Assert
            Assert.Equal(expectedValid, result.IsValid);
        }

        [Theory]
        [InlineData("abc", true)] // minLength=3
        [InlineData("ab", false)] // Too short
        [InlineData("abcdefghijk", false)] // maxLength=10
        [InlineData("", false)] // Empty with minLength > 0
        public void LengthValidator_ShouldValidateCorrectly(string value, bool expectedValid)
        {
            // Arrange
            var validator = CommonValidators.LengthValidator(minLength: 3, maxLength: 10);

            // Act
            var result = validator.Validate(value);

            // Assert
            Assert.Equal(expectedValid, result.IsValid);
        }

        [Theory]
        [InlineData(5, true)]
        [InlineData(1, true)]
        [InlineData(10, true)]
        [InlineData(0, false)]
        [InlineData(11, false)]
        public void RangeValidator_ShouldValidateCorrectly(int value, bool expectedValid)
        {
            // Arrange
            var validator = CommonValidators.RangeValidator<int>(minValue: 1, maxValue: 10);

            // Act
            var result = validator.Validate(value);

            // Assert
            Assert.Equal(expectedValid, result.IsValid);
        }

        [Fact]
        public void DateValidator_WithinRange_ShouldBeValid()
        {
            // Arrange
            var minDate = new DateTime(2000, 1, 1);
            var maxDate = new DateTime(2025, 12, 31);
            var validator = CommonValidators.DateValidator(minDate, maxDate);

            // Act
            var result = validator.Validate(new DateTime(2020, 6, 15));

            // Assert
            Assert.True(result.IsValid);
        }

        [Fact]
        public void DateValidator_OutsideRange_ShouldBeInvalid()
        {
            // Arrange
            var minDate = new DateTime(2000, 1, 1);
            var maxDate = new DateTime(2025, 12, 31);
            var validator = CommonValidators.DateValidator(minDate, maxDate);

            // Act
            var result = validator.Validate(new DateTime(1990, 6, 15));

            // Assert
            Assert.False(result.IsValid);
        }

        [Theory]
        [InlineData(100.50, true)]
        [InlineData(0, true)]
        [InlineData(99.99, true)]
        [InlineData(-1, false)] // Negative
        [InlineData(100.999, false)] // More than 2 decimals
        public void CurrencyValidator_ShouldValidateCorrectly(decimal value, bool expectedValid)
        {
            // Arrange
            var validator = CommonValidators.CurrencyValidator(minValue: 0, maxValue: 1000);

            // Act
            var result = validator.Validate(value);

            // Assert
            Assert.Equal(expectedValid, result.IsValid);
        }

        [Theory]
        [InlineData("123", "^\\d{3}$", true)]
        [InlineData("abc", "^\\d{3}$", false)]
        [InlineData("", "^\\d{3}$", true)] // Empty is valid for regex validator
        public void RegexValidator_ShouldValidateCorrectly(string value, string pattern, bool expectedValid)
        {
            // Arrange
            var validator = CommonValidators.RegexValidator(pattern, "Must match pattern");

            // Act
            var result = validator.Validate(value);

            // Assert
            Assert.Equal(expectedValid, result.IsValid);
        }
    }
}
