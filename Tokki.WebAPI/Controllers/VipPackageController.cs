using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tokki.Application.UseCases.VipPackages.Commands.CreateVipPackage;
using Tokki.Application.UseCases.VipPackages.Commands.DeleteVipPackage;
using Tokki.Application.UseCases.VipPackages.Commands.UpdateVipPackage;
using Tokki.Application.UseCases.VipPackages.Queries.GetAllVipPackages;

namespace Tokki.WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class VipPackageController : ControllerBase
    {
        private readonly IMediator _mediator;

        public VipPackageController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpPost]
        [Authorize(Roles = "Admin")] 
        public async Task<IActionResult> Create([FromBody] CreateVipPackageCommand command)
        {
            var result = await _mediator.Send(command);
            return StatusCode(result.StatusCode, result);
        }

        [HttpGet]
        [AllowAnonymous] 
        public async Task<IActionResult> GetAllForUser()
        {
            var query = new GetAllVipPackagesQuery { IsAdmin = false };
            var result = await _mediator.Send(query);
            return StatusCode(result.StatusCode, result);
        }

        [HttpGet("admin")]
        [Authorize(Roles = "Admin")] 
        public async Task<IActionResult> GetAllForAdmin()
        {
            var query = new GetAllVipPackagesQuery { IsAdmin = true };
            var result = await _mediator.Send(query);
            return StatusCode(result.StatusCode, result);
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Update(string id, [FromBody] UpdateVipPackageCommand command)
        {
            command.Id = id;
            var result = await _mediator.Send(command);
            return StatusCode(result.StatusCode, result);
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")] 
        public async Task<IActionResult> Delete(string id)
        {
            var command = new DeleteVipPackageCommand { Id = id };
            var result = await _mediator.Send(command);
            return StatusCode(result.StatusCode, result);
        }
    }
}