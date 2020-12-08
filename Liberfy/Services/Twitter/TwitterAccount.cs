﻿using Liberfy.Managers;
using Liberfy.Services;
using Liberfy.Services.Common;
using Liberfy.Services.Twitter;
using Liberfy.Services.Twitter.Accessors;
using Liberfy.Settings;
using SocialApis;
using SocialApis.Twitter;
using System;
using System.Net;
using System.Threading.Tasks;

namespace Liberfy
{
    internal class TwitterAccount : AccountBase<TwitterApi, TwitterTimeline, User, Status>
    {
        public static readonly Uri ServerHostUrl = new Uri("https://twitter.com/", UriKind.Absolute);
        private static readonly TwitterDataManager _dataFactory = new TwitterDataManager();

        public override long Id { get; protected set; }

        public override ServiceType Service { get; } = ServiceType.Twitter;

        public TwitterDataManager DataStore { get; }

        public TwitterAccount(TwitterAccountItem item)
            : base(item.Id, ServerHostUrl, item.CreateApi(), item)
        {
            this.DataStore = _dataFactory;
            this.Validator = new TwitterValidator(this);
            this.Info = this.DataStore.GetAccount(item);
        }

        public TwitterAccount(TwitterApi api, User account)
            : base((long)account.Id, ServerHostUrl, api)
        {
            this.DataStore = _dataFactory;
            this.Validator = new TwitterValidator(this);
            this.Info = this.GetUserInfo(account);
        }

        //private IAccountCommandGroup _commands;
        //public override IAccountCommandGroup Commands => _commands ?? (_commands = new Twitter.AccountCommandGroup(this));

        public override IValidator Validator { get; }

        public override IServiceConfiguration ServiceConfiguration { get; } = new TwitterServiceConfiguration();

        private IApiGateway _apiGateway;
        public override IApiGateway ApiGateway => this._apiGateway ??= new TwitterApiGateway(this);

        private TwitterStatusAccessor _statuses;
        private TwitterMediaAccessor _media;

        /// <summary>
        /// ツイート関連のアクセサ
        /// </summary>
        public TwitterStatusAccessor Statuses => this._statuses ??= new TwitterStatusAccessor(this);

        /// <summary>
        /// メディア関連のアクセサ
        /// </summary>
        public TwitterMediaAccessor Media => this._media ??= new TwitterMediaAccessor(this);

        protected override TwitterTimeline CreateTimeline() => new TwitterTimeline(this);

        public override async Task Load()
        {
            if (await base.Login().ConfigureAwait(false))
            {
                await this.GetDetails().ConfigureAwait(false);
                this.StartTimeline();
            }
        }

        protected override async Task<bool> VerifyCredentials()
        {
            try
            {
                var user = await this.Api.Account.VerifyCredentials(new Query
                {
                    ["include_entities"] = true,
                    ["skip_status"] = true,
                    ["include_email"] = false
                });

                this.Id = user.Id.Value;

                this.DataStore.RegisterAccount(user);

                return true;
            }
            catch (TwitterException tex)
            {
                Console.WriteLine(tex.Message);

                if (tex.InnerException is WebException wex)
                {
                    switch (wex.Status)
                    {
                        case WebExceptionStatus.Success:
                            break;
                    }
                }
            }

            return false;
        }

        protected override Task GetDetails()
        {
            return Task.CompletedTask;
            //Task[] tasks =
            //{
            //};

            //return Task.WhenAll(tasks);
        }

        protected override IUserInfo GetUserInfo(User account)
        {
            return this.DataStore.RegisterAccount(account);
        }

        public override void SetApiTokens(TwitterApi api)
        {
            this.Api = api;
        }

        public override AccountSettingBase ToSetting()
        {
            return new TwitterAccountItem
            {
                ItemId = this.ItemId,
                Id = this.Id,
                Name = this.Info.Name,
                ScreenName = this.Info.UserName,
                ProfileImageUrl = this.Info.ProfileImageUrl,
                IsProtected = this.Info.IsProtected,
                ConsumerKey = this.Api.ConsumerKey,
                ConsumerSecret = this.Api.ConsumerSecret,
                AccessToken = this.Api.AccessToken,
                AccessTokenSecret = this.Api.AccessTokenSecret,
            };
        }
    }
}
