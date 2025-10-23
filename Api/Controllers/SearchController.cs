
using Microsoft.AspNetCore.Mvc;
using TotalRecall.Core;

namespace TotalRecall.Api.Controllers
{
    [Route("api/search")]
    [ApiController]
    public class SearchController : ControllerBase
    {
        [HttpPost]
        public async Task Search([FromBody] SearchRequest request)
        {
            var configService = new ConfigurationService();
            var searchIndexService = new SearchIndexService(configService);
            var azureAIService = new AzureAIService(configService);
            var ragService = new RAGService(
               configService, searchIndexService, azureAIService
            );

            var context = await ragService.SearchContextsAsync(request.Query);

            await foreach (var update in azureAIService.GenerateCompletionStreamingAsync(context, request.Query))
            {
                if (update.ContentUpdate.Count > 0)
                {
                    var text = update.ContentUpdate[0].Text;
                    await Response.WriteAsync(text);
                    await Response.Body.FlushAsync();
                }
            }
        }
    }

    public record SearchRequest(string Query);
}
