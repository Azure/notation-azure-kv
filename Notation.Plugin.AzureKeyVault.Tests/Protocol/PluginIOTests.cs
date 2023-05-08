using System;
using System.IO;
using System.Text.Json.Serialization;
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
    }
}
