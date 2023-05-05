using System;
using System.IO;
using Xunit;

namespace Notation.Plugin.Protocol.Tests
{
    [Collection(nameof(OutputTestCollectionDefinition))]
    public class ErrorAndExceptionTests
    {
        [Fact]
        public void PluginException_ValidParameters()
        {
            // Arrange
            string message = "Test error message";
            string code = "TEST_ERROR_CODE";

            // Act
            PluginException exception = new PluginException(message, code);

            // Assert
            Assert.Equal(message, exception.Message);
            Assert.Equal(code, exception.Code);
        }

        [Fact]
        public void PluginException_DefaultErrorCode()
        {
            // Arrange
            string message = "Test error message";

            // Act
            PluginException exception = new PluginException(message);

            // Assert
            Assert.Equal(message, exception.Message);
            Assert.Equal(Error.ERROR, exception.Code);
        }

        [Fact]
        public void ValidationException_ValidParameters()
        {
            // Arrange
            string message = "Test validation error message";

            // Act
            ValidationException exception = new ValidationException(message);

            // Assert
            Assert.Equal(message, exception.Message);
            Assert.Equal(Error.VALIDATION_ERROR, exception.Code);
        }

        [Fact]
        public void PrintError_WritesToStandardError()
        {
            // Arrange
            string errorCode = "TEST_ERROR_CODE";
            string errorMessage = "Test error message";
            string expectedOutput = "{\"errorCode\":\"TEST_ERROR_CODE\",\"errorMessage\":\"Test error message\"}";

            // Redirect standard error output
            StringWriter stringWriter = new StringWriter();
            TextWriter originalStdErr = Console.Error;
            Console.SetError(stringWriter);

            try
            {
                // Act
                Error.PrintError(errorCode, errorMessage);

                // Assert
                string actualOutput = stringWriter.ToString().Trim();
                Assert.Equal(expectedOutput, actualOutput);
            }
            finally
            {
                // Restore original standard error output
                Console.SetError(originalStdErr);
            }
        }
    }
}