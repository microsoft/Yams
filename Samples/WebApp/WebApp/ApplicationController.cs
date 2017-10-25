using System.Threading.Tasks;
using System.Web.Http;
using Newtonsoft.Json.Linq;

namespace WebApp
{
    [RoutePrefix("application")]
    public class ApplicationController : ApiController
    {
        [Route("info")]
        public JObject GetInfo()
        {
            string json = $@"
                {{
                    'Id': '{App.Id}',
                    'Version': '{App.Version}',
                    'ClusterId': '{App.ClusterId}',
                    'InstanceId': '{App.InstanceId}'
                }}
                ";

            return JObject.Parse(json);
        }
    }
}
