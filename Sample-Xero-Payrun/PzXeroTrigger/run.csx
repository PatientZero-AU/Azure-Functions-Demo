#r "System.Reflection"
#r "System.Security"

using PzXero.Core;

using System.Net;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.Text;

using Xero.Api.Core;
using Xero.Api.Example.Applications.Private;
using Xero.Api.Infrastructure.OAuth;
using Xero.Api.Payroll.Common.Model.Status;
using Xero.Api.Serialization;

public static void Run(TimerInfo myTimer, TraceWriter log)
{
    // Get private X509 certificate 
    var cert = PatientXero.GetCertificate();

    // Create Xero client
    var payrollClient = PatientXero.CreatePayrollClient(cert);
    var coreClient = PatientXero.CreateCoreClient(cert);

    // Schedule a payrun if necessary
    if (!PatientXero.AreThereAnyPayrunsToProcess(payrollClient))
    {
        PatientXero.CreateNewPayrun(payrollClient);
    }

    var payrun = PatientXero.GetPayRunToProcess(payrollClient);

    // Check if we have enough funds
    if (PatientXero.DoWeHaveEnoughMoney(coreClient, payrun))
    {
        // Post Payrun
        PatientXero.Pay(payrollClient, payrun);
    }
    else
    {
        log.Info($"We don't have enough money - Do more Azure meetup talks!");
    }
}
