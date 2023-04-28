using System;
using System.IO;
using Xunit;

namespace Notation.Plugin.Protocol.Tests
{
    [CollectionDefinition(nameof(OutputTestCollectionDefinition), DisableParallelization = true)]
    public class OutputTestCollectionDefinition { }

    [Collection(nameof(OutputTestCollectionDefinition))]
    public class PluginIOTests
    {

        [Fact]
        public void ReadInput_ReturnsCorrectString()
        {
            // Arrange
            const string expectedInput = "{\"test\":\"value\"}";
            using var stringReader = new StringReader(expectedInput);
            Console.SetIn(stringReader);

            // Act
            string actualInput = PluginIO.ReadInput();

            // Assert
            Assert.Equal(expectedInput, actualInput);
        }

        [Fact]
        public void ReadInput_ThrowsValidationExceptionOnEmptyInput()
        {
            // Arrange
            using var stringReader = new StringReader(string.Empty);
            Console.SetIn(stringReader);

            // Act & Assert
            Assert.Throws<ValidationException>(() => PluginIO.ReadInput());
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void WriteOutput_WritesCorrectOutput(bool stderr)
        {
            // Arrange
            var obj = new { test = "value" };
            const string expectedOutput = "{\"test\":\"value\"}\n";

            using var stringWriter = new StringWriter();
            if (stderr)
            {
                Console.SetError(stringWriter);
            }
            else
            {
                Console.SetOut(stringWriter);
            }

            // Act
            PluginIO.WriteOutput(obj, stderr);

            // Assert
            string actualOutput = stringWriter.ToString();
            Assert.Equal(expectedOutput, actualOutput);
        }
    }
}
