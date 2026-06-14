


namespace CaSoft.Erp.USVector.Api
{
    public class ClAutorizationCommand
    {
        public static bool AutorizeJob(HttpRequest Request,Guid gJobId) 
        {
          //  bool Result = true; //doit etre a false en prod
             try
            {
                string? Authorization = Request.Headers["Authorization"];
                if (!string.IsNullOrWhiteSpace(Authorization))
                {
                    Guid token = new Guid(Authorization);
                    //Result = (ClDbLogin.GetLoginToken(token, gJobId) == null);

                }
            }
            catch (Exception)
            {

                //throw;
            }

           
            return true;
          //  return Result;
        }


    }
}
