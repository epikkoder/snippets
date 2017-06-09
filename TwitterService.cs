using AutoMapper;
using LinqToTwitter;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System;

namespace System.Web.Services
{
    public class TwitterService : BaseService, ITwitterService
    {
        private ICacheService _cacheService = null;
        IConfigService _configSrv = null;
        IAccountSettingsService _settingSrv = null;

        public TwitterService(ICacheService cacheService, IConfigService configInject, IAccountSettingsService settingInject)
        {
            _cacheService = cacheService;
            _configSrv = configInject;
            _settingSrv = settingInject;
        }

        IMapper mapTweets = new MapperConfiguration(cfg => cfg.CreateMap<Status, TwitterStatus>()).CreateMapper();
        IMapper mapEmbedStatus = new MapperConfiguration(cfg => cfg.CreateMap<EmbeddedStatus, TwitterEmbeddedStatus>()).CreateMapper();

        public SingleUserAuthorizer SetAuthInformation()
        {
            SingleUserAuthorizer auth = new SingleUserAuthorizer
            {
                CredentialStore = new SingleUserInMemoryCredentialStore
                {
                    ConsumerKey = _configSrv.TwitterConsumerKey,
                    ConsumerSecret = _configSrv.TwitterConsumerSecret,
                    AccessToken = _configSrv.TwitterAccessToken,
                    AccessTokenSecret = _configSrv.TwitterAccessTokenSecret
                }
            };

            return auth;
        }

        /* Returns the latest nth (the tweet.Count) tweets in JSON. */
        public async Task<List<TwitterStatus>> GetTweetsByHandle(string handle, int pgIndex = 0, int pgSize = 10)
        {
            List<TwitterStatus> tweets = null;
            //PagedList<TwitterStatus> pagedList = null;
            AccountSetting setting = _settingSrv.GetSettingByHandle(handle, Enums.AccountSettings.TwitterToken);

            if (setting != null)
            {
                int count = pgSize * (pgIndex + 1);
                SingleUserAuthorizer auth = SetAuthInformation();
                TwitterContext context = new TwitterContext(auth);

                tweets =
                    await
                    (
                    from tweet in context.Status

                    where
                        tweet.Type == StatusType.User &&
                        tweet.ScreenName == setting.Value &&
                        tweet.Count == count

                    select mapTweets.Map<Status, TwitterStatus>(tweet)
                    )
                    .ToListAsync<TwitterStatus>();
            }
            return tweets;
        }

        /* Returns the latest nth (the tweet.Count) tweets in JSON. (with parameter of last shown tweet) */
        public async Task<List<TwitterStatus>> GetTweetsByHandleNMax(string handle, string maxIdStr, int pgSize = 10)
        {
            /* Gets the MaxID from the database. */
            ulong storedMaxId = 0;
            if (maxIdStr.Length > 0)
            {
                storedMaxId = GetMaxId(maxIdStr);
            }

            List<TwitterStatus> tweets = null;
            AccountSetting setting = _settingSrv.GetSettingByHandle(handle, Enums.AccountSettings.TwitterToken);

            if (setting != null)
            {
                SingleUserAuthorizer auth = SetAuthInformation();
                TwitterContext context = new TwitterContext(auth);

                tweets =
                    await
                    (
                    from tweet in context.Status

                    where
                        tweet.Type == StatusType.User &&
                        tweet.ScreenName == setting.Value &&
                        tweet.Count == pgSize &&
                        tweet.MaxID == ulong.Parse(maxIdStr) - 1 &&
                        tweet.IncludeMyRetweet == false &&
                        tweet.IncludeRetweets == false

                    select mapTweets.Map<Status, TwitterStatus>(tweet)
                    )
                    .ToListAsync<TwitterStatus>();

                /* Gets the statusID of the last tweet. */
                decimal maxId = 0;
                if (tweets.Count > 0)
                {
                    maxId = tweets.LastOrDefault().StatusID;
                }

                DeleteMaxId();
                InsertMaxId(maxId);

            }
            return tweets;
        }

        public async Task<List<TwitterEmbeddedStatus>> GetTweetEmbedStatus()
        {
            SingleUserAuthorizer auth = SetAuthInformation();
            TwitterContext context = new TwitterContext(auth);

            List<TwitterEmbeddedStatus> embedStatusList = null;
            List<TwitterStatus> tweets = await GetTweets();
            var key = "twitter_" + tweets[0].ScreenName;
            DateTimeOffset expiration = DateTimeOffset.Now.AddMinutes(20);

            if (!_cacheService.Contains(key))
            {


                EmbeddedStatusAlignment align = EmbeddedStatusAlignment.Center;

                if (tweets != null)
                {
                    embedStatusList = new List<TwitterEmbeddedStatus>();
                }

                foreach (TwitterStatus t in tweets)
                {
                    var embedStatus =
                        await
                        (
                        from tweet in context.Status

                        where
                            tweet.Type == StatusType.Oembed &&
                            tweet.ID == t.StatusID &&
                            tweet.OEmbedMaxWidth == 350 &&
                            tweet.OEmbedAlign == align

                        select mapEmbedStatus.Map<EmbeddedStatus, TwitterEmbeddedStatus>(tweet.EmbeddedStatus)
                        )
                        .SingleOrDefaultAsync();

                    embedStatus.TweetId = t.StatusID;
                    embedStatusList.Add(embedStatus);
                }
                if (key != null)
                {
                    _cacheService.Add(key, embedStatusList, expiration);

                }
                return embedStatusList;
            }

            embedStatusList = _cacheService.Get<List<TwitterEmbeddedStatus>>(key);
            return embedStatusList;
        }

