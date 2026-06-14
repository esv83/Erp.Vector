using CaSoft.Erp.USVector.Application;
using Microsoft.AspNetCore.Mvc;

namespace CaSoft.Erp.USVector.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ContactController : Controller


    {
        [HttpGet]
        public IActionResult GetContacts(string FullSearchName, [FromServices] IContactRepository repos)
        {
            try
            {
                             return Ok(repos.GetContactList(FullSearchName));
            }
            catch (Exception ex)
            {

                return BadRequest(ex.Message);
            }

        }
        //[HttpGet]
        //public IActionResult GetContacts(string strName, string? strFirstName)
        //{
        //    try
        //    {
        //        IContactRepository repos = Factory.ClContactRepositoryFactory.GetContactRepository(1);
        //        return Ok(repos.GetContactList(strName, strFirstName));
        //    }
        //    catch (Exception ex)
        //    {

        //        return BadRequest(ex.Message);
        //    }

        //}


        [HttpPatch]
        public ActionResult UpdateContact([FromBody] ClContactModel ContactPatchModel, [FromServices] IContactRepository repository)
        {
            try
            {
                repository.UpdateContact(ContactPatchModel.ToJobBeneficiary());
                return Ok();    
            }
            catch (Exception ex)
            {

                return BadRequest(ex.Message);
            }


        }
    }
}
