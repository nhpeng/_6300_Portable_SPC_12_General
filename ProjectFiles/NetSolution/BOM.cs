/*
 * This C# code is designed to fetch and process weather data from the OpenWeather API for Laverton, Victoria, Australia.
 * It is implemented as part of a FactoryTalk Optix project, demonstrating the integration of external data sources with industrial automation software.
 *
 * Key Components:
 * 1. HttpClient: Used to make asynchronous HTTP requests to the OpenWeather API.
 * 2. Timer: A System.Timers.Timer is used to periodically fetch weather data every minute.
 * 3. JSON Parsing: The fetched weather data is parsed using Newtonsoft.Json.Linq to extract relevant information.
 * 4. Variable Initialization: The code checks and initializes variables for temperature, humidity, pressure, wind speed, and error messages.
 * 5. Data Conversion: The temperature is converted from Kelvin to Celsius, and the timestamp is converted from Unix time to AEST.
 *
 * Main Methods:
 * - Start(): Initializes the timer and starts the periodic fetching of weather data if the bRunBOM variable is true.
 * - Stop(): Stops and disposes of the timer to clean up resources.
 * - OnWeatherTimerElapsed(): Triggered by the timer to fetch weather data.
 * - FetchWeatherData(): Makes an HTTP GET request to the OpenWeather API and processes the response.
 * - ProcessWeatherData(): Parses the JSON response, extracts weather data, and updates the corresponding variables.
 *
 * This code showcases the power and flexibility of the C# engine behind industrial software, allowing for real-time integration of external data sources.
 * Written as is where is SClark NHP.
 */
#region Using directives
using System;
using UAManagedCore;
using OpcUa = UAManagedCore.OpcUa;
using FTOptix.SQLiteStore;
using FTOptix.HMIProject;
using FTOptix.NetLogic;
using FTOptix.UI;
using FTOptix.DataLogger;
using FTOptix.NativeUI;
using FTOptix.Store;
using FTOptix.Report;
using FTOptix.OPCUAClient;
using FTOptix.RAEtherNetIP;
using FTOptix.Retentivity;
using FTOptix.CoreBase;
using FTOptix.CommunicationDriver;
using FTOptix.System;
using FTOptix.Core;
using FTOptix.WebUI;
using FTOptix.OPCUAServer;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using System.Diagnostics;
#endregion

using System.Timers;
using FTOptix.Alarm;

public class BOM : BaseNetLogic
{
    private static readonly HttpClient client = new HttpClient();
    private Timer weatherTimer;

    public override void Start()
    {
        Log.Info("Start method called.");

        var auth = Project.Current.Authentication;
        Log.Info($"Authentication mode: {auth}");

        var bRun = LogicObject.GetVariable("bRunBOM").Value;
        Log.Info($"bRunBOM: {bRun}");

        if (bRun == true)
        {
            Log.Info("Starting FetchWeatherData...");
            FetchWeatherData().Wait();

            // Initialize and start the timer
            weatherTimer = new Timer(600000); // 600000 milliseconds = 10 minutes
            weatherTimer.Elapsed += OnWeatherTimerElapsed;
            weatherTimer.AutoReset = true;
            weatherTimer.Enabled = true;
        }
        else
        {
            Log.Info("bRunBOM is false. Exiting Start method.");
        }

        Log.Info("Start method completed.");
    }

    public override void Stop()
    {
        // Stop the timer
        if (weatherTimer != null)
        {
            weatherTimer.Stop();
            weatherTimer.Dispose();
        }
        Log.Info("Stop method called.");
    }

    private void OnWeatherTimerElapsed(object sender, ElapsedEventArgs e)
    {
        Log.Info("Timer elapsed. Fetching weather data...");
        FetchWeatherData().Wait();
    }

    private async Task FetchWeatherData()
    {
        try
        {
            string apiKey = "7d43c117e00e8777507ed52d380dd293"; // Replace with your OpenWeather API key
            string url = $"http://api.openweathermap.org/data/2.5/weather?q=Laverton&appid={apiKey}";
            Log.Info($"Fetching data from URL: {url}");

            // Set User-Agent header
            client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/58.0.3029.110 Safari/537.3");

            HttpResponseMessage response = await client.GetAsync(url);
            response.EnsureSuccessStatusCode();
            string responseBody = await response.Content.ReadAsStringAsync();
            Log.Info("Data fetched successfully.");

            JObject weatherData = JObject.Parse(responseBody);
            ProcessWeatherData(weatherData);
        }
        catch (HttpRequestException e)
        {
            var errorTag = LogicObject.GetVariable("ErrorMessage");
            if (errorTag != null)
            {
                errorTag.Value = $"Exception Caught! Message: {e.Message}";
            }
            Log.Info($"Exception Caught! Message: {e.Message}");
        }
    }

