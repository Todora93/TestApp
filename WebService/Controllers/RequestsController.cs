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

        [HttpPut]
        public async Task<IActionResult> Put([FromBody] object request)
        {
            try
            {
                string serviceUri = $"{this.serviceContext.CodePackageActivationContext.ApplicationName}/{this.configSettings.RequestsServiceName}";

                ServicePartitionList partitions = await this.fabricClient.QueryManager.GetPartitionListAsync(new Uri(serviceUri));

                if(partitions == null || partitions.Count == 0)
                {
                    return new ContentResult { StatusCode = 404, Content = "There's no available replica. Please check service status." };
                }

                ServicePartitionKey key = new ServicePartitionKey(((Int64RangePartitionInformation)(partitions[0].PartitionInformation)).LowKey);

                IRequestManager service = ServiceProxy.Create<IRequestManager>(new Uri(serviceUri), key);

                var userId = JsonConvert.DeserializeObject<UserRequest>(request.ToString());

                await service.AddRequestAsync(userId);

                ServiceEventSource.Current.Message($"Added new user request: {userId.ToString()}!");

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

        // GET: api/values
        [HttpGet]
        public async Task<IActionResult> Get()
        {
            try
            {
                string serviceUri = $"{this.serviceContext.CodePackageActivationContext.ApplicationName}/{this.configSettings.RequestsServiceName}";

                ServicePartitionList partitions = await this.fabricClient.QueryManager.GetPartitionListAsync(new Uri(serviceUri));

                List<UserRequest> result = new List<UserRequest>();

                foreach (Partition partition in partitions)
                {
                    ServicePartitionKey key = new ServicePartitionKey(((Int64RangePartitionInformation)partition.PartitionInformation).LowKey);
                    IRequestManager service = ServiceProxy.Create<IRequestManager>(new Uri(serviceUri), key);

                    var list = JsonConvert.DeserializeObject<List<UserRequest>>(await service.GetAllRequests());
                    if (list != null && list.Any())
                    {
                        result.AddRange(list);
                    }
                }

                var json = this.Json(result);

                ServiceEventSource.Current.Message($"Get all user requests: {json}!");

                return json;
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

        // DELETE: api/values
        [HttpDelete]
        public async Task<IActionResult> Delete()
        {
            try
            {
                string serviceUri = $"{this.serviceContext.CodePackageActivationContext.ApplicationName}/{this.configSettings.RequestsServiceName}";

                ServicePartitionList partitions = await this.fabricClient.QueryManager.GetPartitionListAsync(new Uri(serviceUri));

                List<UserRequest> result = new List<UserRequest>();

                foreach (Partition partition in partitions)
                {
                    ServicePartitionKey key = new ServicePartitionKey(((Int64RangePartitionInformation)partition.PartitionInformation).LowKey);
                    IRequestManager service = ServiceProxy.Create<IRequestManager>(new Uri(serviceUri), key);

                    await service.DeleteAllRequests();
                }

                ServiceEventSource.Current.Message($"Deleted all user requests!");

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
        //public async Task<IActionResult> GetAsync()
        //{
        //    string serviceUri = $"{this.serviceContext.CodePackageActivationContext.ApplicationName}/{configSettings.RequestsServiceName}";

        //    ServicePartitionList partitions = await this.fabricClient.QueryManager.GetPartitionListAsync(new Uri(serviceUri));

        //    List<UserRequest> result = new List<UserRequest>();

        //    foreach (Partition partition in partitions)
        //    {

        //        long partitionKey = ((Int64RangePartitionInformation)partition.PartitionInformation).LowKey;

        //        string proxyUrl =
        //            $"http://localhost:{this.configSettings.ReverseProxyPort}/{serviceUri.Replace("fabric:/", "")}/api/values?PartitionKind={partition.PartitionInformation.Kind}&PartitionKey={partitionKey}";

        //        HttpResponseMessage response = await this.httpClient.GetAsync(proxyUrl);

        //        if (response.StatusCode != System.Net.HttpStatusCode.OK)
        //        {
        //            // if one partition returns a failure, you can either fail the entire request or skip that partition.
        //            return this.StatusCode((int)response.StatusCode);
        //        }

        //        List<UserRequest> list =
        //            JsonConvert.DeserializeObject<List<UserRequest>>(await response.Content.ReadAsStringAsync());

        //        if (list != null && list.Any())
        //        {
        //            result.AddRange(list);
        //        }
        //    }

        //    return this.Json(result);

        //}

        //// PUT api/values
        //[HttpPut]
        //public async Task<IActionResult> PutAsync([FromBody] long value)
        //{
        //    string serviceUri = $"{this.serviceContext.CodePackageActivationContext.ApplicationName.Replace("fabric:/", "")}/{this.configSettings.RequestsServiceName}";

        //    string proxyUrl =
        //        $"http://localhost:{this.configSettings.ReverseProxyPort}/{serviceUri}/api/values/0?PartitionKind=Int64Range&PartitionKey=0";

        //    string payload = $"{{ 'value' : '{value}' }}";
        //    StringContent putContent = new StringContent(payload, Encoding.UTF8, "application/json");
        //    putContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");

        //    HttpResponseMessage response = await this.httpClient.PutAsync(proxyUrl, putContent);

        //    return new ContentResult()
        //    {
        //        StatusCode = (int)response.StatusCode,
        //        Content = await response.Content.ReadAsStringAsync()
        //    };
        //}

    }
}