﻿using ChilliCream.Tracing.Analyzer;
using FluentAssertions;
using Xunit;

namespace ChilliCream.Tracing.Tests
{
    public class ApplicationEventSourceTests
    {
        [Fact(DisplayName = "Analyze: Inspects ApplicationEventSource schema")]
        public void Analyze()
        {
            // arrange
            EventSourceAnalyzer analyzer = new EventSourceAnalyzer();

            // act
            Report report = analyzer.Inspect(ApplicationEventSource.Log);

            // assert
            report.HasErrors.Should().BeFalse();
        }
    }
}