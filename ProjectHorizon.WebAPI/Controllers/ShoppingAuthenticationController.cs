using Microsoft.AspNetCore.Mvc;

namespace ProjectHorizon.WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ShoppingAuthenticationController : HorizonBaseController
    {
        //private readonly IShoppingAuthenticationService _shoppingAuthenticationService;

        //public ShoppingAuthenticationController(IShoppingAuthenticationService shoppingAuthenticationService)
        //{
        //    _shoppingAuthenticationService = shoppingAuthenticationService;
        //}

        //[HttpGet]
        //[ProducesResponseType(StatusCodes.Status200OK)]
        //[ProducesResponseType(StatusCodes.Status400BadRequest)]
        //public async Task<IActionResult> Login()
        //{
        //    HttpContext.VerifyUserHasAnyAcceptedScope(scopeRequiredByApi);
        //    string email = User.Identity.Name;

        //    Response<UserDto> response = await _shoppingAuthenticationService.LoginAsync();

        //    if (response.IsSuccessful)
        //        SetRefreshTokenCookie(response.Dto.RefreshToken);

        //    return Ok(response);
        //}
    }
}
