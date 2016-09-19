using Newtonsoft.Json;

public class UsageAggregate
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Type { get; set; }
    public Properties Properties { get; set; }
}

public class UsagePayload
{
    public List<UsageAggregate> Value { get; set; }
}

public class InfoFields
{
    public string MeteredRegion { get; set; }
    public string MeteredService { get; set; }
    public string Project { get; set; }
    public string MeteredServiceType { get; set; }
    public string ServiceInfo1 { get; set; }
}

public class Properties
{
    public string SubscriptionId { get; set; }
    public string UsageStartTime { get; set; }
    public string UsageEndTime { get; set; }
    public string MeterId { get; set; }
    public InfoFields InfoFields { get; set; }

    [JsonProperty("instanceData")]
    public string InstanceDataRaw { get; set; }
    public InstanceDataType InstanceData => JsonConvert.DeserializeObject<InstanceDataType>(InstanceDataRaw.Replace("\\\"", ""));

    public double Quantity { get; set; }
    public string Unit { get; set; }
    public string MeterName { get; set; }
    public string MeterCategory { get; set; }
    public string MeterSubCategory { get; set; }
    public string MeterRegion { get; set; }
}

public class InstanceDataType
{
    [JsonProperty("Microsoft.Resources")]
    public MicrosoftResourcesDataType MicrosoftResources { get; set; }
}

public class MicrosoftResourcesDataType
{
    public string ResourceUri { get; set; }

    public IDictionary<string, string> Tags { get; set; }

    public IDictionary<string, string> AdditionalInfo { get; set; }

    public string Location { get; set; }

    public string PartNumber { get; set; }

    public string OrderNumber { get; set; }
}

public class RateCardPayload
{
    public List<Offer> OfferTerms { get; set; }
    public List<Resource> Meters { get; set; }
    public string Currency { get; set; }
    public string Locale { get; set; }
    public string RatingDate { get; set; }
    public bool IsTaxIncluded { get; set; }
}

public class Offer
{
    public string Name { get; set; }
    public double Credit { get; set; }
    public IEnumerable<Guid> ExcludedMeterIds { get; set; }
    public DateTime EffectiveDate { get; set; }
}

public class Resource
{
    public string MeterId { get; set; }
    public string MeterName { get; set; }
    public string MeterCategory { get; set; }
    public string MeterSubCategory { get; set; }
    public string Unit { get; set; }
    public Dictionary<double, double> MeterRates { get; set; }
    public string EffectiveDate { get; set; }
    public List<string> MeterTags { get; set; }
    public string MeterRegion { get; set; }
    public double IncludedQuantity { get; set; }

}