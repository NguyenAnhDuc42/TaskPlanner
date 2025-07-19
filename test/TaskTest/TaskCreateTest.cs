using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Moq;
using src.Domain.Entities.WorkspaceEntity;
using src.Feature.ListManager.CreateTaskInList;
using src.Feature.TaskManager.CreateTask;
using src.Infrastructure.Abstractions.IServices;
using src.Infrastructure.Data;
using Xunit;
using Microsoft.EntityFrameworkCore.Query;

namespace test.TaskTest
{
    // --- Helper classes to enable async mocking for EF Core ---
    // These helpers make the mock DbSet behave like a real one for async queries.
    internal class TestAsyncQueryProvider<TEntity> : IAsyncQueryProvider
    {
        private readonly IQueryProvider _inner;

        internal TestAsyncQueryProvider(IQueryProvider inner)
        {
            _inner = inner;
        }

        public IQueryable CreateQuery(Expression expression)
        {
            return new TestAsyncEnumerable<TEntity>(expression);
        }

        public IQueryable<TElement> CreateQuery<TElement>(Expression expression)
        {
            return new TestAsyncEnumerable<TElement>(expression);
        }

        public object Execute(Expression expression)
        {
            return _inner.Execute(expression);
        }

        public TResult Execute<TResult>(Expression expression)
        {
            return _inner.Execute<TResult>(expression);
        }

        public TResult ExecuteAsync<TResult>(Expression expression, CancellationToken cancellationToken)
        {
            var resultType = typeof(TResult).GetGenericArguments()[0];
            var executionResult = typeof(IQueryProvider)
                                 .GetMethod(nameof(IQueryProvider.Execute), 1, new[] { typeof(Expression) })
                                 .MakeGenericMethod(resultType)
                                 .Invoke(this, new[] { expression });

            return (TResult)typeof(Task).GetMethod(nameof(Task.FromResult))
                                        .MakeGenericMethod(resultType)
                                        .Invoke(null, new[] { executionResult });
        }
    }

