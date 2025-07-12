using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace src.Feature.Workspace
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public partial class WorkspaceController : ControllerBase
    {
        private readonly IMediator _mediator;

        public WorkspaceController(IMediator mediator)
        {
            _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        }
    } 
}
