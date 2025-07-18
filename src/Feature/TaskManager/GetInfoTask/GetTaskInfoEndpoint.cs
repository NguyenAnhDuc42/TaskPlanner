using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using src.Feature.TaskManager.GetInfoTask;
using src.Helper.Results;

namespace src.Feature.TaskManager
{
    public partial class TaskController
    {
        [HttpGet("{id}")]
        [Authorize(Policy = "ViewTask")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var request = new GetTaskInfoRequest(id);
            var result = await _mediator.Send(request);
            return result.ToApiResult();
        }
    }
}
