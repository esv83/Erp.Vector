using Microsoft.AspNetCore.Mvc;

namespace CaSoft.Erp.USVector.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DocumentController : Controller
    {
        [HttpGet("{DocPrefix}")]
        public IActionResult Index(Guid DocId)
        {
            Byte[] fileBytes = System.IO.File.ReadAllBytes(@"Sample.pdf");
            var content = Convert.ToBase64String(fileBytes);
            return Ok(content);
        }
    }
}
