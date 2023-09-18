using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class GitController : ControllerBase
{

    [HttpGet]
    public IActionResult Get()
    {
        return Ok(new
        {
            message = "Hello World!"
        });
    }
}
