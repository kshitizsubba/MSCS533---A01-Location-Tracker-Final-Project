using Microsoft.Maui.Devices.Sensors;
using System.Threading.Tasks;

namespace LocationHeatMapApp.Services
{
    public class LocationService
    {
        public async Task<bool> CheckAndRequestLocationPermissionAsync()
        {
            var status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
            if (status != PermissionStatus.Granted)
            {
                status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
            }
            return status == PermissionStatus.Granted;
        }

        public async Task<Location?> GetCurrentLocationAsync()
        {
            return await Geolocation.GetLocationAsync(new GeolocationRequest(GeolocationAccuracy.Best));
        }
    }
}
