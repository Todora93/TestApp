namespace WebService.Controllers
{
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.ServiceFabric.Services.Client;
    using Microsoft.ServiceFabric.Services.Remoting.Client;
    using MyActorService.Interfaces;
    using System;
    using System.Fabric;
    using System.Fabric.Query;
    using System.Threading;
    using System.Threading.Tasks;

    [Route("api/[controller]")]
    public class MatchmakerController : Controller
    {
        private readonly StatelessServiceContext serviceContext;
        private readonly FabricClient fabricClient;
        private readonly ConfigSettings configSettings;

        public MatchmakerController(StatelessServiceContext serviceContext, FabricClient fabricClient, ConfigSettings configSettings)
        {
            this.serviceContext = serviceContext;
            this.fabricClient = fabricClient;
            this.configSettings = configSettings;
        }

        // GET: api/values
        [HttpGet]
        public async Task<IActionResult> Get()
        {
            try
            {
                string serviceUri = $"{this.serviceContext.CodePackageActivationContext.ApplicationName}/{this.configSettings.RequestsServiceName}";

                ServicePartitionList partitions = await this.fabricClient.QueryManager.GetPartitionListAsync(new Uri(serviceUri));

                if (partitions == null || partitions.Count == 0)
                {
                    return new ContentResult { StatusCode = 404, Content = "There's no available replica. Please check service status." };
                }

                ServicePartitionKey key = new ServicePartitionKey(((Int64RangePartitionInformation)(partitions[0].PartitionInformation)).LowKey);

                IRequestManager service = ServiceProxy.Create<IRequestManager>(new Uri(serviceUri), key);

                await service.Matchmake(new CancellationToken());

                ServiceEventSource.Current.Message($"Matchmaked performed!");

                return this.Ok();
            }
            catch (FabricNotPrimaryException)
            {
                return new ContentResult { StatusCode = 410, Content = "The primary replica has moved. Please re-resolve the service." };
            }
            catch (/*FabricException*/ Exception e)
            {
                return new ContentResult { StatusCode = 503, Content = $"The service was unable to process the request. Please try again. Exception: {e}" };
            }
        }
    }
}