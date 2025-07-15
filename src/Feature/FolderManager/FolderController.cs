using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace src.Feature.FolderManager
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public partial class FolderController : ControllerBase
    {
        private readonly IMediator _mediator;

        public FolderController(IMediator mediator)
        {
            _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        }
  
    }
}