using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Moq;
using src.Domain.Entities.WorkspaceEntity;
using src.Domain.Entities.WorkspaceEntity.SupportEntiy;
using src.Feature.TaskManager.UpdateTask;
using src.Infrastructure.Abstractions.IRepositories;
using src.Infrastructure.Data;
using Xunit;

namespace test.TaskTest;

public class UpdateTaskTests
{
    private readonly Mock<PlannerDbContext> _mockContext;
    private readonly Mock<IHierarchyRepository> _mockHierarchyRepo;
    private readonly UpdateTaskHandler _handler;

    public UpdateTaskTests()
    {
        // Command: Setup common mocks for all tests in this class
        var options = new DbContextOptionsBuilder<PlannerDbContext>().Options;
        _mockContext = new Mock<PlannerDbContext>(options);
        _mockHierarchyRepo = new Mock<IHierarchyRepository>();
        _handler = new UpdateTaskHandler(_mockContext.Object, _mockHierarchyRepo.Object);
    }

    [Fact]
    public async System.Threading.Tasks.Task Handle_WhenTaskExists_ShouldUpdateTaskAndSaveChanges()
    {
        // Command: Arrange the test
        var testTaskId = Guid.NewGuid();
        var originalTask = PlanTask.Create("Original Name", "Original Desc", 1, PlanTaskStatus.ToDo, null, null, false, Guid.NewGuid(), Guid.NewGuid(), null, Guid.NewGuid(), Guid.NewGuid());

        // Setup the repository to return our test task when asked for it
        _mockHierarchyRepo.Setup(r => r.GetPlanTaskByIdAsync(testTaskId, It.IsAny<CancellationToken>()))
                          .ReturnsAsync(originalTask);

        var request = new UpdateTaskRequest(
            Id: testTaskId,
            Name: "Updated Name",
            Description: "Updated Description",
            Priority: 3,
            Status: PlanTaskStatus.Done,
            StartDate: null, DueDate: null, TimeEstimate: null, TimeSpent: null, OrderIndex: null, IsArchived: null, IsPrivate: true
        );

        // Command: Act by calling the handler
        var result = await _handler.Handle(request, CancellationToken.None);

        // Command: Assert the outcome
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        
        // Verify all updated properties
        Assert.Equal("Updated Name", result.Value.task.Name);
        Assert.Equal("Updated Description", result.Value.task.Description);
        Assert.Equal(3, result.Value.task.Priority);
        Assert.Equal(PlanTaskStatus.Done, result.Value.task.Status);
        Assert.True(result.Value.task.IsPrivate);

        // Verify that SaveChanges was called to persist the update
        _mockContext.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async System.Threading.Tasks.Task Handle_WhenTaskDoesNotExist_ShouldReturnNotFound()
    {
        // Command: Arrange the test for a non-existent task
        var nonExistentTaskId = Guid.NewGuid();
        _mockHierarchyRepo.Setup(r => r.GetPlanTaskByIdAsync(nonExistentTaskId, It.IsAny<CancellationToken>()))
                          .ReturnsAsync((PlanTask)null);

        var request = new UpdateTaskRequest(nonExistentTaskId, "Doesn't Matter", null, null, null, null, null, null, null, null, null, null);

        // Command: Act by calling the handler
        var result = await _handler.Handle(request, CancellationToken.None);

        // Command: Assert the outcome
        Assert.False(result.IsSuccess);
        Assert.Equal("Task not found", result.Error.Title);

        // Also, verify that we didn't try to save any changes on failure
        _mockContext.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never());
    }
}