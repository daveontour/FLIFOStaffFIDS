using System.Net;

namespace FLIFOStaffFIDSCommon;

public class RestResponse
{
    public HttpStatusCode StatusCode { get; set; }
    public string? Content { get; set; }
}