using System;
using System.Threading.Tasks;
using System.Web.Http;
using GrainInterfaces;
using Orleans;

namespace WebApp
{
    [RoutePrefix("orleans")]
    public class OrleansHelloController : ApiController
    {
        [HttpGet]
        [Route("hello")]
        public async Task<string> SayHello()
        {
            var helloGrain = GrainClient.GrainFactory.GetGrain<IHelloGrain>(0);
            return await helloGrain.SayHello();
        }
    }
}
