﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Liberfy
{
    internal interface IUnsubscribableObserver<T> : IObservable<T>
    {
        void Unsubscribe(IObserver<T> observer);
    }
}
