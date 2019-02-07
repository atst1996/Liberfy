﻿using Liberfy.Services;
using Liberfy.Settings;
using SocialApis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Liberfy
{
    internal interface IAccount
    {
        long Id { get; }
        ServiceType Service { get; }
        IApi Tokens { get; }
        UserInfo Info { get; }
        TimelineBase Timeline { get; }
        bool IsLoading { get; }
        bool IsLoggedIn { get; }
        Task Load();
        IValidator Validator { get; }
        ValueTask<bool> Login();
        Task LoadDetails();
        void SetClient(ApiTokenInfo tokens);
        void Unload();
        AccountItem ToSetting();
    }
}
