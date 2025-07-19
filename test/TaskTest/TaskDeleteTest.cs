using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Moq;
using src.Domain.Entities.WorkspaceEntity;
using src.Domain.Entities.WorkspaceEntity.SupportEntiy;
using src.Feature.TaskManager.DeleteTask;
using src.Infrastructure.Abstractions.IRepositories;
using src.Infrastructure.Data;
using Xunit;
namespace test.TaskTest;

public class DeleteTaskTests
{
    private readonly Mock<PlannerDbContext> _mockContext;
    private readonly Mock<DbSet<PlanTask>> _mockTaskDbSet;
    private readonly Mock<IHierarchyRepository> _mockHierarchyRepo;
    private readonly DeleteTaskHandler _handler;

    public DeleteTaskTests()
    {
        // Command: Setup common mocks for all tests in this class
        var options = new DbContextOptionsBuilder<PlannerDbContext>().Options;
        _mockContext = new Mock<PlannerDbContext>(options);
        _mockTaskDbSet = new Mock<DbSet<PlanTask>>();
        _mockHierarchyRepo = new Mock<IHierarchyRepository>();

        // Setup the context to return the mock DbSet. This requires the 'Tasks' property on PlannerDbContext to be virtual.
        _mockContext.Setup(c => c.Tasks).Returns(_mockTaskDbSet.Object);
        _handler = new DeleteTaskHandler(_mockContext.Object, _mockHierarchyRepo.Object);
    }

    [Fact]
    public async Task Handle_WhenTaskExists_ShouldRemoveTaskAndSaveChanges()
    {
        // Command: Arrange the test
        var testTaskId = Guid.NewGuid();
        var taskToDelete = PlanTask.Create("Task to Delete", "", 1, PlanTaskStatus.ToDo, null, null, false, Guid.NewGuid(), Guid.NewGuid(), null, Guid.NewGuid(), Guid.NewGuid());

        _mockHierarchyRepo.Setup(r => r.GetPlanTaskByIdAsync(testTaskId, It.IsAny<CancellationToken>()))
                          .ReturnsAsync(taskToDelete);

        var request = new DeleteTaskRequest(testTaskId);

        // Command: Act by calling the handler
        var result = await _handler.Handle(request, CancellationToken.None);

        // Command: Assert the outcome
        Assert.True(result.IsSuccess);
        
        // Verify that Remove was called on the DbSet, not the context
        _mockTaskDbSet.Verify(dbSet => dbSet.Remove(taskToDelete), Times.Once);
        _mockContext.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenTaskDoesNotExist_ShouldReturnNotFound()
    {
        // Command: Arrange the test for a non-existent task
        var nonExistentTaskId = Guid.NewGuid();
        _mockHierarchyRepo.Setup(r => r.GetPlanTaskByIdAsync(nonExistentTaskId, It.IsAny<CancellationToken>())).ReturnsAsync((PlanTask)null);
        var request = new DeleteTaskRequest(nonExistentTaskId);

        // Command: Act by calling the handler
        var result = await _handler.Handle(request, CancellationToken.None);

        // Command: Assert the outcome
        Assert.False(result.IsSuccess);
        Assert.Equal("Task not found", result.Error.Title);

        // Verify that SaveChanges was NOT called
        _mockContext.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never());
    }
}
