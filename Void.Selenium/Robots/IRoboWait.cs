﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Void.Selenium
{
    public interface IRoboWait
    {
        TimeSpan Timeout { get; }
        TimeSpan Interval { get; }
        bool IsThrowTimeoutException { get; }
        bool IsIgnoreConditionExceptions { get; }
        Func<Exception, bool> ExceptionHandler { get; }
        CancellationToken CancellationToken { get; }

        Task<bool> UntilAsync(Func<bool> condition);
        Task<bool> UntilAsync(Func<IRoboWaitContext, bool> condition);
        Task<object> UntilAsync(Func<object> condition);
        Task<object> UntilAsync(Func<IRoboWaitContext, object> condition);
        Task<T> UntilAsync<T>(Func<T> condition) where T : class;
        Task<T> UntilAsync<T>(Func<IRoboWaitContext, T> condition) where T : class;
        IRoboWait WithTimeout(TimeSpan value);
        IRoboWait WithInterval(TimeSpan value);
        IRoboWait ThrowTimeoutException();
        IRoboWait NotThrowTimeoutException();
        IRoboWait IgnoreConditionExceptions();
        IRoboWait NotIgnoreConditionExceptions();
        IRoboWait UsingExceptionHandler(Func<Exception, bool> handler);
        IRoboWait RemoveExceptionHandler();
        IRoboWait UsingCancellationToken(CancellationToken token);
        IRoboWait RemoveCancellationToken();
    }
}
