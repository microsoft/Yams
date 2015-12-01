using System;
using System.Web.Http;

namespace WebApp
{
    [RoutePrefix("coinflip")]
    public class CoinFlipController : ApiController
    {
        private static readonly Random Random = new Random();

        [HttpGet]
        [Route("next")]
        public string Run()
        {
            if (Random.Next(2) == 0)
            {
                return "Heads";
            }
            return "Tails";
        }
    }
}
