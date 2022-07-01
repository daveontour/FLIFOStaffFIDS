using FLIFOStaffFIDSCommon;
using Microsoft.AspNetCore.SignalR;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NLog;
using System.Net;
using System.Text;
using System.Timers;
using System.Xml;

namespace FLIFOStaffFIDSBlazorServerApp.Shared.Services;

public class FlightStatusService : BackgroundService
{
    public static Action OnServerFlightsUpdates;
    public static Action OnServerNoFlightsUpdates;
    public string? InitTemplate { get; private set; }
    public string? UpdateTemplateURL { get; private set; }
    private int UpdateInterval { get; set; }
    private int LoadInterval { get; set; }
    private bool EnableUpdates { get; set; } = true;

    private readonly List<FieldMapping> fieldMappings = new List<FieldMapping>();

    private static readonly Logger logger = LogManager.GetLogger("consoleLogger");
    private readonly Logger arrLogger = LogManager.GetLogger("arrivalLogger");
    private readonly Logger depLogger = LogManager.GetLogger("depLogger");

    private readonly List<Airport> airports = new List<Airport>();

    private System.Timers.Timer updateTimer;
    private System.Timers.Timer reloadTimer;
    public DataAccessService dao;
    private bool Test = false;
    private string TestDepartures;
    private string TestArrivals;
    private string TestDeparturesUpdates;
    private string TestArrivalsUpdates;

    //private IClientStateController clientController;

    public FlightStatusService(DataAccessService dao)
    {
        this.dao = dao;
    }

    private void ReloadFlightData(object sender, ElapsedEventArgs e)
    {
        foreach (Airport airport in airports)
        {
            InitFromFlifo("4", "12", "A", airport);
            airport.ArrivalLastUpdate = DateTime.Now;

            InitFromFlifo("4", "12", "D", airport);
            airport.DepartureLastUpdate = DateTime.Now;

            dao.ClearOldStuff();
        }

        OnServerFlightsUpdates?.Invoke();
    }

    private void InitFromFlifo(string pastWindow, string futurWindow, string direction, Airport airport)
    {
        string url = String.Format(InitTemplate, airport.FlifoAptCode, direction);
        logger.Trace($"Init From FLIFO  {url}");

        if (Test)
        {
            string json;
            if (direction == "D")
            {
                json = File.ReadAllText(TestDepartures);
            }
            else
            {
                json = File.ReadAllText(TestArrivals);
            }

            ProcessResponse(json, direction, airport);
        }
        else
        {
            RestResponse resp = GetRestURI(url, airport.FlifoKey).Result;
            if (resp.StatusCode == HttpStatusCode.OK)
            {
                ProcessResponse(resp.Content, direction, airport);
            }
            else
            {
                logger.Error(resp.StatusCode);
            }
        }
    }

    private void CheckUpdateFromFLIFO(object sender, ElapsedEventArgs e)
    {
        foreach (Airport airport in airports)
        {
            arrLogger.Info("Checking for Arrival Updates");
            airport.ArrivalLastUpdate = DateTime.Now;
            List<Flight> updatedArrivalflights = UpdateFromFLIFO("A", airport);
            if (updatedArrivalflights != null) OnServerFlightsUpdates?.Invoke();

            depLogger.Info("Checking for Departure Updates");
            airport.DepartureLastUpdate = DateTime.Now;
            List<Flight> updatedDepartureflights = UpdateFromFLIFO("D", airport);

            //Send Changes to all the clients
            if (updatedDepartureflights != null) OnServerFlightsUpdates?.Invoke();

            if (updatedDepartureflights == null && updatedArrivalflights == null) OnServerNoFlightsUpdates?.Invoke();
        }
    }

    private List<Flight> UpdateFromFLIFO(string direction, Airport airport)
    {
        List<Flight> updatedflights = new List<Flight>();
        string to = DateTime.Now.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ");
        string from = airport.ArrivalLastUpdate.AddSeconds(-10).ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ");
        if (direction == "D")
        {
            from = airport.DepartureLastUpdate.AddSeconds(-10).ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ");
        }

        string url = String.Format(UpdateTemplateURL, airport.FlifoAptCode, direction, from, to);

        if (Test)
        {
            string json;
            if (direction == "D")
            {
                json = File.ReadAllText(TestDeparturesUpdates);
            }
            else
            {
                json = File.ReadAllText(TestArrivalsUpdates);
            }
            updatedflights = ProcessResponse(json, direction, airport);
        }
        else
        {
            RestResponse resp = GetRestURI(url, airport.FlifoKey).Result;
            if (resp.StatusCode == HttpStatusCode.OK)
            {
                updatedflights = ProcessResponse(resp.Content, direction, airport);
            }
            else
            {
                logger.Error(resp.StatusCode);
            }
        }
        return updatedflights;
    }

