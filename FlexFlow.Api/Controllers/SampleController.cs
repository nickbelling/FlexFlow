using System;
using System.Linq;
using FlexFlow.Api.Database;
using FlexFlow.Data.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace FlexFlow.Api.Controllers
{
    [Route("api")]
    [ApiController]
    public class SampleController : ControllerBase
    {
        private readonly FlexFlowContext _context;
        private readonly UserManager<User> _userManager;

        public SampleController(
            FlexFlowContext context,
            UserManager<User> userManager )
        {
            _context = context;
            _userManager = userManager;
        }

        [HttpGet("test")]
        public ActionResult<string> Test()
        {
            return Ok($"Current time: {DateTime.Now}");
        }
    }
}
