using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace MetaWeblog.Portable.XmlRpc
{
    public class Service
    {

        /// <summary>
        /// 
        /// </summary>
        public String Url { get; private set; }



        public Service(string url)
        {
            this.Url = url;
        }

        public async Task<MethodResponse> Execute(MethodCall methodcall)
        {
            var doc = methodcall.CreateDocument();

            var handler = new HttpClientHandler {AllowAutoRedirect = true};
            if (handler.SupportsAutomaticDecompression)
            {
                handler.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
            }

            var client = new HttpClient(handler);
            client.DefaultRequestHeaders.ExpectContinue = false;
            client.DefaultRequestHeaders.Add("user-agent", "MetaWeblogPortable");

            var bytes = Encoding.UTF8.GetBytes(doc.ToString());
            var response = client.PostAsync(Url, new ByteArrayContent(bytes));
            response.Result.EnsureSuccessStatusCode();

            var result = await response.Result.Content.ReadAsStringAsync();
            return new MethodResponse(result);
        }
    }
}