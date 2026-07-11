using FluentAssertions;
using SmartTaskbar.Win11.Helpers;
using Xunit;

namespace SmartTaskbar.Win11.Tests;

public class TaskbarAlignmentHelperTests
{
    [Fact]
    public void IsCentered_WhenTaskbarAlIsZero_ReturnsTrue()
        => new TaskbarAlignmentHelper(new TestRegistryReader(0)).IsCentered.Should().BeTrue();

    [Fact]
    public void IsCentered_WhenTaskbarAlIsOne_ReturnsFalse()
        => new TaskbarAlignmentHelper(new TestRegistryReader(1)).IsCentered.Should().BeFalse();

    [Fact]
    public void IsCentered_WhenTaskbarAlNotPresent_ReturnsTrue()
        => new TaskbarAlignmentHelper(new TestRegistryReader(null)).IsCentered.Should().BeTrue();

    [Fact]
    public void IsCentered_WhenTaskbarAlIsTwo_ReturnsFalse()
        => new TaskbarAlignmentHelper(new TestRegistryReader(2)).IsCentered.Should().BeFalse();

    private class TestRegistryReader : IRegistryReader
    {
        private readonly int? _value;

        public TestRegistryReader(int? value)
        {
            _value = value;
        }

        public int? GetDwordValue(string keyPath, string valueName)
        {
            return _value;
        }
    }
}
