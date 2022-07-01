namespace FLIFOStaffFIDSCommon;

public interface IClientStateController
{
    public event Action OnFlightsUpdated;

    public event Action OnTerminalUpdated;

    public event Action OnRulesUpdated;

    public event Action OnPageChange;

    public event Action OnPagerLengthChange;

    public DateTime LastUpdated { get; set; }
    public string SelectedTerminal { get; set; }
    public bool ApplyRollOffRules { get; set; }
    public int PagerLength { get; set; }

    public bool StateControllerReady();

    public Task<List<View>> GetViews();

    public Task<List<Terminal>> GetTerminals();

    public Task<MetaData> GetMetaData();

    public List<RolloffRule> GetRules();

    public Task<List<Flight>> GetAllFlights(bool applyRolloff = true, string terminal = "all");

    public Task<List<Flight>> GetArrFlights(bool applyRolloff = true, string terminal = "all");

    public Task<List<Flight>> GetDepFlights(bool applyRolloff = true, string terminal = "all");

    public void NotifyUpdateTerminal();

    public void NotifyUpdateFlights();

    public void NotifyUpdateRules();

    public void NotifyPageChange();

    public void NotifyPagerLengthChange();
}