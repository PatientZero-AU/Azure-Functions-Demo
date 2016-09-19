#r "System.Configuration"
#r "System.Security"
#r "System.Web"

using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Newtonsoft.Json;
using System.Configuration;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Authentication;
using System.Threading;
using System.Web;

private const double CostThreshold = 1000.0;
private const double CreditThreshold = 10.0;

private static async Task<AuthenticationResult> GetToken()
{
    // Set below settings in the function settings section
    var aadInstance = ConfigurationManager.AppSettings["ida:AADInstance"];
    var tenant = ConfigurationManager.AppSettings["ida:Tenant"];
    var clientId = ConfigurationManager.AppSettings["ida:ClientId"];
    var appKey = ConfigurationManager.AppSettings["ida:AppKey"];
    var resourceId = ConfigurationManager.AppSettings["ida:ResourceID"];
    var authority = $"{aadInstance}/{tenant}/";

    var authContext = new AuthenticationContext(authority);
    var clientCredential = new ClientCredential(clientId, appKey);

    var result = await authContext.AcquireTokenAsync(resourceId, clientCredential);
    return result;
}

private static async Task<UsagePayload> GetUsageForSubscription(string accessToken, DateTimeOffset startDate, DateTimeOffset endDate, CancellationToken cancellationToken)
{
    var httpClient = new HttpClient();
    httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

    var startDateEncoded = HttpUtility.UrlEncode(startDate.ToString("u"));
    var endDateEncoded = HttpUtility.UrlEncode(endDate.ToString("u"));
    
    var usageApiUrl =
        $"{ConfigurationManager.AppSettings["ARMBillingServiceURL"]}/subscriptions/{ConfigurationManager.AppSettings["ida:SubscriptionID"]}/providers/Microsoft.Commerce/UsageAggregates?api-version=2015-06-01-preview&reportedstartTime={startDateEncoded}&reportedEndTime={endDateEncoded}";
    
    var response = await httpClient.GetAsync(usageApiUrl, cancellationToken);

    if (!response.IsSuccessStatusCode)
        throw new Exception($"Failed to retrieve usage list\nError:  {response.ReasonPhrase}\n");

    // Read the response and output it to the console.
    var usageResponse = await response.Content.ReadAsStringAsync();
    var usagePayload = JsonConvert.DeserializeObject<UsagePayload>(usageResponse);
    return usagePayload;
}

private static async Task<RateCardPayload> GetRatesForSubscription(string accessToken, CancellationToken cancellationToken)
{
    var httpClient = new HttpClient();
    httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

    var rateApiUrl =
        $"{ConfigurationManager.AppSettings["ARMBillingServiceURL"]}/subscriptions/{ConfigurationManager.AppSettings["ida:SubscriptionID"]}/providers/Microsoft.Commerce/RateCard?api-version=2015-06-01-preview&$filter=OfferDurableId eq \'{ConfigurationManager.AppSettings["OfferID"]}\' and Currency eq \'AUD\' and Locale eq \'en-AU\' and RegionInfo eq \'AU\'";

    var response = await httpClient.GetAsync(rateApiUrl, cancellationToken);

    if (!response.IsSuccessStatusCode)
        throw new Exception($"Failed to retrieve rate card list\nError:  {response.ReasonPhrase}\n");

    var rateResponse = await response.Content.ReadAsStringAsync();
    return JsonConvert.DeserializeObject<RateCardPayload>(rateResponse);
}

private static bool IsStartingToGetCostly(UsagePayload usage, RateCardPayload rates)
{
    var currentCost = WhatWeSpentSoFar(usage, rates);
    return currentCost > CostThreshold;
}

private static bool AreWeRunningLowOnCredit(UsagePayload usage, RateCardPayload rates)
{
    var currentCost = WhatWeSpentSoFar(usage, rates);
    var totalCredit = rates.OfferTerms.Sum(c => c.Credit);
    return totalCredit - currentCost < CreditThreshold;
}

private static double WhatWeSpentSoFar(UsagePayload usage, RateCardPayload rates)
{
    var meters = usage.Value.Select(m => m.Properties);

    var query = (from rate in rates.Meters
                 join meter in meters on rate.MeterId equals meter.MeterId
                 where !meter.MeterName.EndsWith("(in 10,000s)")
                 select
                     new
                     {
                         rate.MeterName,
                         Usage = meter.Quantity,
                         Rate = rate.MeterRates.Single(k => k.Key == 0).Value,
                         meter.Unit
                     }).ToArray();

    return query.Sum(q => q.Rate * q.Usage);
}

private static async Task Notify(string message, CancellationToken cancellationToken)
{
    // Send slack notification using Slack webhook
    var client = new HttpClient();
    var uri = new Uri($"{ConfigurationManager.AppSettings["SlackWebHook"]}");
    var content = new FormUrlEncodedContent(new[]
    {
        new KeyValuePair<string, string>("payload",
            $@"{{""text"": ""{message}""}}")
    });
    await client.PostAsync(uri, content, cancellationToken);
}

private static async Task StopVms(CancellationToken cancellationToken)
{
    // Using a Runbook web hook to stop VM
    var client = new HttpClient();
    var uri = new Uri($"{ConfigurationManager.AppSettings["StopVmWebHook"]}");
    await client.PostAsync(uri, null, cancellationToken);
}