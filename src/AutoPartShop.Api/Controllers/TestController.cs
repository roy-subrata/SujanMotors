

using Microsoft.AspNetCore.Mvc;

namespace AutoPartShop.API.Controllers;
[ApiController]
    [Route("api/[controller]")]
    public class TestController : ControllerBase
    {
        /// <summary>
        /// Checking test endpoint
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public IActionResult Get()
        {
            return Ok("I am test controller");
        }
    }