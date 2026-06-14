using CaSoft.Framework;
using Microsoft.AspNetCore.Mvc;

namespace CaSoft.Erp.USVector.Api
{
    public class ClUseCaseHandler
    {
        IUseCase _useCase;
        ClWebApiPresenter _presenter;


        public ClUseCaseHandler( IUseCase UseCase)
        {
                        _useCase = UseCase;
            _presenter = new ClWebApiPresenter();
        }
             
        public ActionResult Execute() 
        {
            try
            {
              _useCase.Execute(_presenter);
                return _presenter.Result;
            }
            catch (Exception Ex)
            {

                return new BadRequestObjectResult (Ex.Message);
            }
        }
           
              

    }

   }