    private List<Flight> ProcessResponse(string response, string direction, Airport airport)
    {
        JsonReader reader = new JsonTextReader(new StringReader(response));
        reader.DateParseHandling = DateParseHandling.None;
        JObject o = JObject.Load(reader);

        var flightRecords = o["flightRecords"];

        if (flightRecords == null)
        {
            logger.Warn("No Updates");
            return null;
        }
        List<Flight> updatedFlights = new List<Flight>();
        foreach (var flight in flightRecords)
        {
            try
            {
                JToken flightToken = (JObject)flight;

                Flight flt = new Flight(flightToken, airport.FlifoAptCode, fieldMappings);
                updatedFlights.Add(flt);

                if (flt.IsArrival)
                {
                    arrLogger.Info($"Updating Arrival {flt.Info}");
                }
                else
                {
                    depLogger.Info($"Updating Departure  {flt.Info}");
                }

                SendToRepo(flt);
            }
            catch (Exception ex)
            {
                logger.Error($"Error processing flight. {ex.Message} \n{flight.ToString()}");
            }
        }
        Console.WriteLine("Flights Created");
        return updatedFlights;
    }

    private void SendToRepo(Flight flt)
    {
        try
        {
            dao.SaveRecord(flt);
        }
        catch (Exception ex)
        {
            logger.Error(ex);
        }
    }

    public async Task<RestResponse> GetRestURI(string uri, string token)
    {
        RestResponse response = new RestResponse() { StatusCode = HttpStatusCode.NoContent };

        try
        {
            HttpClient _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("X-apiKey", token);

            using (var result = await _httpClient.GetAsync(uri))
            {
                response.StatusCode = result.StatusCode;
                response.Content = result.Content.ReadAsStringAsync().Result;

                return response;
            }
        }
        catch (Exception ex)
        {
            logger.Error(ex.Message);
            logger.Error(uri);
            return response;
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.Info(
 $"Queued Hosted Service is running.{Environment.NewLine}" +
 $"{Environment.NewLine}Tap W to add a work item to the " +
 $"background queue.{Environment.NewLine}");

        await BackgroundProcessing(stoppingToken);
    }

    private async Task BackgroundProcessing(CancellationToken stoppingToken)
    {
        XmlDocument doc = new XmlDocument();
        doc.Load("widget.config.xml");

        UpdateInterval = Int32.Parse(doc.SelectSingleNode(".//UpdateInterval").InnerText);
        LoadInterval = Int32.Parse(doc.SelectSingleNode(".//LoadInterval").InnerText);
        InitTemplate = doc.SelectSingleNode(".//InitURL").InnerText;
        UpdateTemplateURL = doc.SelectSingleNode(".//UpdateURL").InnerText;

        try
        {
            Test = bool.Parse(doc.SelectSingleNode(".//Test").InnerText);
            TestArrivals = doc.SelectSingleNode(".//TestArrivals").InnerText;
            TestDepartures = doc.SelectSingleNode(".//TestDepartures").InnerText;
            TestArrivalsUpdates = doc.SelectSingleNode(".//TestArrivalsUpdates").InnerText;
            TestDeparturesUpdates = doc.SelectSingleNode(".//TestDeparturesUpdates").InnerText;
        }
        catch (Exception)
        {
            Test = false;
        }
        try
        {
            EnableUpdates = bool.Parse(doc.SelectSingleNode(".//EnableUpdates").InnerText);
        }
        catch (Exception)
        {
            EnableUpdates = true;
        }

        foreach (XmlNode node in doc.SelectNodes(".//airport"))
        {
            airports.Add(new Airport()
            {
                FlifoAptCode = node.Attributes["FlifoAptCode"]?.Value,
                FlifoKey = node.Attributes["FlifoKey"]?.Value,
            });
        }

        foreach (XmlNode node in doc.SelectNodes(".//propertyMapping"))
        {
            fieldMappings.Add(new FieldMapping(node));
        }

        //One time initial load of the data 5 seconds after startup
        System.Timers.Timer initTimer = new System.Timers.Timer
        {
            Interval = 5 * 1000,
            AutoReset = false,
            Enabled = true
        };
        initTimer.Elapsed += ReloadFlightData;

        // One Clean out the old data 60 seconds after startup

        // No longer required because of ephemeral flight cache

        //System.Timers.Timer initCleanTimer = new System.Timers.Timer
        //{
        //    Interval = 60 * 1000,
        //    AutoReset = false,
        //    Enabled = true
        //};
        //initCleanTimer.Elapsed += CleanOldFlightData;

        // Regular clean up of the old data once every 12 hours

        // No longer required because the cache is reloaded every hour

        //System.Timers.Timer cleanTimer = new System.Timers.Timer
        //{
        //    Interval = 60 * 1000 * 720,
        //    AutoReset = true,
        //    Enabled = true
        //};
        //cleanTimer.Elapsed += CleanOldFlightData;

        if (EnableUpdates)
        {
            // Regular checks for the updated changes
            updateTimer = new System.Timers.Timer
            {
                Interval = UpdateInterval * 1000,
                AutoReset = true,
                Enabled = true
            };
            updateTimer.Elapsed += CheckUpdateFromFLIFO;
        }

        // Regular complete reload of the cache every 60 minutes
        reloadTimer = new System.Timers.Timer
        {
            Interval = LoadInterval * 60 * 1000,
            AutoReset = true,
            Enabled = true
        };
        reloadTimer.Elapsed += ReloadFlightData;
    }

    private void CleanOldFlightData(object? sender, ElapsedEventArgs e)
    {
        dao.ClearOldStuff();
    }
}