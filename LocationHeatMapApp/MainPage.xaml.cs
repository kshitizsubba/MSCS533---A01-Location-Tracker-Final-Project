using Microsoft.Maui.Maps;
using Microsoft.Maui.Devices.Sensors;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls.Maps;
using LocationHeatMapApp.Models;
using LocationHeatMapApp.Services;
using Microsoft.Maui.Graphics;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

namespace LocationHeatMapApp;

public partial class MainPage : ContentPage
{
    LocationService locationService;
    DatabaseService databaseService;

    bool dummyDataAdded = false;

    public MainPage()
    {
        InitializeComponent();

        locationService = new LocationService();

        // Build SQLite database path
        string dbPath = Path.Combine(FileSystem.AppDataDirectory, "locations.db3");
        databaseService = new DatabaseService(dbPath);

        // Show current location with pin
        RequestLocationAndShowPin();

        // Load simple dots from saved locations
        _ = LoadAndDisplayDotsAsync();

        // Start periodic tracking every 30 seconds
        StartTrackingLocation();
    }

    async void RequestLocationAndShowPin()
    {
        try
        {
            var status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();

            if (status != PermissionStatus.Granted)
            {
                status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
            }

            if (status == PermissionStatus.Granted)
            {
                var location = await Geolocation.GetLastKnownLocationAsync();

                if (location == null)
                    location = await Geolocation.GetLocationAsync(new GeolocationRequest(GeolocationAccuracy.Best));

                if (location != null)
                {
                    var userPosition = new Location(location.Latitude, location.Longitude);

                    MyMap.MoveToRegion(MapSpan.FromCenterAndRadius(userPosition, Distance.FromMiles(1)));

                    var pin = new Pin
                    {
                        Label = "You are here",
                        Location = userPosition,
                        Type = PinType.Place
                    };
                    MyMap.Pins.Add(pin);

                    // Add scattered dummy data near the user location only once
                    if (!dummyDataAdded)
                    {
                        var random = new Random();
                        var dummyLocations = new List<UserLocation>();

                        for (int i = 0; i < 15; i++) // 15 scattered points
                        {
                            double latOffset = (random.NextDouble() - 0.5) / 100;   // ±0.005 degrees (~500m)
                            double lonOffset = (random.NextDouble() - 0.5) / 100;

                            dummyLocations.Add(new UserLocation
                            {
                                Latitude = userPosition.Latitude + latOffset,
                                Longitude = userPosition.Longitude + lonOffset,
                                Timestamp = DateTime.UtcNow
                            });
                        }

                        foreach (var dummy in dummyLocations)
                        {
                            await databaseService.InsertLocationAsync(dummy);
                        }

                        dummyDataAdded = true;

                        // Refresh dots with new dummy locations
                        await LoadAndDisplayDotsAsync();
                    }
                }
                else
                {
                    await DisplayAlert("Error", "Unable to get location", "OK");
                }
            }
            else
            {
                await DisplayAlert("Permission Denied", "Location permission was denied.", "OK");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", ex.Message, "OK");
        }
    }

    private void StartTrackingLocation()
    {
        Device.StartTimer(TimeSpan.FromSeconds(30), () =>
        {
            _ = TrackAndSaveLocationAsync();
            return true; // run repeatedly
        });
    }

    private async Task TrackAndSaveLocationAsync()
    {
        bool hasPermission = await locationService.CheckAndRequestLocationPermissionAsync();
        if (!hasPermission)
        {
            await DisplayAlert("Permission Denied", "Location permission is required.", "OK");
            return;
        }

        var location = await locationService.GetCurrentLocationAsync();
        if (location != null)
        {
            var userLocation = new UserLocation
            {
                Latitude = location.Latitude,
                Longitude = location.Longitude,
                Timestamp = DateTime.UtcNow
            };

            await databaseService.InsertLocationAsync(userLocation);

            await LoadAndDisplayDotsAsync();
        }
    }

    private async Task LoadAndDisplayDotsAsync()
    {
        var locations = await databaseService.GetLocationsAsync();

        MyMap.MapElements.Clear(); // clear old dots

        if (locations == null || locations.Count == 0)
            return;

        // Group nearby locations and create dots
        var clusteredLocations = ClusterNearbyLocations(locations);

        foreach (var cluster in clusteredLocations)
        {
            CreateLocationDot(cluster);
        }
    }

    private List<LocationCluster> ClusterNearbyLocations(List<UserLocation> locations)
    {
        var clusters = new List<LocationCluster>();
        const double clusterRadius = 0.0005; // ~55 meters - locations closer than this get grouped

        foreach (var location in locations)
        {
            // Find existing cluster within radius
            var nearbyCluster = clusters.FirstOrDefault(c => 
                GetDistance(c.CenterLatitude, c.CenterLongitude, location.Latitude, location.Longitude) < clusterRadius);

            if (nearbyCluster != null)
            {
                // Add to existing cluster
                nearbyCluster.LocationCount++;
                // Optionally update center to average position
                nearbyCluster.CenterLatitude = (nearbyCluster.CenterLatitude + location.Latitude) / 2;
                nearbyCluster.CenterLongitude = (nearbyCluster.CenterLongitude + location.Longitude) / 2;
            }
            else
            {
                // Create new cluster
                clusters.Add(new LocationCluster
                {
                    CenterLatitude = location.Latitude,
                    CenterLongitude = location.Longitude,
                    LocationCount = 1
                });
            }
        }

        return clusters;
    }

    private void CreateLocationDot(LocationCluster cluster)
    {
        // Fixed small size like map apps (about 8-10 meters radius)
        const double dotRadius = 8.0; // meters
        
        // Determine if high intensity (red border)
        bool isHighIntensity = cluster.LocationCount >= 3; // 3+ visits = high intensity

        var fillColor = Colors.Blue.WithAlpha(0.7f);
        var strokeColor = isHighIntensity ? Colors.Red : Colors.Blue;
        var strokeWidth = isHighIntensity ? 2.0f : 1.0f;

        var circle = new Circle
        {
            Center = new Location(cluster.CenterLatitude, cluster.CenterLongitude),
            Radius = Distance.FromMeters(dotRadius),
            FillColor = fillColor,
            StrokeColor = strokeColor,
            StrokeWidth = strokeWidth
        };

        MyMap.MapElements.Add(circle);
    }

    private double GetDistance(double lat1, double lon1, double lat2, double lon2)
    {
        // Simple distance calculation using Pythagorean theorem
        // For more accuracy, use Haversine formula
        var deltaLat = lat2 - lat1;
        var deltaLon = lon2 - lon1;
        return Math.Sqrt(deltaLat * deltaLat + deltaLon * deltaLon);
    }
}

// Helper class for location clustering
public class LocationCluster
{
    public double CenterLatitude { get; set; }
    public double CenterLongitude { get; set; }
    public int LocationCount { get; set; }
}