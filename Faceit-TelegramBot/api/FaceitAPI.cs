using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Faceit_TelegramBot.Model;
using Newtonsoft.Json;

namespace Faceit_TelegramBot.api
{
    class FaceitAPI
    {
        public HttpClient _client { get; set; }

        public FaceitAPI() 
        {
            _client = new ();
        }

        public async Task<SearchResponseModel?> Search(int offset, int limit, string query)
        {
            string url = "https://www.faceit.com/api/search/v1/";

            if (offset == 0)
                url += "?offset=0";
            else
                url += $"?offset={offset}";

            if (limit == 0)
                url += "&limit=5";
            else
                url += $"&limit={limit}";

            if (query == string.Empty)
                return null;

            url += $"&query={query}";

            HttpResponseMessage response = await _client.GetAsync(url);
            string responseString = await response.Content.ReadAsStringAsync();

            SearchResponseModel? responseModel = JsonConvert.DeserializeObject<SearchResponseModel>(responseString);

            if (responseModel != null)
            {
                responseModel.url = url;
                return responseModel;
            }

            return null;
        }
    }
}