    private void ProcessWeatherData(JObject weatherData)
    {
        Log.Info("ProcessWeatherData method called.");
        var main = weatherData["main"];
        var wind = weatherData["wind"];
        var timestamp = weatherData["dt"];
        if (main != null && wind != null && timestamp != null)
        {
            var temperatureTag = LogicObject.GetVariable("Temperature");
            var humidityTag = LogicObject.GetVariable("Humidity");
            var pressureTag = LogicObject.GetVariable("Pressure");
            var windSpeedTag = LogicObject.GetVariable("WindSpeed");
            var timestampTag = LogicObject.GetVariable("Timestamp");
            var errorMessageTag = LogicObject.GetVariable("ErrorMessage");

            if (temperatureTag == null)
            {
                Log.Info("Temperature tag is not initialized.");
            }
            else
            {
                Log.Info("Temperature tag is initialized.");
            }

            if (humidityTag == null)
            {
                Log.Info("Humidity tag is not initialized.");
            }
            else
            {
                Log.Info("Humidity tag is initialized.");
            }

            if (pressureTag == null)
            {
                Log.Info("Pressure tag is not initialized.");
            }
            else
            {
                Log.Info("Pressure tag is initialized.");
            }

            if (windSpeedTag == null)
            {
                Log.Info("WindSpeed tag is not initialized.");
            }
            else
            {
                Log.Info("WindSpeed tag is initialized.");
            }

            if (timestampTag == null)
            {
                Log.Info("Timestamp tag is not initialized.");
            }
            else
            {
                Log.Info("Timestamp tag is initialized.");
            }

            if (errorMessageTag == null)
            {
                Log.Info("ErrorMessage tag is not initialized.");
            }
            else
            {
                Log.Info("ErrorMessage tag is initialized.");
            }

            if (temperatureTag != null && humidityTag != null && pressureTag != null && windSpeedTag != null && timestampTag != null)
            {
                double temperature = main["temp"].ToObject<double>();
                double humidity = main["humidity"].ToObject<double>();
                double pressure = main["pressure"].ToObject<double>();
                double windSpeed = wind["speed"].ToObject<double>();
                long unixTimestamp = timestamp.ToObject<long>();

                // Convert Unix timestamp to DateTime in UTC
                DateTime utcDateTime = DateTimeOffset.FromUnixTimeSeconds(unixTimestamp).UtcDateTime;

                // Convert UTC DateTime to AEST (UTC+10)
                TimeZoneInfo aestTimeZone = TimeZoneInfo.FindSystemTimeZoneById("AUS Eastern Standard Time");
                DateTime aestDateTime = TimeZoneInfo.ConvertTimeFromUtc(utcDateTime, aestTimeZone);

                Log.Info($"Raw Temperature (Kelvin): {temperature}");
                Log.Info($"Raw Humidity: {humidity}");
                Log.Info($"Raw Pressure: {pressure}");
                Log.Info($"Raw Wind Speed: {windSpeed}");
                Log.Info($"Data Timestamp (UTC): {utcDateTime}");
                Log.Info($"Data Timestamp (AEST): {aestDateTime}");

                temperatureTag.Value = (float)(temperature - 273.15); // Convert from Kelvin to Celsius and cast to float
                humidityTag.Value = (float)humidity;
                pressureTag.Value = (float)pressure;
                windSpeedTag.Value = (float)windSpeed;
                timestampTag.Value = aestDateTime.ToString("yyyy-MM-dd HH:mm:ss");

                Log.Info($"Converted Temperature: {temperatureTag.Value}°C");
                Log.Info($"Humidity: {humidityTag.Value}%");
                Log.Info($"Pressure: {pressureTag.Value} hPa");
                Log.Info($"Wind Speed: {windSpeedTag.Value} m/s");
                Log.Info($"Timestamp: {timestampTag.Value}");
            }
            else
            {
                Log.Info("One or more tags are not initialized.");
            }
        }
        else
        {
            Log.Info("No data found in weather response.");
        }
        Log.Info("ProcessWeatherData method completed.");
    }
}
