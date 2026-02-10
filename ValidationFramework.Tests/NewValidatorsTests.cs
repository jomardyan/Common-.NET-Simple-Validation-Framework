using ValidationFramework;
using Xunit;

namespace ValidationFramework.Tests
{
    public class NewValidatorsTests
    {
        [Theory]
        [InlineData("12345", true)]
        [InlineData("12345-6789", true)]
        [InlineData("1234", false)] // Too short
        [InlineData("123456", false)] // Too long
        [InlineData("12345-678", false)] // Invalid format
        [InlineData("abcde", false)] // Letters
        [InlineData("", false)]
        public void ZipCodeValidator_ShouldValidateCorrectly(string zipCode, bool expectedValid)
        {
            // Arrange
            var validator = CommonValidators.ZipCodeValidator();

            // Act
            var result = validator.Validate(zipCode);

            // Assert
            Assert.Equal(expectedValid, result.IsValid);
        }

        [Theory]
        [InlineData("abc123", true)]
        [InlineData("Test123", true)]
        [InlineData("abc-123", false)] // Hyphen
        [InlineData("abc 123", false)] // Space
        [InlineData("abc@123", false)] // Special char
        [InlineData("", false)]
        public void AlphanumericValidator_ShouldValidateCorrectly(string value, bool expectedValid)
        {
            // Arrange
            var validator = CommonValidators.AlphanumericValidator();

            // Act
            var result = validator.Validate(value);

            // Assert
            Assert.Equal(expectedValid, result.IsValid);
        }

        [Theory]
        [InlineData("192.168.1.1", true)]
        [InlineData("10.0.0.1", true)]
        [InlineData("255.255.255.255", true)]
        [InlineData("256.1.1.1", false)] // Out of range
        [InlineData("192.168", false)] // Incomplete
        [InlineData("abc.def.ghi.jkl", false)] // Letters
        [InlineData("", false)]
        public void IpAddressValidator_ShouldValidateCorrectly(string ip, bool expectedValid)
        {
            // Arrange
            var validator = CommonValidators.IpAddressValidator();

            // Act
            var result = validator.Validate(ip);

            // Assert
            Assert.Equal(expectedValid, result.IsValid);
        }

        [Theory]
        [InlineData("john_doe", true)]
        [InlineData("user123", true)]
        [InlineData("test-user", true)]
        [InlineData("a_b_c", true)]
        [InlineData("123user", false)] // Starts with number
        [InlineData("_user", false)] // Starts with underscore
        [InlineData("us", false)] // Too short (min 3)
        [InlineData("user@test", false)] // Invalid char
        [InlineData("", false)]
        public void UsernameValidator_ShouldValidateCorrectly(string username, bool expectedValid)
        {
            // Arrange
            var validator = CommonValidators.UsernameValidator();

            // Act
            var result = validator.Validate(username);

            // Assert
            Assert.Equal(expectedValid, result.IsValid);
        }

        [Theory]
        [InlineData("4532015112830366", true)] // Valid Visa
        [InlineData("6011111111111117", true)] // Valid Discover
        [InlineData("5425233430109903", true)] // Valid Mastercard
        [InlineData("4532 0151 1283 0366", true)] // Valid with spaces
        [InlineData("4532-0151-1283-0366", true)] // Valid with hyphens
        [InlineData("4532015112830367", false)] // Invalid checksum
        [InlineData("123", false)] // Too short
        [InlineData("12345678901234567890", false)] // Too long
        [InlineData("", false)]
        public void CreditCardValidator_ShouldValidateCorrectly(string cardNumber, bool expectedValid)
        {
            // Arrange
            var validator = CommonValidators.CreditCardValidator();

            // Act
            var result = validator.Validate(cardNumber);

            // Assert
            Assert.Equal(expectedValid, result.IsValid);
        }

        [Theory]
        [InlineData("K1A 0B1", true)]
        [InlineData("M5W1E6", true)] // Without space
        [InlineData("K1A-0B1", true)] // With hyphen
        [InlineData("k1a 0b1", true)] // Lowercase
        [InlineData("12345", false)] // Not postal code
        [InlineData("ABCDEF", false)] // Invalid format
        [InlineData("", false)]
        public void PostalCodeValidator_ShouldValidateCorrectly(string postalCode, bool expectedValid)
        {
            // Arrange
            var validator = CommonValidators.PostalCodeValidator();

            // Act
            var result = validator.Validate(postalCode);

            // Assert
            Assert.Equal(expectedValid, result.IsValid);
        }

        [Theory]
        [InlineData("#FF0000", true)] // Red
        [InlineData("#00FF00", true)] // Green
        [InlineData("#00f", true)] // Short form
        [InlineData("#ABC", true)] // Short form
        [InlineData("FF0000", false)] // Missing #
        [InlineData("#GG0000", false)] // Invalid hex
        [InlineData("#12345", false)] // Invalid length
        [InlineData("", false)]
        public void HexColorValidator_ShouldValidateCorrectly(string color, bool expectedValid)
        {
            // Arrange
            var validator = CommonValidators.HexColorValidator();

            // Act
            var result = validator.Validate(color);

            // Assert
            Assert.Equal(expectedValid, result.IsValid);
        }

        [Fact]
        public void AlphanumericValidator_WithCustomLength_ShouldValidateCorrectly()
        {
            // Arrange
            var validator = CommonValidators.AlphanumericValidator(minLength: 5, maxLength: 10);

            // Act
            var shortResult = validator.Validate("abc");
            var validResult = validator.Validate("abcde");
            var longResult = validator.Validate("abcdefghijk");

            // Assert
            Assert.False(shortResult.IsValid);
            Assert.True(validResult.IsValid);
            Assert.False(longResult.IsValid);
        }

        [Fact]
        public void UsernameValidator_WithCustomLength_ShouldValidateCorrectly()
        {
            // Arrange
            var validator = CommonValidators.UsernameValidator(minLength: 5, maxLength: 15);

            // Act
            var shortResult = validator.Validate("user");
            var validResult = validator.Validate("username");
            var longResult = validator.Validate("verylongusername");

            // Assert
            Assert.False(shortResult.IsValid);
            Assert.True(validResult.IsValid);
            Assert.False(longResult.IsValid);
        }
    }
}
