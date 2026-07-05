using Microsoft.AspNetCore.Mvc;

namespace CaSoft.Erp.USVector.Api.Controllers
{
    using CaSoft.Erp.USVector.Api.Infrastructure;
    using CaSoft.Erp.USVector.Application;
    using Microsoft.AspNetCore.Mvc;
    using System;
    using System.Collections.Generic;
    using System.Linq;

    
        [ApiController]
        [Route("analyze")]
        public class AnalyzeController : ControllerBase
        {
        private ILogAnalyzeRepository _repository;
        private ClMechanicService _mechanicService;
       

        public AnalyzeController( ILogAnalyzeRepository repository)
        {
            _repository = repository;
            _mechanicService = new ClMechanicService(_repository);
        }

        [HttpGet("{intlogId}")]
            public ActionResult<ClGetLogAnalyzeModel> GetAnalyze(int intlogId)
            {
         

            return new ClGetLogAnalyzeUseCase(intlogId, _repository).Handle().ToActionResult();

        }

            [HttpPost]
            public IActionResult AddAnalyze([FromBody] ClEditLogAnalyzeModel model)
            {

            ClNoResponseHandler result= _mechanicService.InsertAnalyze(model);
            if (result.IsSuccess)
            {
              return  Ok();
            }
            else
            {
               return BadRequest(result.ErrorText);
            }
            //    ClInsertLogAnalyzeUseCase useCase = new ClInsertLogAnalyzeUseCase(model, _repository);
            //ClUseCaseHandler handler = new ClUseCaseHandler(useCase);

            //return handler.Execute();
        }

            [HttpPut]
            public IActionResult UpdateAnalyze([FromBody] ClEditLogAnalyzeModel model)
            {


            ClNoResponseHandler result = _mechanicService.UpdateAnalyze(model);
            if (result.IsSuccess)
            {
                return Ok();
            }
            else
            {
                return BadRequest(result.ErrorText);
            }

            //ClUpdateLogAnalyzeUseCase useCase = new ClUpdateLogAnalyzeUseCase(model, _repository);
            //ClUseCaseHandler handler = new ClUseCaseHandler(useCase);

            //return handler.Execute();
        }

            //[HttpPatch("{logId}")]
            //public IActionResult PatchAnalyze(int logId, [FromBody] Dictionary<string, object> updates)
            //{
            //    var analyze = _analyzes.FirstOrDefault(a => a.LogId == logId);
            //    if (analyze == null)
            //        return NotFound();

            //    foreach (var update in updates)
            //    {
            //        var prop = typeof(ClLogAnalyzeModel).GetProperty(update.Key);
            //        if (prop != null && prop.CanWrite)
            //        {
            //            var value = Convert.ChangeType(update.Value, Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType);
            //            prop.SetValue(analyze, value);
            //        }
            //    }

            //    return NoContent();
            //}


        [HttpDelete("{logId}")]
        public IActionResult DeleteAnalyze(int intLogId)
        {


            ClNoResponseHandler result = _mechanicService.DeleteAnalyze(intLogId);
            if (result.IsSuccess)
            {
                return Ok();
            }
            else
            {
                return BadRequest(result.ErrorText);
            }

            //ClDeleteLogAnalyzeUseCase useCase = new ClDeleteLogAnalyzeUseCase(logId, _repository);
            //ClUseCaseHandler handler = new ClUseCaseHandler(useCase);

            //return handler.Execute();
        }

        [HttpDelete("{logId}/actions/{actionId}")]
        public IActionResult DeleteAction(int logId, int actionId)
        {
            //var analyze = _analyzes.FirstOrDefault(a => a.LogId == logId);
            //if (analyze == null)
            //    return NotFound("Analyse non trouvée.");

            //var action = analyze.Actions.FirstOrDefault(a => a.Id == actionId);
            //if (action == null)
            //    return NotFound("Action non trouvée.");

            //analyze.Actions.Remove(action);
            return NoContent();
        }

    }
    }

