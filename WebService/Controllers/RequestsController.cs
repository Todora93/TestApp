namespace WebService.Controllers
{
    using System;
    using System.Collections.Generic;
    using System.Fabric;
    using System.Fabric.Query;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.ServiceFabric.Services.Client;
    using Microsoft.ServiceFabric.Services.Remoting.Client;
    using Newtonsoft.Json;
    using MyActorService.Interfaces;

    [Route("api/[controller]")]
    public class RequestsController : Controller
    {
        private readonly StatelessServiceContext serviceContext;
        private readonly FabricClient fabricClient;
        private readonly ConfigSettings configSettings;

        public RequestsController(StatelessServiceContext serviceContext, FabricClient fabricClient, ConfigSettings configSettings)
        {
            this.serviceContext = serviceContext;
            this.fabricClient = fabricClient;
            this.configSettings = configSettings;
        }

        //[HttpPut]
        //public async Task<IActionResult> Put([FromBody] object request)
        //{
        //    try
        //    {
        //        string serviceUri = $"{this.serviceContext.CodePackageActivationContext.ApplicationName}/{this.configSettings.RequestsServiceName}";

        //        ServicePartitionList partitions = await this.fabricClient.QueryManager.GetPartitionListAsync(new Uri(serviceUri));

        //        if(partitions == null || partitions.Count == 0)
        //        {
        //            return new ContentResult { StatusCode = 404, Content = "There's no available replica. Please check service status." };
        //        }

        //        ServicePartitionKey key = new ServicePartitionKey(((Int64RangePartitionInformation)(partitions[0].PartitionInformation)).LowKey);

        //        IRequestManager service = ServiceProxy.Create<IRequestManager>(new Uri(serviceUri), key);

        //        var user = JsonConvert.DeserializeObject<UserRequest>(request.ToString());

        //        await service.AddRequestAsync(user);

        //        ServiceEventSource.Current.Message($"Added new user request: {user.ToString()}!");

        //        return this.Ok();
        //    }
        //    catch (FabricNotPrimaryException)
        //    {
        //        return new ContentResult { StatusCode = 410, Content = "The primary replica has moved. Please re-resolve the service." };
        //    }
        //    catch (/*FabricException*/ Exception e)
        //    {
        //        return new ContentResult { StatusCode = 503, Content = $"The service was unable to process the request. Please try again. Exception: {e}" };
        //    }
        //}

        [HttpPut]
        public async Task<IActionResult> Put([FromBody] object request)
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

                var user = JsonConvert.DeserializeObject<UserRequest>(request.ToString());

                var c = Int32.Parse(user.UserName);

                //await service.AddRequestAsync(user);

                await service.StartGeneratingRequests(c);

                ServiceEventSource.Current.Message($"Generating requests: {c}!");

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

        //// GET: api/values
        //[HttpGet]
        //public async Task<IActionResult> Get()
        //{
        //    try
        //    {
        //        string serviceUri = $"{this.serviceContext.CodePackageActivationContext.ApplicationName}/{this.configSettings.RequestsServiceName}";

        //        ServicePartitionList partitions = await this.fabricClient.QueryManager.GetPartitionListAsync(new Uri(serviceUri));

        //        List<UserRequest> result = new List<UserRequest>();

        //        foreach (Partition partition in partitions)
        //        {
        //            ServicePartitionKey key = new ServicePartitionKey(((Int64RangePartitionInformation)partition.PartitionInformation).LowKey);
        //            IRequestManager service = ServiceProxy.Create<IRequestManager>(new Uri(serviceUri), key);

        //            var list = JsonConvert.DeserializeObject<List<UserRequest>>(await service.GetAllRequests());
        //            if (list != null && list.Any())
        //            {
        //                result.AddRange(list);
        //            }
        //        }

        //        var json = this.Json(result);

        //        ServiceEventSource.Current.Message($"Get all user requests: {json}!");

        //        return json;
        //    }
        //    catch (FabricNotPrimaryException)
        //    {
        //        return new ContentResult { StatusCode = 410, Content = "The primary replica has moved. Please re-resolve the service." };
        //    }
        //    catch (/*FabricException*/ Exception e)
        //    {
        //        return new ContentResult { StatusCode = 503, Content = $"The service was unable to process the request. Please try again. Exception: {e}" };
        //    }
        //}

        //// DELETE: api/values
        //[HttpDelete]
        //public async Task<IActionResult> Delete()
        //{
        //    try
        //    {
        //        string serviceUri = $"{this.serviceContext.CodePackageActivationContext.ApplicationName}/{this.configSettings.RequestsServiceName}";

        //        ServicePartitionList partitions = await this.fabricClient.QueryManager.GetPartitionListAsync(new Uri(serviceUri));

        //        List<UserRequest> result = new List<UserRequest>();

        //        foreach (Partition partition in partitions)
        //        {
        //            ServicePartitionKey key = new ServicePartitionKey(((Int64RangePartitionInformation)partition.PartitionInformation).LowKey);
        //            IRequestManager service = ServiceProxy.Create<IRequestManager>(new Uri(serviceUri), key);

        //            //await service.DeleteAllRequests();

        //            await service.DeleteAll();
        //        }

        //        ServiceEventSource.Current.Message($"Deleted all user requests!");

        //        return this.Ok();
        //    }
        //    catch (FabricNotPrimaryException)
        //    {
        //        return new ContentResult { StatusCode = 410, Content = "The primary replica has moved. Please re-resolve the service." };
        //    }
        //    catch (/*FabricException*/ Exception e)
        //    {
        //        return new ContentResult { StatusCode = 503, Content = $"The service was unable to process the request. Please try again. Exception: {e}" };
        //    }
        //}
    }
}