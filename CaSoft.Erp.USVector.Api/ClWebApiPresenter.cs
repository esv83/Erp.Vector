using CaSoft.Framework;
using CaSoft.Erp.USVector.Application;
using Microsoft.AspNetCore.Mvc;

namespace CaSoft.Erp.USVector.Api
{
    public class ClWebApiPresenter : IResponseHandler
    {
        public ClWebApiPresenter()
        {
            Result = new BadRequestObjectResult("La requete n'a pas été présenté au serveur");
        }

        public void Handle(ClUseCaseResponseBase response)
        {
            if (response.IsSuccess)
            {
                if (response.Data is null)
                {
                    Result = new NotFoundResult();
                }
                else
                {
                    Result = new OkObjectResult(response.Data);
                }

               
            }
           
            else
            {
                Result = new BadRequestObjectResult(response.ErrorText);
            }
        }

        public ActionResult Result { get; private set; }

        public static ClWebApiPresenter GetPresenter() { return new ClWebApiPresenter(); }
    }
}
