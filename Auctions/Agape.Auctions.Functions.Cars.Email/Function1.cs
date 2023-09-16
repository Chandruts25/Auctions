using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Mail;
using Microsoft.Azure.Documents;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using System.IO;
using System.Text;

namespace Agape.Auctions.Functions.Cars.Email
{
    public static class Function1
    {
        [FunctionName("Function1")]
        public static void Run([CosmosDBTrigger(databaseName: "Tasks", collectionName: "Car", ConnectionStringSetting = "CosmosDbConnectionstring", LeaseCollectionName = "leases")] IReadOnlyList<Document> input, ILogger log)
        {
            if (input != null && input.Count > 0)
            {
                log.LogInformation("Documents modified " + input.Count);
                log.LogInformation("First document Id " + input[0].Id);

                SendTestEmail(input[0].Id);
            }
        }

        public static void SendTestEmail(string documentId)
        {

            MailMessage mail = new MailMessage();
            mail.From = new MailAddress("krish.trenzlife@gmail.com");
            mail.Sender = new MailAddress("krishtna.ar@gmail.com");
            mail.To.Add("krish@agapeworks.org");
            mail.IsBodyHtml = true;
            mail.Subject = "New Car added " + documentId;
            mail.Body = "New Car added . Car ID - " + documentId + "  Thanks, ShopCarHere Team";

            SmtpClient smtp = new SmtpClient("smtp.gmail.com", 587);
            smtp.UseDefaultCredentials = false;

            smtp.Credentials = new System.Net.NetworkCredential("krishtna.ar@gmail.com", "rocky2019");
            smtp.DeliveryMethod = SmtpDeliveryMethod.Network;
            smtp.EnableSsl = true;

            smtp.Timeout = 30000;
            try
            {
                smtp.Send(mail);
            }
            catch (SmtpException e)
            {
                string dd = e.Message;
            }
        }
    }
}
