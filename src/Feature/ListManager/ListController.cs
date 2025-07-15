using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace src.Feature.ListManager
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public partial class ListController : ControllerBase
    {
        private readonly IMediator _mediator;

        public ListController(IMediator mediator)
        {
            _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        }
    }
}