using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace LCModManager
{
    namespace DownloadManager
    {
        public class REST_Endpoint
        {
            private HttpClient HTTPClient;

            public REST_Endpoint()
            {
                HTTPClient = new();
            }

            async public Task<object> Get(Uri uri)
            {
                using HttpResponseMessage response = await HTTPClient.GetAsync(uri);

                return "";
            }
        }
    }
}
