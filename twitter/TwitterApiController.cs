using LinqToTwitter;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;

namespace System.Web.Controllers.Api.Twitter
{
    [RoutePrefix("api/twitter")]
    public class TwitterApiController : ApiController
    {
        private ITwitterService _twitterService = null;

        public TwitterApiController(ITwitterService twitterService)
        {
            _twitterService = twitterService;
        }

        [AllowAnonymous]
        [Route, HttpGet]
        public async Task<HttpResponseMessage> GetTweets()
        {
            ItemResponse<object> response = new ItemResponse<object>();

            var result = await _twitterService.GetTweetEmbedStatus();
            response.Item = result;

            return Request.CreateResponse(HttpStatusCode.OK, response);
        }

        [Route("{handle}")]
        [Route("{handle}/{pgIndex:int}")]
        [Route("{handle}/{pgIndex:int}/{pgSize:int}")]
        [HttpGet]
        public async Task<HttpResponseMessage> GetTweetsByHandle(string handle, int pgIndex = 0, int pgSize = 10)
        {
            ItemResponse<object> response = new ItemResponse<object>();

            List<TwitterStatus> tweets = await _twitterService.GetTweetsByHandle(handle, pgIndex, pgSize);
            List<TwitterEmbeddedStatus> embedded = await _twitterService.EmbedTweets(tweets, pgIndex);
            response.Item = embedded;

            return Request.CreateResponse(HttpStatusCode.OK, response);
        }

        [AllowAnonymous]
        [Route("content"), HttpGet]
        public async Task<HttpResponseMessage> GetTweetsContent()
        {
            ItemResponse<object> response = new ItemResponse<object>();

            var result = await _twitterService.GetTweets();
            response.Item = result;

            return Request.CreateResponse(HttpStatusCode.OK, response);
        }

        [AllowAnonymous]
        //[Route("{maxIdStr}"), HttpGet]
        public async Task<HttpResponseMessage> GetTweets(string maxIdStr)
        {
            ItemResponse<object> response = new ItemResponse<object>();

            var result = await _twitterService.GetTweetEmbedStatus(maxIdStr);
            response.Item = result;

            return Request.CreateResponse(HttpStatusCode.OK, response);
        }
    }
}