    internal class TestAsyncEnumerable<T> : EnumerableQuery<T>, IAsyncEnumerable<T>, IQueryable<T>
    {
        public TestAsyncEnumerable(IEnumerable<T> enumerable) : base(enumerable) { }
        public TestAsyncEnumerable(Expression expression) : base(expression) { }
        public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default) => new TestAsyncEnumerator<T>(this.AsEnumerable().GetEnumerator());
        IQueryProvider IQueryable.Provider => new TestAsyncQueryProvider<T>(this);
    }

    internal class TestAsyncEnumerator<T> : IAsyncEnumerator<T>
    {
        private readonly IEnumerator<T> _inner;
        public TestAsyncEnumerator(IEnumerator<T> inner) => _inner = inner;
        public void Dispose() => _inner.Dispose();
        public T Current => _inner.Current;
        public ValueTask<bool> MoveNextAsync() => new ValueTask<bool>(Task.FromResult(_inner.MoveNext()));
        public ValueTask DisposeAsync()
        {
            _inner.Dispose();
            return new ValueTask();
        }
    }

    public class TaskCreateTests
    {
        private readonly Mock<PlannerDbContext> _mockContext;
        private readonly Mock<ICurrentUserService> _mockUserService;
        private readonly Mock<DbSet<PlanTask>> _mockTaskDbSet;
        private readonly Mock<DbSet<PlanList>> _mockListDbSet;

        public TaskCreateTests()
        {
            var options = new DbContextOptionsBuilder<PlannerDbContext>().Options;
            _mockContext = new Mock<PlannerDbContext>(options);
            _mockUserService = new Mock<ICurrentUserService>();
            _mockTaskDbSet = new Mock<DbSet<PlanTask>>();
            _mockListDbSet = new Mock<DbSet<PlanList>>();

            _mockContext.Setup(c => c.Tasks).Returns(_mockTaskDbSet.Object);
            _mockContext.Setup(c => c.Lists).Returns(_mockListDbSet.Object);
        }

        [Fact]
        public async Task Handle_WhenCalledWithValidCommand_ShouldAddTaskAndSaveChanges()
        {
            // Arrange
            var testUserId = Guid.NewGuid();
            var testListId = Guid.NewGuid();
            var testWorkspaceId = Guid.NewGuid();
            var testSpaceId = Guid.NewGuid();

            var lists = new List<PlanList>
            {
                PlanList.Create("Test List", testWorkspaceId, testSpaceId, null, testUserId)
            };
            // Use reflection to set the protected Id property for the test
            var idProperty = typeof(Entity<Guid>).GetProperty("Id");
            idProperty.SetValue(lists[0], testListId);

            // Setup the mock DbSet to use our in-memory list and async helpers
            var mockableLists = lists.AsQueryable();
            _mockListDbSet.As<IQueryable<PlanList>>().Setup(m => m.Provider).Returns(new TestAsyncQueryProvider<PlanList>(mockableLists.Provider));
            _mockListDbSet.As<IQueryable<PlanList>>().Setup(m => m.Expression).Returns(mockableLists.Expression);
            _mockListDbSet.As<IQueryable<PlanList>>().Setup(m => m.ElementType).Returns(mockableLists.ElementType);
            _mockListDbSet.As<IAsyncEnumerable<PlanList>>().Setup(m => m.GetAsyncEnumerator(It.IsAny<CancellationToken>())).Returns(new TestAsyncEnumerator<PlanList>(mockableLists.GetEnumerator()));

            _mockTaskDbSet.Setup(d => d.AddAsync(It.IsAny<PlanTask>(), It.IsAny<CancellationToken>()))
                .Callback<PlanTask, CancellationToken>((t, c) => { });

            _mockUserService.Setup(s => s.CurrentUserId()).Returns(testUserId);
            var handler = new CreateTaskHandler(_mockContext.Object, _mockUserService.Object);
            var command = new CreateTaskRequest(
                name: "My First Test Task",
                description: "This is a description from a unit test.",
                priority: 1,
                status: null,
                startDate: null,
                dueDate: null,
                isPrivate: false,
                workspaceId: testWorkspaceId,
                spaceId: testSpaceId,
                folderId: null,
                listId: testListId
            );

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Value);
            _mockTaskDbSet.Verify(m => m.AddAsync(It.Is<PlanTask>(t => t.CreatorId == testUserId), It.IsAny<CancellationToken>()), Times.Once());
            _mockContext.Verify(m => m.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once());
        }

        [Fact]
        public async Task CreateTaskInList_WhenListExists_ShouldCreateTaskSuccessfully()
        {
            // Arrange
            var testUserId = Guid.NewGuid();
            var testListId = Guid.NewGuid();
            var mockList = PlanList.Create("Test List", Guid.NewGuid(), Guid.NewGuid(), null, testUserId);
            // Use reflection to set the protected Id property for the test
            var idProperty = typeof(Entity<Guid>).GetProperty("Id");
            idProperty.SetValue(mockList, testListId);

            // Setup FindAsync to work with the mock
            _mockListDbSet.Setup(m => m.FindAsync(new object[] { testListId }, It.IsAny<CancellationToken>()))
                          .ReturnsAsync(mockList);

            _mockUserService.Setup(s => s.CurrentUserId()).Returns(testUserId);
            var handler = new CreateTaskInListHandler(_mockContext.Object, _mockUserService.Object);
            var request = new CreateTaskInListRequest(
                name: "Task in a List",
                description: "A test task created within a list.",
                priority: 2,
                status: null,
                startDate: null,
                dueDate: null,
                isPrivate: false,
                listId: testListId
            );

            // Act
            var result = await handler.Handle(request, CancellationToken.None);

            // Assert
            Assert.True(result.IsSuccess, "The handler should have succeeded but returned a failure. This often happens if the DbSet property on the DbContext is not marked as 'virtual'.");
            Assert.NotNull(result.Value);
            _mockTaskDbSet.Verify(m => m.AddAsync(
                It.Is<PlanTask>(task =>
                    task.ListId == testListId &&
                    task.WorkspaceId == mockList.WorkspaceId),
                It.IsAny<CancellationToken>()),
                Times.Once());
            _mockContext.Verify(m => m.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once());
        }
    }
}
