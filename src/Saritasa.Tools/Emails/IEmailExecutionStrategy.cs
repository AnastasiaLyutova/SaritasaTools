﻿// Copyright (c) 2015-2016, Saritasa. All rights reserved.
// Licensed under the BSD license. See LICENSE file in the project root for full license information.

namespace Saritasa.Tools.Emails
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    using NameValueDict = System.Collections.Generic.IDictionary<string, object>;

    /// <summary>
    /// Email execution strategies.
    /// </summary>
    public interface IEmailExecutionStrategy<TMessage> where TMessage : class
    {
        /// <summary>
        /// Execute mail send handler.
        /// </summary>
        /// <param name="handler">Handler.</param>
        /// <param name="message">Mail message.</param>
        /// <param name="data">Additional data.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Task.</returns>
        Task Execute(Func<TMessage, NameValueDict, Task> handler, TMessage message, NameValueDict data,
            CancellationToken cancellationToken);
    }
}
