using ValidationFramework;
using Xunit;

namespace ValidationFramework.Tests
{
    public class ValidationResultTests
    {
        [Fact]
        public void ValidationResult_DefaultIsValid_ShouldBeTrue()
        {
            // Arrange & Act
            var result = new ValidationResult { IsValid = true };

            // Assert
            Assert.True(result.IsValid);
            Assert.Empty(result.Errors);
            Assert.Empty(result.FieldErrors);
        }

        [Fact]
        public void AddError_ShouldAddToErrorsList()
        {
            // Arrange
            var result = new ValidationResult();

            // Act
            result.AddError("Test error");

            // Assert
            Assert.Single(result.Errors);
            Assert.Equal("Test error", result.Errors[0]);
        }

        [Fact]
        public void AddError_WithNullOrWhitespace_ShouldNotAdd()
        {
            // Arrange
            var result = new ValidationResult();

            // Act
            result.AddError(null!);
            result.AddError("");
            result.AddError("   ");

            // Assert
            Assert.Empty(result.Errors);
        }

        [Fact]
        public void AddFieldError_ShouldAddToFieldErrors()
        {
            // Arrange
            var result = new ValidationResult();

            // Act
            result.AddFieldError("Email", "Invalid email");

            // Assert
            Assert.Single(result.FieldErrors);
            Assert.Contains("Email", result.FieldErrors.Keys);
            Assert.Single(result.FieldErrors["Email"]);
            Assert.Equal("Invalid email", result.FieldErrors["Email"][0]);
        }

        [Fact]
        public void AddFieldError_MultipleErrors_ShouldAddAll()
        {
            // Arrange
            var result = new ValidationResult();

            // Act
            result.AddFieldError("Email", "Required");
            result.AddFieldError("Email", "Invalid format");

            // Assert
            Assert.Equal(2, result.FieldErrors["Email"].Count);
        }

        [Fact]
        public void GetFieldErrors_ExistingField_ShouldReturnErrors()
        {
            // Arrange
            var result = new ValidationResult();
            result.AddFieldError("Email", "Error 1");
            result.AddFieldError("Email", "Error 2");

            // Act
            var errors = result.GetFieldErrors("Email");

            // Assert
            Assert.Equal(2, errors.Count);
        }

        [Fact]
        public void GetFieldErrors_NonExistingField_ShouldReturnEmpty()
        {
            // Arrange
            var result = new ValidationResult();

            // Act
            var errors = result.GetFieldErrors("NonExisting");

            // Assert
            Assert.Empty(errors);
        }

        [Fact]
        public void GetFirstFieldError_ExistingField_ShouldReturnFirst()
        {
            // Arrange
            var result = new ValidationResult();
            result.AddFieldError("Email", "First error");
            result.AddFieldError("Email", "Second error");

            // Act
            var error = result.GetFirstFieldError("Email");

            // Assert
            Assert.Equal("First error", error);
        }

        [Fact]
        public void GetFirstFieldError_NonExistingField_ShouldReturnNull()
        {
            // Arrange
            var result = new ValidationResult();

            // Act
            var error = result.GetFirstFieldError("NonExisting");

            // Assert
            Assert.Null(error);
        }

        [Fact]
        public void ToErrorDictionary_ShouldConvertToStringArrays()
        {
            // Arrange
            var result = new ValidationResult();
            result.AddFieldError("Email", "Error 1");
            result.AddFieldError("Name", "Error 2");

            // Act
            var dict = result.ToErrorDictionary();

            // Assert
            Assert.Equal(2, dict.Count);
            Assert.IsType<string[]>(dict["Email"]);
            Assert.Single(dict["Email"]);
            Assert.Equal("Error 1", dict["Email"][0]);
        }
    }
}
