using System.Diagnostics;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using RagNet.Abstractions;
using RagNet.Core.Pipeline;
using Xunit;

namespace RagNet.Core.Tests.Pipeline;

public class DefaultRagPipelineTests
{
    [Fact]
    public async Task ExecuteAsync_ExecutesMiddlewares_AndReturnsResponse()
    {
        // Arrange
        var expectedResponse = new RagResponse { Answer = "Test Answer" };
        bool middlewareExecuted = false;

        RagPipelineDelegate pipelineDelegate = context =>
        {
            middlewareExecuted = true;
            context.OriginalQuery.Should().Be("Test Query");
            return Task.FromResult(expectedResponse);
        };

        var pipeline = new DefaultRagPipeline(pipelineDelegate);

        // Act
        var result = await pipeline.ExecuteAsync("Test Query");

        // Assert
        result.Should().Be(expectedResponse);
        middlewareExecuted.Should().BeTrue();
    }

    [Fact]
    public async Task Pipeline_EmitsExpectedActivitySpans()
    {
        // Arrange
        var activities = new List<Activity>();
        using var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name.StartsWith("RagNet"),
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStopped = activity => activities.Add(activity)
        };
        ActivitySource.AddActivityListener(listener);

        RagPipelineDelegate pipelineDelegate = context =>
        {
            return Task.FromResult(new RagResponse { Answer = "Activity Test" });
        };

        var pipeline = new DefaultRagPipeline(pipelineDelegate);

        // Act
        await pipeline.ExecuteAsync("test query");

        // Assert
        activities.Should().Contain(a => a.OperationName == "RagNet.Pipeline.Execute");
        var pipelineSpan = activities.First(a => a.OperationName == "RagNet.Pipeline.Execute");
        pipelineSpan.GetTagItem("ragnet.query.original").Should().Be("test query");
    }
}
