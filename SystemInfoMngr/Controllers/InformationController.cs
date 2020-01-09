using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DarkSky.Models;
using DarkSky.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Runtime.InteropServices;
using static System.Math;

// For more information on enabling MVC for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace SystemInfoMngr.Controllers
{

    public class InformationController : Controller
    {
        public string PublicIP { get; set; } = "IP Lookup Failed";
        public double Long { get; set; }
        public double Lat { get; set; }
        public string City { get; set; }
        public string CurrentWeatherIcon { get; set; }
        public string WeatherAttribution { get; set; }
        public string CurrentTemp { get; set; } = "undetermined";
        public string DayWeatherSummary { get; set; }
        public string TempUnitOfMeasure { get; set; }
        private readonly IWebHostEnvironment _hostEnv;

        public InformationController(IWebHostEnvironment hostEnvironment)
        {
            _hostEnv = hostEnvironment;
        }

        public class LocationInfo
        {
            public string ip { get; set; }
            public string city { get; set; }
            public string region { get; set; }
            public string region_code { get; set; }
            public string country { get; set; }
            public string country_name { get; set; }
            public string postal { get; set; }
            public double latitude { get; set; }
            public double longtitude { get; set; }
            public string timezone { get; set; }
            public string asn { get; set; }
            public string org { get; set; }
        }

        private async Task GetLocationInfo()
        {
            var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Macintosh; Intel Mac OS X 10.13; rv:72.0) Gecko/20100101 Firefox/72.0");
            var urlString = "https://ipapi.co/json";

            // needs a full a cess
            string response = await httpClient.GetStringAsync(urlString);
            string jsonResponse = JsonConvert.DeserializeObject<string>(response);
            LocationInfo info = JsonConvert.DeserializeObject<LocationInfo>(jsonResponse);

            PublicIP = info.ip;
            Long = info.longtitude;
            Lat = info.latitude;
            City = info.city;
          
        }

        private OptionalParameters GetUnitOfMeasure()
        {
            bool blnMetric = RegionInfo.CurrentRegion.IsMetric;
            OptionalParameters optParams = new OptionalParameters();

            if(blnMetric)
            {
                optParams.MeasurementUnits = "si";
                TempUnitOfMeasure = "C";
            } else
            {
                optParams.MeasurementUnits = "us";
                TempUnitOfMeasure = "F";
            }

            return optParams;
        }

        private string GetCurrentWeatherIcon(Icon ic)
        {
            string iconFileName = string.Empty;

            switch(ic)
            {
                case Icon.ClearDay:
                    iconFileName = "Sun.svg";
                    break;
                case Icon.ClearNight:
                    iconFileName = "Moon.svg";
                    break;
                case Icon.Cloudy:
                    iconFileName = "Cloud.svg";
                    break;
                case Icon.Fog:
                    iconFileName = "Cloud-Fog.svg";
                    break;
                case Icon.PartlyCloudyDay:
                    iconFileName = "Cloud-Sun.svg";
                    break;
                case Icon.PartlyCloudyNight:
                    iconFileName = "Cloud-Moon.svg";
                    break;
                case Icon.Rain:
                    iconFileName = "Cloud-Rain.svg";
                    break;
                case Icon.Snow:
                    iconFileName = "Snowflake.svg";
                    break;
                case Icon.Wind:
                    iconFileName = "Wind.svg";
                    break;
                default:
                    iconFileName = "thremometer.svg";
                    break;
            }

            return iconFileName;

        }

        private async Task GetWeatherInfo()
        {
            string apiKey = "588c667ae7afae74c301cb4bd17926f9";
            DarkSkyService weather = new DarkSkyService(apiKey);
            OptionalParameters optParams = GetUnitOfMeasure();
            var foreCast = await weather.GetForecast(Lat, Long, optParams);
            string iconFileName = GetCurrentWeatherIcon(foreCast.Response.Currently.Icon);
            string svgFile = Path.Combine(_hostEnv.ContentRootPath, "climacons", iconFileName);
            CurrentWeatherIcon = System.IO.File.ReadAllText($"{svgFile}");

            WeatherAttribution = foreCast.AttributionLine;
            DayWeatherSummary = foreCast.Response.Daily.Summary;

            if(foreCast.Response.Currently.Temperature.HasValue)
            {
                CurrentTemp = Round(foreCast.Response.Currently.Temperature.Value, 0).ToString();
             }
        }

        // GET: /<controller>/
        public IActionResult GetInfo()
        {
            Models.InformationModel model = new Models.InformationModel();
            model.OperatingSystem = RuntimeInformation.OSDescription;
            model.FrameworkDescription = RuntimeInformation.FrameworkDescription;
            model.OSArchitecture = RuntimeInformation.OSArchitecture.ToString();
            model.ProcessArchitecture = RuntimeInformation.ProcessArchitecture.ToString();
            string title = string.Empty;
            string OSArchitechture = string.Empty;

            OSArchitechture = model.OSArchitecture.ToUpper().Equals("X64") ? "64-bit" : "32-bit";
            if(RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                title = $"Windows {OSArchitechture}";
            } else if(RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            { 
                title = $"OSX {OSArchitechture}";
            } else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                title = $"Linux {OSArchitechture}";
            }

            GetLocationInfo().Wait();

            model.IPAddressString = PublicIP;
            model.CurrentIcon = CurrentWeatherIcon;
            model.WheatherBy = WeatherAttribution;
            model.CurrentTemperature = CurrentTemp;
            model.DailySummary = DayWeatherSummary;
            model.CurrentCity = City;
            model.UnitOfMeasure = TempUnitOfMeasure;

            model.InfoTitle = title;

            return View(model);
        }
    }
}
