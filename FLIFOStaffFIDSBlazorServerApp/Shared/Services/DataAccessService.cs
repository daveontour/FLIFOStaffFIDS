using FLIFOStaffFIDSCommon;
using System.Collections.Concurrent;

namespace FLIFOStaffFIDSBlazorServerApp.Shared.Services;

public class DataAccessService
{
    private static ConcurrentDictionary<string, Flight> flightsCache = new ConcurrentDictionary<string, Flight>();

    public DataAccessService()
    {
    }

    public IEnumerable<Flight> GetFlights()
    {
        var flights = flightsCache.Values;
        return flights;
    }

    public ConcurrentDictionary<string, Flight> GetFlightsCache()
    {
        return flightsCache;
    }

    public IEnumerable<Flight>? GetFlights(DateTime from, DateTime to)
    {
        return flightsCache.Values.Where(f => f.STODateTime > from && f.STODateTime <= to);
    }

    public IEnumerable<Flight>? GetOpFlightsWindow()
    {
        DateTime from = DateTime.Now.AddDays(-1);
        DateTime to = DateTime.Now.AddDays(1);

        return GetFlights(from, to);
    }

    public void SaveRecord(Flight record)
    {
        flightsCache.AddOrUpdate(record.Key, record, (key, oldValue) => record);
    }

    public void DeleteRecord(Flight record)
    {
        try
        {
            flightsCache.TryRemove(record.Key, out record);
        }
        catch (Exception) { }
    }

    public void Clear()
    {
        flightsCache.Clear();
    }

    public void ClearOldStuff()
    {
        DateTime from = DateTime.Now.AddDays(-1);
        DateTime to = DateTime.Now.AddDays(1);
        foreach (Flight f in GetFlights())
        {
            if (f.STODateTime < from || f.STODateTime > to)
            {
                DeleteRecord(f);
            }
        }
    }
}