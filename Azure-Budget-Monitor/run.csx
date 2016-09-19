#load "PayloadDtos.csx"
#load "methods.csx"

#r "System.Security"

using System;
using System.Diagnostics;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;

using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Newtonsoft.Json;
using System.Security.Authentication;
using System.Threading;

public static async Task Run(TimerInfo myTimer, TraceWriter log, CancellationToken cancellationToken)
{
    // Get OAuth2 token to call Azure API's
    var authResult = await GetToken();
    if (string.IsNullOrEmpty(authResult.AccessToken))
        throw new AuthenticationException("Client not authenticated");

    // Select the date we're interested in. This is usually the date your meter starts
    var today = DateTime.Today;
    var month = new DateTime(today.Year, today.Month - 1, 1);
    var startDate = new DateTimeOffset(month);
    var endDate = new DateTime(startDate.Year, startDate.Month + 1, 1);

    // Get how much we already spent on Azure services
    var usage = await GetUsageForSubscription(authResult.AccessToken, startDate, endDate, cancellationToken);
    // Get rates for Azure service
    var rates = await GetRatesForSubscription(authResult.AccessToken, cancellationToken);

    // Calculate whether we're over budget
    if (IsStartingToGetCostly(usage, rates))
    {
        log.Info($"Shutdown");
        // Shut non-essential VM's Down
        await StopVms(cancellationToken);
        
        // Send us a slack message
        await Notify("We just exceeded our qouta. Managers about to get angry. Shutting down some none essential VM's", cancellationToken);
    }
    else 
    {
        log.Info($"Wow, this is costing us close to nothing!");
    }
    
    // Check if we're runnig Low on Credit
    if (AreWeRunningLowOnCredit(usage, rates))
    {
        log.Info($"Low on Credit");
        // Send us a slack message
        await Notify("Hey we're running low on credit", cancellationToken);
    }
    else
    {
        log.Info($"Plenty of Credit");
    }
}

