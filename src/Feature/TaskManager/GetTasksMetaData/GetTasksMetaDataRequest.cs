using MediatR;
using Microsoft.AspNetCore.Mvc.RazorPages;
using src.Contract;
using src.Helper.Filters;
using src.Helper.Results;

namespace src.Feature.TaskManager.GetTasksMetaData;

public record class GetTasksMetaDataRequest(TaskQuery query) : IRequest<TasksMetadata>;

