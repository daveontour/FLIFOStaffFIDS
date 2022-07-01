namespace FLIFOStaffFIDSCommon;

public class RolloffRule
{
    public int RolloffTime { get; set; }
    public string? Status { get; set; }
    public string? Kind { get; set; }
    public string? Reference { get; set; }

    public bool RollOff(Flight entry)
    {
        if (Kind.ToLower() != entry.Kind.ToLower()) { return false; }
        if (Status != entry.Status && Status != "*") { return false; }

        DateTime? refTime;

        if (Reference == "ATD")
        {
            try
            {
                refTime = DateTime.Parse(entry.DepATDDateTime);
                if (DateTime.Now > refTime.Value.AddMinutes(RolloffTime))
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch { return false; }
        }
        if (Reference == "ATA")
        {
            try
            {
                refTime = DateTime.Parse(entry.ArrATADateTime);
                if (DateTime.Now > refTime.Value.AddMinutes(RolloffTime))
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch { return false; }
        }
        if (Reference == "STO")
        {
            try
            {
                if (DateTime.Now > entry.STODateTime.AddMinutes(RolloffTime))
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch { return false; }
        }
        return false;
    }
}