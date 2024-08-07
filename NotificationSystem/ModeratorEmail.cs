using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos.Table;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using NotificationSystem.Models;
using NotificationSystem.Utilities;

namespace NotificationSystem
{
    [StorageAccount("OrcaNotificationStorageSetting")]
    public static class ModeratorEmail
    {
        [FunctionName("SubscribeToModeratorEmail")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", "delete")] HttpRequest req,
            [Table("EmailList")] CloudTable cloudTable,
            ILogger log)
        {
            return await EmailHelpers.UpdateEmailList<ModeratorEmailEntity>(req, cloudTable, log);
        }

        [FunctionName("ListModeratorEmails")]
        public static IEnumerable<string> List(
            [HttpTrigger(AuthorizationLevel.Function, "get")] HttpRequest req,
            [Table("EmailList")] CloudTable cloudTable,
            ILogger log)
        {
            return EmailHelpers.GetEmailEntities(cloudTable, "Moderator").Select(e => e.Email);
        }
    }
}
