using MediatR;
using Microsoft.AspNetCore.Mvc;
using Tokki.Application.UseCases.VipPackages.Commands.CreateVipPackage;
using Tokki.Application.UseCases.VipPackages.Commands.DeleteVipPackage;
using Tokki.Application.UseCases.VipPackages.Commands.UpdateVipPackage;
using Tokki.Application.UseCases.VipPackages.Queries.GetAllVipPackages;

namespace Tokki.WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class VipPackageController : ControllerBase
    {
        private readonly IMediator _mediator;

        public VipPackageController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllForUser()
        {
            var query = new GetAllVipPackagesQuery { IsAdmin = false };
            var result = await _mediator.Send(query);
            return Ok(result);
        }

        [HttpGet("admin")]
        public async Task<IActionResult> GetAllForAdmin()
        {
            var query = new GetAllVipPackagesQuery { IsAdmin = true };
            var result = await _mediator.Send(query);
            return Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateVipPackageCommand command)
        {
            var result = await _mediator.Send(command);
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(string id, [FromBody] UpdateVipPackageCommand command)
        {
            command.Id = id; 
            var result = await _mediator.Send(command);
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            var command = new DeleteVipPackageCommand { Id = id };
            var result = await _mediator.Send(command);
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }
    }
}