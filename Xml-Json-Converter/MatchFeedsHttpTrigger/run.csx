using System.Net;

public static HttpResponseMessage Run(HttpRequestMessage req, TraceWriter log, out string outputQueueMsg)
{
    // Get Feed
    var MatchFeedXml = Task.Run(() => req.Content.ReadAsStringAsync()).GetAwaiter().GetResult();
    
    // Insert xml message into service bus
    outputQueueMsg = MatchFeedXml;
    return req.CreateResponse(HttpStatusCode.OK);
}