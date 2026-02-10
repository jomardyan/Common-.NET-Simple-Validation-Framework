using ValidationFramework;
using Xunit;

namespace ValidationFramework.Tests
{
    public class ValidatorTests
    {
        [Fact]
        public void Validator_NoRules_ShouldReturnValid()
        {
            // Arrange
            var validator = new Validator<string>();

            // Act
            var result = validator.Validate("test");

            // Assert
            Assert.True(result.IsValid);
            Assert.Empty(result.Errors);
        }

        [Fact]
        public void Validator_SinglePassingRule_ShouldReturnValid()
        {
            // Arrange
            var validator = new Validator<string>()
                .AddRule(value => !string.IsNullOrEmpty(value), "Value is required");

            // Act
            var result = validator.Validate("test");

            // Assert
            Assert.True(result.IsValid);
            Assert.Empty(result.Errors);
        }

        [Fact]
        public void Validator_SingleFailingRule_ShouldReturnInvalid()
        {
            // Arrange
            var validator = new Validator<string>()
                .AddRule(value => !string.IsNullOrEmpty(value), "Value is required");

            // Act
            var result = validator.Validate("");

            // Assert
            Assert.False(result.IsValid);
            Assert.Single(result.Errors);
            Assert.Equal("Value is required", result.Errors[0]);
        }

        [Fact]
        public void Validator_MultipleFailingRules_ShouldReturnAllErrors()
        {
            // Arrange
            var validator = new Validator<string>("Name")
                .AddRule(value => !string.IsNullOrEmpty(value), "Name is required")
                .AddRule(value => value.Length >= 3, "Name must be at least 3 characters");

            // Act
            var result = validator.Validate("");

            // Assert
            Assert.False(result.IsValid);
            Assert.Equal(2, result.Errors.Count);
        }

        [Fact]
        public void Validator_WithFieldName_ShouldAddFieldErrors()
        {
            // Arrange
            var validator = new Validator<string>("Email")
                .AddRule(value => !string.IsNullOrEmpty(value), "Email is required");

            // Act
            var result = validator.Validate("");

            // Assert
            Assert.False(result.IsValid);
            Assert.Single(result.FieldErrors);
            Assert.Contains("Email", result.FieldErrors.Keys);
        }

        [Fact]
        public void Validator_WithoutFieldName_ShouldAddGeneralErrors()
        {
            // Arrange
            var validator = new Validator<string>()
                .AddRule(value => !string.IsNullOrEmpty(value), "Value is required");

            // Act
            var result = validator.Validate("");

            // Assert
            Assert.False(result.IsValid);
            Assert.Single(result.Errors);
            Assert.Empty(result.FieldErrors);
        }

        [Fact]
        public void Validator_FluentAPI_ShouldChainRules()
        {
            // Arrange
            var validator = new Validator<int>("Age")
                .AddRule(age => age >= 0, "Age cannot be negative")
                .AddRule(age => age <= 120, "Age cannot exceed 120");

            // Act
            var result = validator.Validate(25);

            // Assert
            Assert.True(result.IsValid);
        }
    }
}
