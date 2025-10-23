using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace TotalRecall.Api.Controllers
{
    [Route("api/search")]
    [ApiController]
    public class SearchController : ControllerBase
    {
        [HttpGet]
        public ActionResult<string> SayHello()
        {
            return "Hello";
        }
    }
}
