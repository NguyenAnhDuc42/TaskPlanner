using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BaseController : ControllerBase
    {
        protected readonly IMediator _mediator;

        protected BaseController(IMediator mediator)
        {
            _mediator = mediator;
        }

        // Example helper method
        protected async Task<IActionResult> SendRequest<TResponse>(IRequest<TResponse> request)
        {
            var response = await _mediator.Send(request);
            return Ok(response);
        }
    }
}
