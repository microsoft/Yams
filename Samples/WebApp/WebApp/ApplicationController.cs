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
            string json = string.Format(@"
                {{
                    'Id': '{0}',
                    'Version': '{1}',
                    'Cloud Service Deployment Id': '{2}'
                }}
                ", App.Id, App.Version, App.ClusterId);

            return JObject.Parse(json);
        }
    }
}
