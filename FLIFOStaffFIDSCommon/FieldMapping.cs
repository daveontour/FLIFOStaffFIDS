using Newtonsoft.Json.Linq;
using System.Globalization;
using System.Xml;

namespace FLIFOStaffFIDSCommon;

public class FieldMapping
{
    public string JPath { get; set; }
    public string Property { get; set; }
    public string InputFormat { get; set; }
    public string OutputFormat { get; set; }
    public bool IsDateTransform { get; set; } = false;
    public bool HasDataTransformer { get; set; } = false;
    public string DataTransformerClass { get; set; }

    private readonly string propertyTemplate = @"{0}";

    public FieldMapping(XmlNode node)
    {
        try
        {
            // The JSONPath to the element of interes
            JPath = node.Attributes["jpath"]?.Value;

            // The AMS External Name to map to
            Property = node.Attributes["property"]?.Value;

            // Input format if it is a Date
            InputFormat = node.Attributes["inputFormat"]?.Value;

            // Output Format if it is a Date
            OutputFormat = node.Attributes["outputFormat"]?.Value;

            //Transform the Date or not, if it is a Date
            bool trans;
            if (bool.TryParse(node.Attributes["transformDate"]?.Value, out trans))
            {
                IsDateTransform = trans;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
    }

    public Dictionary<string, string> SetFieldValue(JToken token, Dictionary<string, string> Fields)
    {
        if (Fields == null)
        {
            Console.WriteLine("Null Fields");
        }
        // Properties for custom fields take the form ".departure.customProperties.[?(@.key == 'ArrDelayCode')].value"
        try
        {
            if (JPath == null) return Fields;

            string input = token.SelectToken(JPath)?.ToString();

            if (input == null)
            {
                return Fields;
            }

            string value = input;
            if (IsDateTransform)
            {
                value = ConvertDateTime(value, InputFormat, OutputFormat);
            }

            if (!Fields.ContainsKey(Property))
            {
                Fields.Add(Property, value);
            }
            return Fields;
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.StackTrace);
            return Fields;
        }
    }

    public string ConvertDateTime(string inputDate, string inputFormat, string outputFormat)
    {
        try
        {
            if (inputDate.Length > 19)
            {
                inputDate = inputDate.Substring(0, 19);
            }
            DateTime d = DateTime.ParseExact(inputDate, inputFormat, CultureInfo.InvariantCulture);
            return d.ToString(outputFormat);
        }
        catch (Exception e)
        {
            Console.WriteLine($"Error with CnvertDate. {e.Message}  Input date:{inputDate}   Input Format:{inputFormat}  Output Format: {outputFormat}");
            return inputDate;
        }
    }
}