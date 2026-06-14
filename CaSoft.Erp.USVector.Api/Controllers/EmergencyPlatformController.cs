
using EmergencyPlatformConnector;
using Microsoft.AspNetCore.Mvc;

namespace CaSoft.Erp.USVector.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EmergencyPlatformController : Controller
    {
        private IEmergencyConnector _platformConnector;
        public EmergencyPlatformController(IEmergencyConnector platformConnector)
        {
            _platformConnector = platformConnector;
        }

        [HttpPut]
        public IActionResult AddVehicleToTrackList(string strImmat)
        {
            _platformConnector.AddVehicleToTrackList(strImmat);
            return Ok();
            //return new ClResponseHandler(this, crewListResponse).Result();

        }
        [HttpDelete]
        public IActionResult RemoveVehicleFromTrackList( string strImmat)
        {
            _platformConnector.RemoveVehicleFromTrackList(strImmat);
            return Ok();
            //return new ClResponseHandler(this, crewListResponse).Result();

        }

        [HttpPatch]
        public IActionResult UpdateStatut( string strImmat, int intStatut)
        {
            _platformConnector.UpdateVehicleStatut(strImmat,intStatut);
            return Ok();
            //return new ClResponseHandler(this, crewListResponse).Result();

        }

        [HttpGet]
        public IActionResult UpdateLocation()
        {
            _platformConnector.UpdatePositionFromGeolocToPlatform();
            return Ok();
            //return new ClResponseHandler(this, crewListResponse).Result();
        }
    }
}
