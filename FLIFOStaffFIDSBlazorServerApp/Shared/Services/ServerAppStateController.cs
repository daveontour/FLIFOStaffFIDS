using FLIFOStaffFIDSCommon;
using System.Xml;

namespace FLIFOStaffFIDSBlazorServerApp.Shared.Services;

public class ServerAppStateController : IClientStateController
{
    private DataAccessService dao;

    public ServerAppStateController(DataAccessService dao)
    {
        FlightStatusService.OnServerFlightsUpdates += ServerFlightsUpdated;
        this.dao = dao;
        Rules = GetRules();
    }

    private List<RolloffRule> Rules = new List<RolloffRule>();

    private void ServerFlightsUpdated()
    {
        LastUpdated = DateTime.Now;
        NotifyUpdateFlights();
    }

    private DateTime _lastUpdate;
    public DateTime LastUpdated { get => _lastUpdate; set => _lastUpdate = value; }
    public string SelectedTerminal { get; set; } = "all";
    public bool ApplyRollOffRules { get; set; } = true;
    public int PagerLength { get; set; } = 1;

    public event Action? OnFlightsUpdated;

    public event Action? OnTerminalUpdated;

    public event Action? OnRulesUpdated;

    public event Action? OnPageChange;

    public event Action? OnPagerLengthChange;

    public Task<List<Flight>> GetAllFlights(bool applyRolloff = true, string terminal = "all")
    {
        if (SelectedTerminal == "all")
        {
            return Task.FromResult(ApplyRules(dao.GetFlights().OrderBy(f => f.STODateTime).ToList()));
        }
        else
        {
            return Task.FromResult(ApplyRules(dao.GetFlights().Where<Flight>(f => f.Terminal == SelectedTerminal).OrderBy(f => f.STODateTime).ToList()));
        }
    }

    public Task<List<Flight>> GetArrFlights(bool applyRolloff = true, string terminal = "all")
    {
        if (SelectedTerminal == "all")
        {
            return Task.FromResult(ApplyRules(dao.GetFlights().OrderBy(f => f.STODateTime).ToList()));
        }
        else
        {
            return Task.FromResult(ApplyRules(dao.GetFlights().Where<Flight>(f => f.Terminal == SelectedTerminal && f.IsArrival).OrderBy(f => f.STODateTime).ToList()));
        }
    }

    public Task<List<Flight>> GetDepFlights(bool applyRolloff = true, string terminal = "all")
    {
        if (SelectedTerminal == "all")
        {
            return Task.FromResult(ApplyRules(dao.GetFlights().OrderBy(f => f.STODateTime).ToList()));
        }
        else
        {
            return Task.FromResult(ApplyRules(dao.GetFlights().Where<Flight>(f => f.Terminal == SelectedTerminal && !f.IsArrival).OrderBy(f => f.STODateTime).ToList()));
        }
    }

    public Task<MetaData> GetMetaData()
    {
        XmlDocument doc = new XmlDocument();
        doc.Load("widget.config.xml");
        MetaData meta = new MetaData();

        XmlNode node = doc.SelectSingleNode(".//metadata");

        meta.AirportName = node?.Attributes?["airportName"]?.Value;

        return Task.FromResult(meta);
    }

    public Task<List<Terminal>> GetTerminals()
    {
        XmlDocument doc = new XmlDocument();
        doc.Load("widget.config.xml");
        List<Terminal> terminals = new();
        terminals.Add(new Terminal()
        {
            Name = "All Terminals",
            Identifier = "all"
        });
        foreach (XmlNode node in doc.SelectNodes(".//terminal"))
        {
            terminals.Add(new Terminal()
            {
                Name = node.Attributes?["name"]?.Value,
                Identifier = node.Attributes?["identifier"]?.Value
            }
            );
        }
        return Task.FromResult(terminals);
    }

    public Task<List<View>> GetViews()
    {
        XmlDocument doc = new XmlDocument();
        doc.Load("widget.config.xml");
        List<View> views = new();
        foreach (XmlNode node in doc.SelectNodes(".//view"))
        {
            View v = new View()
            {
                Name = node.Attributes?["name"]?.Value,
                Identifier = node.Attributes?["identifier"]?.Value,
                Type = node.Attributes?["type"]?.Value,
                Enabled = bool.Parse(node.Attributes?["enabled"]?.Value)
            };
            try
            {
                foreach (XmlNode n in node.SelectNodes("./field"))
                {
                    v.Fields.Add(n.InnerText);
                }
            }
            catch (Exception) { }

            views.Add(v);
        }
        return Task.FromResult(views);
    }

    public List<RolloffRule> GetRules()
    {
        XmlDocument doc = new XmlDocument();
        doc.Load("widget.config.xml");
        List<RolloffRule> rules = new List<RolloffRule>();
        foreach (XmlNode node in doc.SelectNodes(".//rolloffrule"))
        {
            rules.Add(new RolloffRule()
            {
                Status = node.Attributes["status"].Value,
                RolloffTime = int.Parse(node.Attributes["rollofftime"].Value),
                Kind = node.Attributes["kind"].Value,
                Reference = node.Attributes["reference"].Value,
            }
            ); ;
        }

        return rules;
    }

    public void NotifyUpdateFlights()
    {
        OnFlightsUpdated?.Invoke();
    }

    public void NotifyUpdateRules()
    {
        OnRulesUpdated?.Invoke();
    }

    public void NotifyUpdateTerminal()
    {
        OnTerminalUpdated?.Invoke();
    }

    public void NotifyPageChange()
    {
        OnPageChange?.Invoke();
    }

    public void NotifyPagerLengthChange()
    {
        OnPagerLengthChange?.Invoke();
    }

    public bool StateControllerReady()
    {
        return true;
    }

    public List<Flight> ApplyRules(List<Flight> fls)
    {
        if (!ApplyRollOffRules)
        {
            return fls;
        }

        if (Rules == null || Rules.Count() == 0)
        {
            return fls;
        }

        List<Flight> flsRtn = new();
        foreach (var rule in Rules)
        {
            flsRtn = fls.Where<Flight>(e => !rule.RollOff(e)).ToList<Flight>();
        }

        return flsRtn;
    }
}