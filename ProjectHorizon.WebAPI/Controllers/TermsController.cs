using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ProjectHorizon.ApplicationCore.Interfaces;
using ProjectHorizon.ApplicationCore.Options;
using System.Threading.Tasks;

namespace ProjectHorizon.WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]/[action]")]
    public class TermsController : HorizonBaseController
    {
        private readonly ITermsService _termsAndConditionsService;

        public TermsController(ITermsService termsAndConditionsService)
        {
            _termsAndConditionsService = termsAndConditionsService;
        }

        [HttpGet]
        [ProducesResponseType(typeof(ApplicationInformation), StatusCodes.Status200OK)]
        public async Task<IActionResult> CheckAccepted()
        {
            bool accepted = await _termsAndConditionsService.CheckAcceptedTermsLastVersionAsync();

            return Ok(accepted);
        }

        [HttpPost]
        [ProducesResponseType(typeof(ApplicationInformation), StatusCodes.Status200OK)]
        public async Task<IActionResult> Accept()
        {
            int statusCode = await _termsAndConditionsService.AcceptTermsAsync();

            if (statusCode == StatusCodes.Status400BadRequest)
            {
                return BadRequest();
            }

            return Ok();
        }
    }
}
