﻿using Liberfy.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Liberfy.Services
{
    internal interface IValidator
    {
        int MaxPostTextLength { get; }

        int GetTextLength(ServicePostParameters parameters);

        bool CanPost(ServicePostParameters parameters);

        bool CanFavorite(StatusItem item);

        bool CanRetweet(StatusItem item);
    }
}
