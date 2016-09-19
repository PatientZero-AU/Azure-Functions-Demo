using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using Xero.Api.Example.Applications.Private;
using Xero.Api.Infrastructure.OAuth;
using Xero.Api.Payroll.Australia.Model;
using Xero.Api.Payroll.Australia.Model.Types;
using Xero.Api.Payroll.Common.Model.Status;
using Xero.Api.Serialization;

using XeroCoreApi = Xero.Api.Core.XeroCoreApi;
using AustralianPayroll = Xero.Api.Payroll.AustralianPayroll;

namespace PzXero.Core
{
    public class PatientXero
    {
        private const string XeroEndpoint = "https://api.xero.com/api.xro/2.0/";
        private const string Password = "[Your_Password]";
        private const string Key = "[Your_Key]";
        private const string Secret = "[Your_Secret]";
        
        public static XeroCoreApi CreateCoreClient(X509Certificate2 certificate)
        {
            return new XeroCoreApi(XeroEndpoint, new PrivateAuthenticator(certificate),
                new Consumer(Key, Secret), null, new DefaultMapper(), new DefaultMapper());
        }

        public static PayRun GetPayRunToProcess(AustralianPayroll payrollClient)
        {
            return GetPayRuns(payrollClient).First(p => p.PayRunStatus == PayRunStatus.Draft);
        }

        public static X509Certificate2 GetCertificate()
        {
            // In this case we use a certificate baked into the dll. You might want to consider using 
            // a safer authentication mechanism, like an OAuth access token.
            // Please note that I have not added the certificate in this sample
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = assembly.GetName().Name + "." + "Certs.public_privatekey.pfx";

            var stream = assembly.GetManifestResourceStream(resourceName);

            if (stream == null)
                throw new ArgumentException("Stream is null");

            byte[] bytes;
            using (var streamReader = new MemoryStream())
            {
                stream.CopyTo(streamReader);
                bytes = streamReader.ToArray();
            }

            var filename = "pz_cert" + Guid.NewGuid();
            var file = Path.Combine(Path.GetTempPath(), filename);

            File.WriteAllBytes(file, bytes);
            var cert = new X509Certificate2(file, Password, X509KeyStorageFlags.MachineKeySet);

            return cert;
        }

        public static AustralianPayroll CreatePayrollClient(X509Certificate2 certificate)
        {
            var payrollClient = new AustralianPayroll(XeroEndpoint, new PrivateAuthenticator(certificate),
                new Consumer(Key, Secret), null, new DefaultMapper(), new DefaultMapper());

            return payrollClient;
        }

        public static PayrollCalendar GetPayrollCalendar(AustralianPayroll payrollClient)
        {
            return payrollClient.PayrollCalendars.Find().Single(c => c.CalendarType == CalendarType.Fortnightly);
        }

        public static IEnumerable<PayRun> GetPayRuns(AustralianPayroll payrollClient)
        {
            return payrollClient.PayRuns.Find();
        }

        public static bool AreThereAnyPayrunsToProcess(AustralianPayroll payrollClient)
        {
            return GetPayRuns(payrollClient).Any(i => i.PayRunStatus == PayRunStatus.Draft);
        }

        public static void CreateNewPayrun(AustralianPayroll payrollClient)
        {
            // Create a payrun using a calendar, in our case a fortnight calendar
            var calendar = GetPayrollCalendar(payrollClient);
            var newPayrun = new PayRun
            {
                PayrollCalendarId = calendar.Id
            };
            payrollClient.PayRuns.Create(newPayrun);
        }

        public static bool DoWeHaveEnoughMoney(XeroCoreApi coreClient, PayRun payRun)
        {
            // The only way to get the balance in Xero is from the reports Api
            var report = coreClient.Reports.BalanceSheet(DateTime.Today);
            var balanceStr = report.Rows[2].Rows[0].Cells[1].Value;
            var balance = decimal.Parse(balanceStr);
            return Math.Abs(balance) >= payRun.Wages;
        }

        public static void Pay(AustralianPayroll payrollClient, PayRun payrun)
        {
            // Complete Payrun
            payrun.PayRunStatus = PayRunStatus.Posted;
            payrollClient.PayRuns.Update(payrun);
        }
    }
}
