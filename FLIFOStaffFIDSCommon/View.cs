namespace FLIFOStaffFIDSCommon;

public class View
{
    public string? Name { get; set; }
    public string? Identifier { get; set; }
    public string? Type { get; set; }
    public List<string> Fields { get; set; } = new List<string>();
    public bool Enabled { get; set; }
}