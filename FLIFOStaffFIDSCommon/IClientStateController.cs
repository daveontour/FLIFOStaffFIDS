namespace FLIFOStaffFIDSCommon;

public interface IClientStateController
{
    public event Action OnFlightsUpdated;

    public event Action OnTerminalUpdated;

    public event Action OnRulesUpdated;

    public DateTime LastUpdated { get; set; }
    public string SelectedTerminal { get; set; }
    public bool ApplyRollOffRules { get; set; }

    public bool StateControllerReady();

    public Task<List<View>> GetViews();

    public Task<List<Terminal>> GetTerminals();

    public Task<MetaData> GetMetaData();

    public List<RolloffRule> GetRules();

    public Task<List<Flight>> GetAllFlights(bool applyRolloff = true);

    public Task<List<Flight>> GetArrFlights(bool applyRolloff = true);

    public Task<List<Flight>> GetDepFlights(bool applyRolloff = true);

    public void NotifyUpdateTerminal();

    public void NotifyUpdateFlights();

    public void NotifyUpdateRules();
}