        /* This returns a list of objects with an html property
         * that contains the embeddable html tweet to be used for the View. */
        public async Task<List<TwitterEmbeddedStatus>> GetTweetEmbedStatus(string maxIdStr)
        {
            SingleUserAuthorizer auth = SetAuthInformation();
            TwitterContext context = new TwitterContext(auth);

            List<TwitterEmbeddedStatus> embedStatusList = null;
            List<TwitterStatus> tweets = await GetTweets(maxIdStr);

            var key = "twitter_" + tweets[0].ScreenName + maxIdStr;
            DateTimeOffset expiration = DateTimeOffset.Now.AddMinutes(20);

            if (!_cacheService.Contains(key))
            {
                EmbeddedStatusAlignment align = EmbeddedStatusAlignment.Center;

                if (tweets != null)
                {
                    embedStatusList = new List<TwitterEmbeddedStatus>();
                }

                foreach (TwitterStatus t in tweets)
                {
                    var embedStatus =
                        await
                        (
                        from tweet in context.Status

                        where
                            tweet.Type == StatusType.Oembed &&
                            tweet.ID == t.StatusID &&
                            tweet.OEmbedMaxWidth == 350 &&
                            tweet.OEmbedAlign == align

                        select mapEmbedStatus.Map<EmbeddedStatus, TwitterEmbeddedStatus>(tweet.EmbeddedStatus)
                        )
                        .SingleOrDefaultAsync();

                    embedStatus.TweetId = t.StatusID;
                    embedStatusList.Add(embedStatus);
                }
                if (key != null)
                {
                    _cacheService.Add(key, embedStatusList, expiration);

                }
                return embedStatusList;
            }
            embedStatusList = _cacheService.Get<List<TwitterEmbeddedStatus>>(key);
            return embedStatusList;
        }

        public void InsertMaxId(decimal maxId)
        {
            string maxIdStr = maxId.ToString();
            DataProvider.ExecuteNonQuery
            (
                GetConnection
                , "dbo.TwitterMaxId_Insert"
                , inputParamMapper: delegate (SqlParameterCollection paramCollection)
                {
                    paramCollection.AddWithValue("@MaxId", maxId);
                    paramCollection.AddWithValue("@MaxIdStr", maxIdStr);
                }
            );
        }

        public ulong GetMaxId(string maxIdStr)
        {
            Status s = new Status();
            int index = 0;

            DataProvider.ExecuteCmd
            (
                GetConnection
                , "dbo.TwitterMaxId_Get"
                , inputParamMapper: delegate (SqlParameterCollection paramCollection)
                {
                    paramCollection.AddWithValue("@MaxIdStr", maxIdStr);
                }
                , map: delegate (IDataReader reader, short set)
                {
                    s.StatusID = (ulong)reader.GetSafeDecimal(index++);
                }
            );

            return s.StatusID;
        }

        public void DeleteMaxId()
        {
            DataProvider.ExecuteNonQuery
            (
                GetConnection
                , "dbo.TwitterMaxId_Delete"
                , inputParamMapper: null
            );
        }

        public async Task<List<TwitterEmbeddedStatus>> EmbedTweets(List<TwitterStatus> list, int cacheKey)
        {
            TwitterContext context = new TwitterContext(SetAuthInformation());
            List<TwitterEmbeddedStatus> embedStatusList = null;
            //PagedList<TwitterEmbeddedStatus> pagedList = null;

            var key = "twitter_" + list[0].ScreenName + "_pg" + cacheKey;
            DateTimeOffset expiration = DateTimeOffset.Now.AddMinutes(20);

            if (!_cacheService.Contains(key))
            {
                EmbeddedStatusAlignment align = EmbeddedStatusAlignment.Center;

                if (list != null)
                {
                    embedStatusList = new List<TwitterEmbeddedStatus>();
                }

                foreach (TwitterStatus t in list)
                {
                    var embedStatus =
                        await
                        (
                        from tweet in context.Status

                        where
                            tweet.Type == StatusType.Oembed &&
                            tweet.ID == t.StatusID &&
                            tweet.OEmbedMaxWidth == 350 &&
                            tweet.OEmbedAlign == align

                        select mapEmbedStatus.Map<EmbeddedStatus, TwitterEmbeddedStatus>(tweet.EmbeddedStatus)
                        )
                        .SingleOrDefaultAsync();

                    embedStatus.TweetId = t.StatusID;
                    embedStatusList.Add(embedStatus);
                }
                if (key != null)
                {
                    _cacheService.Add(key, embedStatusList, expiration);

                }
                //pagedList = new PagedList<TwitterEmbeddedStatus>(embedStatusList, list.PageIndex, list.PageSize, list.TotalCount);

                return embedStatusList;
            }

            embedStatusList = _cacheService.Get<List<TwitterEmbeddedStatus>>(key);
            //pagedList = new PagedList<TwitterEmbeddedStatus>(embedStatusList, list.PageIndex, list.PageSize, list.TotalCount);
            return embedStatusList;
        }
    }
}