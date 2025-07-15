using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace src.Feature.SpaceManager
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public partial class SpaceController : ControllerBase
    {
         private readonly IMediator _mediator;

        public SpaceController(IMediator mediator)
        {
            _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        }
    }
}
