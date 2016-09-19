using System;
using System.Threading.Tasks;
using System.Xml;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

public static void Run(string newQueueItem, out object newJsonFeed, TraceWriter log)
{
    var json = ConvertToJson(newQueueItem);

    // Insert json message into Document DB
    newJsonFeed = json;
}

private static JObject ConvertToJson(string xml){
    var doc = new XmlDocument();
    doc.LoadXml(xml);
    string json = JsonConvert.SerializeXmlNode(doc);
    return JObject.Parse(json);
}
