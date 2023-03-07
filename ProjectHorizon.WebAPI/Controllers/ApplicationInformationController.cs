using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using ProjectHorizon.ApplicationCore.Constants;
using ProjectHorizon.ApplicationCore.Options;

namespace ProjectHorizon.WebAPI.Controllers
{
    [Route("api/[controller]")]
    [EnableCors(CorsConstants.ShopPolicy)]
    [ApiController]
    public class ApplicationInformationController : HorizonBaseController
    {
        private readonly ApplicationInformation _applicationInformation;

        public ApplicationInformationController(IOptions<ApplicationInformation> applicationInformation)
        {
            _applicationInformation = applicationInformation.Value;
        }

        [AllowAnonymous]
        [HttpGet]
        [ProducesResponseType(typeof(ApplicationInformation), StatusCodes.Status200OK)]
        public IActionResult Get()
        {
            return Ok(_applicationInformation);
        }
    }
}
