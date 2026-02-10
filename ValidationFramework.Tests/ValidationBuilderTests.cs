using ValidationFramework;
using Xunit;

namespace ValidationFramework.Tests
{
    public class ValidationExtensionsTests
    {
        [Fact]
        public void Merge_TwoValidResults_ShouldReturnValid()
        {
            // Arrange
            var result1 = new ValidationResult { IsValid = true };
            var result2 = new ValidationResult { IsValid = true };

            // Act
            var merged = result1.Merge(result2);

            // Assert
            Assert.True(merged.IsValid);
            Assert.Empty(merged.Errors);
        }

        [Fact]
        public void Merge_OneInvalidResult_ShouldReturnInvalid()
        {
            // Arrange
            var result1 = new ValidationResult { IsValid = true };
            var result2 = new ValidationResult { IsValid = false };
            result2.AddError("Error from result2");

            // Act
            var merged = result1.Merge(result2);

            // Assert
            Assert.False(merged.IsValid);
            Assert.Single(merged.Errors);
        }

        [Fact]
        public void Merge_ShouldCombineErrors()
        {
            // Arrange
            var result1 = new ValidationResult { IsValid = false };
            result1.AddError("Error 1");

            var result2 = new ValidationResult { IsValid = false };
            result2.AddError("Error 2");

            // Act
            var merged = result1.Merge(result2);

            // Assert
            Assert.False(merged.IsValid);
            Assert.Equal(2, merged.Errors.Count);
        }

        [Fact]
        public void Merge_ShouldCombineFieldErrors()
        {
            // Arrange
            var result1 = new ValidationResult { IsValid = false };
            result1.AddFieldError("Email", "Invalid email");

            var result2 = new ValidationResult { IsValid = false };
            result2.AddFieldError("Name", "Name required");

            // Act
            var merged = result1.Merge(result2);

            // Assert
            Assert.False(merged.IsValid);
            Assert.Equal(2, merged.FieldErrors.Count);
            Assert.Contains("Email", merged.FieldErrors.Keys);
            Assert.Contains("Name", merged.FieldErrors.Keys);
        }

        [Fact]
        public void MergeAll_MultipleResults_ShouldCombineAll()
        {
            // Arrange
            var results = new List<ValidationResult>
            {
                new ValidationResult { IsValid = false },
                new ValidationResult { IsValid = false },
                new ValidationResult { IsValid = false }
            };
            results[0].AddError("Error 1");
            results[1].AddError("Error 2");
            results[2].AddError("Error 3");

            // Act
            var merged = results.MergeAll();

            // Assert
            Assert.False(merged.IsValid);
            Assert.Equal(3, merged.Errors.Count);
        }

        [Fact]
        public void MergeAll_EmptyList_ShouldReturnValid()
        {
            // Arrange
            var results = new List<ValidationResult>();

            // Act
            var merged = results.MergeAll();

            // Assert
            Assert.True(merged.IsValid);
            Assert.Empty(merged.Errors);
        }

        [Fact]
        public void MergeAll_NullResults_ShouldThrowArgumentNullException()
        {
            // Arrange
            IEnumerable<ValidationResult> results = null!;

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => results.MergeAll());
        }
    }

    public class ValidationBuilderTests
    {
        private class TestModel
        {
            public string Email { get; set; } = string.Empty;
            public string Name { get; set; } = string.Empty;
            public int Age { get; set; }
        }

        [Fact]
        public void ValidationBuilder_NoValidations_ShouldReturnValid()
        {
            // Arrange
            var builder = new ValidationBuilder<TestModel>();
            var model = new TestModel();

            // Act
            var result = builder.Validate(model);

            // Assert
            Assert.True(result.IsValid);
        }

        [Fact]
        public void ValidationBuilder_SingleValidation_ShouldValidate()
        {
            // Arrange
            var builder = new ValidationBuilder<TestModel>()
                .AddValidation("Email", m => CommonValidators.EmailValidator().Validate(m.Email));

            var model = new TestModel { Email = "invalid-email" };

            // Act
            var result = builder.Validate(model);

            // Assert
            Assert.False(result.IsValid);
            Assert.Contains("Email", result.FieldErrors.Keys);
        }

        [Fact]
        public void ValidationBuilder_MultipleValidations_ShouldValidateAll()
        {
            // Arrange
            var builder = new ValidationBuilder<TestModel>()
                .AddValidation("Email", m => CommonValidators.EmailValidator().Validate(m.Email))
                .AddValidation("Name", m => CommonValidators.NameValidator().Validate(m.Name))
                .AddValidation("Age", m => CommonValidators.RangeValidator<int>(minValue: 0, maxValue: 120).Validate(m.Age));

            var model = new TestModel
            {
                Email = "invalid",
                Name = "X",
                Age = -5
            };

            // Act
            var result = builder.Validate(model);

            // Assert
            Assert.False(result.IsValid);
            Assert.Equal(3, result.FieldErrors.Count);
        }

        [Fact]
        public void ValidationBuilder_AllValid_ShouldReturnValid()
        {
            // Arrange
            var builder = new ValidationBuilder<TestModel>()
                .AddValidation("Email", m => CommonValidators.EmailValidator().Validate(m.Email))
                .AddValidation("Name", m => CommonValidators.NameValidator().Validate(m.Name));

            var model = new TestModel
            {
                Email = "test@example.com",
                Name = "John Doe"
            };

            // Act
            var result = builder.Validate(model);

            // Assert
            Assert.True(result.IsValid);
        }

        [Fact]
        public void UserProfileValidator_InvalidProfile_ShouldReturnErrors()
        {
            // Arrange
            var validator = new UserProfileValidator();
            var profile = new UserProfile
            {
                Email = "invalid",
                FirstName = "X",
                LastName = "",
                PhoneNumber = "123",
                Password = "weak",
                Website = "not-a-url",
                DateOfBirth = DateTime.Now.AddYears(-5), // Too young
                Salary = -100
            };

            // Act
            var result = validator.ValidateProfile(profile);

            // Assert
            Assert.False(result.IsValid);
            Assert.NotEmpty(result.FieldErrors);
            Assert.True(result.FieldErrors.Count > 5); // Multiple fields have errors
        }

        [Fact]
        public void UserProfileValidator_ValidProfile_ShouldReturnValid()
        {
            // Arrange
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

            // Act
            var result = validator.ValidateProfile(profile);

            // Assert
            Assert.True(result.IsValid);
            Assert.Empty(result.Errors);
        }
    }
}
