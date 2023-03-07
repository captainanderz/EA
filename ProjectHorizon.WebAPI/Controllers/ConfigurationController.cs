using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace ProjectHorizon.WebAPI.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class ConfigurationController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public ConfigurationController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [AllowAnonymous]
        [HttpGet]
        [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
        public ActionResult<string> ApplicationInsightsConnectionString()
        {
            return Ok(_configuration["ApplicationInsights:ConnectionString"]);
        }
    }
}
