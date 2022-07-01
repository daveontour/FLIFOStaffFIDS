using Newtonsoft.Json.Linq;
using System.Reflection;
using System.Text;

namespace FLIFOStaffFIDSCommon;

public class Flight
{
    public List<FieldMapping> FieldMappings { get; private set; }

    public bool IsArrival;
    public string HomeAirport { get; private set; }
    public string Key { get; set; }
    public string Info { get; set; }
    public string STODateTimeStr { get; set; }
    public DateTime STODateTime { get => DateTime.Parse(STODateTimeStr); }

    public string SchedDate
    {
        get => STODateTime.ToString("MMM dd");
    }

    public string SchedTimeShort
    {
        get => STODateTime.ToString("HH:mm");
    }

    public string Terminal
    {
        get
        {
            if (Kind == "Arrival")
            {
                return ArrTerminal;
            }
            else
            {
                return DepTerminal;
            }
        }
    }

    public string Status
    {
        get
        {
            if (Kind == "Arrival")
            {
                return ArrStatus;
            }
            else
            {
                return DepStatus;
            }
        }
    }

    public string Route
    {
        get
        {
            if (Kind == "Arrival")
            {
                return DepRoute;
            }
            else
            {
                return ArrRoute;
            }
        }
    }

    public string RouteCity
    {
        get
        {
            if (Kind == "Arrival")
            {
                return DepRouteCity;
            }
            else
            {
                return ArrRouteCity;
            }
        }
    }

    public string CheckIn
    {
        get
        {
            if (Kind == "Arrival")
            {
                return "-";
            }
            else
            {
                return DepCheckIn;
            }
        }
    }

    public string Carousel
    {
        get
        {
            if (Kind == "Arrival")
            {
                return ArrCarousel;
            }
            else
            {
                return "-";
            }
        }
    }

    public string Gate
    {
        get
        {
            if (Kind == "Arrival")
            {
                return ArrGate;
            }
            else
            {
                return DepGate;
            }
        }
    }

    public string ETA
    {
        get
        {
            if (Kind == "Arrival")
            {
                return ArrETAShort;
            }
            else
            {
                return "-";
            }
        }
    }

    public string ATA
    {
        get
        {
            if (Kind == "Arrival")
            {
                return ArrATAShort;
            }
            else
            {
                return "-";
            }
        }
    }

    public string ETD
    {
        get
        {
            if (Kind == "Arrival")
            {
                return "-";
            }
            else
            {
                return DepETDShort;
            }
        }
    }

    public string ATD
    {
        get
        {
            if (Kind == "Arrival")
            {
                return "-";
            }
            else
            {
                return DepATDShort;
            }
        }
    }

    public string Kind { get; set; }
    public string CodeShares { get; set; }
    public string OpAirlineCode { get; set; }
    public string FlightNumber { get; set; }
    public string OpAirlineName { get; set; }
    public string AircraftRegistration { get; set; }
    public string AircraftTypeCode { get; set; }
    public string AircraftModel { get; set; }
    public string FltDuration { get; set; }
    public string ServiceType { get; set; }

    public string ArrRoute { get; set; }
    public string ArrRouteCity { get; set; }
    public string ALDT { get; set; }
    public string AIBT { get; set; }
    public string EIBT { get; set; }
    public string ArrGate { get; set; }
    public string ArrStatus { get; set; }
    public string ArrCarousel { get; set; }
    public string ArrTerminal { get; set; }
    public string ArrSTODateTime { get; set; }
    public string ArrETADateTime { get; set; }
    public string ArrATADateTime { get; set; }

    public string ArrETAShort
    {
        get
        {
            string d = GetProperty("ArrETADateTime");
            if (d != "-")
            {
                return DateTime.Parse(d).ToString("HH:mm");
            }
            else
            {
                return d;
            }
        }
    }

    public string ArrATAShort
    {
        get
        {
            string d = GetProperty("ArrATADateTime");
            if (d != "-")
            {
                return DateTime.Parse(d).ToString("HH:mm");
            }
            else
            {
                return d;
            }
        }
    }

    public string DepRoute { get; set; }
    public string DepRouteCity { get; set; }
    public string AOBT { get; set; }
    public string ATOT { get; set; }
    public string EOBT { get; set; }
    public string DepGate { get; set; }
    public string DepStatus { get; set; }
    public string DepCheckIn { get; set; }
    public string DepTerminal { get; set; }
    public string DepSTODateTime { get; set; }
    public string DepSTODate { get; set; }
    public string DepETDDateTime { get; set; }

    public string DepETDShort
    {
        get
        {
            string d = GetProperty("DepETDDateTime");
            if (d != "-")
            {
                return DateTime.Parse(d).ToString("HH:mm");
            }
            else
            {
                return d;
            }
        }
    }

    public string DepATDDateTime { get; set; }

    public string DepATDShort
    {
        get
        {
            string d = GetProperty("DepATDDateTime");
            if (d != "-")
            {
                return DateTime.Parse(d).ToString("HH:mm");
            }
            else
            {
                return d;
            }
        }
    }

    public string ExtField1 { get; set; }
    public string ExtField2 { get; set; }
    public string ExtField3 { get; set; }
    public string ExtField4 { get; set; }
    public string ExtField5 { get; set; }
    public string ExtField6 { get; set; }
    public string ExtField7 { get; set; }
    public string ExtField8 { get; set; }
    public string ExtField9 { get; set; }

    public string GetProperty(string name)
    {
        PropertyInfo propertyInfo = this.GetType().GetProperty(name);
        string val = (string)propertyInfo.GetValue(this);

        if (string.IsNullOrEmpty(val))
        {
            return "-";
        }
        else
        {
            return val;
        }
    }

    public Flight()
    { }

    public Flight(JToken flight, string apt, List<FieldMapping> fieldMappings)
    {
        this.HomeAirport = apt;
        PopulateFieldsDictionary(flight, fieldMappings, apt);
    }

    public void PopulateFieldsDictionary(JToken flightToken, List<FieldMapping> fieldMappings, string apt)
    {
        foreach (FieldMapping field in fieldMappings)
        {
            string? value;
            try
            {
                value = flightToken?.SelectToken(field.JPath)?.ToString();
            }
            catch (Exception)
            {
                value = null;
            }
            try
            {
                PropertyInfo propertyInfo = this.GetType().GetProperty(field.Property);
                propertyInfo.SetValue(this, value, null);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        SetCodeShares(flightToken);

        try
        {
            if (ArrRoute == apt)
            {
                IsArrival = true;
                STODateTimeStr = ArrSTODateTime;
                Kind = "Arrival";
            }
            else
            {
                IsArrival = false;
                STODateTimeStr = DepSTODateTime;
                Kind = "Departure";
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Problem determining IsArrival. {ex.Message}");
            IsArrival = false;
        }

        Key = $"{OpAirlineCode}{FlightNumber}{STODateTime.ToString()}{Kind}";
        Info = $"{OpAirlineCode} {FlightNumber} {Kind}";
    }

    private void SetCodeShares(JToken flightToken)
    {
        StringBuilder sb = new();
        JToken? cselement = flightToken.SelectToken(".flightIdentifier.marketingCarriers");
        if (cselement == null) return;
        IEnumerable<JToken> codes = cselement.SelectTokens("$..iataCode");
        foreach (JToken code in codes)
        {
            sb.Append(code.ToString());
            sb.Append(",");
        }
        string t = sb.ToString();
        if (t.Length > 0)
        {
            t = t.Remove(t.Length - 1, 1);
            CodeShares = t;
        }
    }
}