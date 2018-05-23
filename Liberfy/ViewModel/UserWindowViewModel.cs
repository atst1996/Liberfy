﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Liberfy.ViewModel
{
    internal class UserWindowViewModel : ViewModelBase
    {
        public UserWindowViewModel(UserInfo user, Account account)
        {
            this.User = user;
            this.Account = account;
        }

        public Account Account { get; }

        public UserInfo User { get; }
    }
}
