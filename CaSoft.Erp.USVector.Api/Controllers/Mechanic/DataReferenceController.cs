using CaSoft.Erp.USVector.Application;
using Microsoft.AspNetCore.Mvc;

namespace CaSoft.Erp.USVector.Api.Controllers
{
  

        [ApiController]
    [Route("api/[controller]")]
    public class ReferenceDataController : ControllerBase
        {
            [HttpGet("actors")]
            public ActionResult<List<ClReferenceItem>> GetActors()
            {
                var list = new List<ClReferenceItem>
            {
                new ClReferenceItem(1, "Garage du gros pin"),
                new ClReferenceItem(2, "Natalino DI VOZZO"),
                new ClReferenceItem(3, "Christophe POUGEUX"),
                  new ClReferenceItem(4, "FirstStop")
            };
                return Ok(list);
            }

            [HttpGet("actions")]
            public ActionResult<List<ClReferenceItem>> GetActionTypes()
            {
                var list = new List<ClReferenceItem>
            {
                new ClReferenceItem(1, "Controle technique"),
                new ClReferenceItem(2, "Réparation"),
                new ClReferenceItem(3, "Avis")
            };
                return Ok(list);
            }

            [HttpGet("constraints")]
            public ActionResult<List<ClConstraintModel>> GetConstraints()
            {
                var list = new List<ClConstraintModel>
            {
                new ClConstraintModel(1, "Avant",true),
                    new ClConstraintModel(2, "A la prochaine revision",false),
                new ClConstraintModel(4, "Dés que possible", false),
                new ClConstraintModel(3, "Aucune", false)
            };
                return Ok(list);
            }

            [HttpGet("nature")]
            public ActionResult<List<ClReferenceItem>> GetNature()
            {
                var list = new List<ClReferenceItem>
            {
                new ClReferenceItem(1, "Casse"),
                new ClReferenceItem(2, "Usure"),
                 new ClReferenceItem(4, "Perte / disparition"),
                new ClReferenceItem(3, "Maintenance programmée")
            };
                return Ok(list);
            }

            [HttpGet("concerning")]
            public ActionResult<List<ClReferenceItem>> GetConcerning()
            {
                var list = new List<ClReferenceItem>
            {
                new ClReferenceItem(1, "Véhicule"),
                new ClReferenceItem(2, "Materiel"),
               
            };
                return Ok(list);
            }
        }
    }

