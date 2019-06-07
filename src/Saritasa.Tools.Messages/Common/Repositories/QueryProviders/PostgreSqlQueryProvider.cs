﻿// Copyright (c) 2015-2019, Saritasa. All rights reserved.
// Licensed under the BSD license. See LICENSE file in the project root for full license information.

using System;
using Saritasa.Tools.Messages.Internal;

namespace Saritasa.Tools.Messages.Common.Repositories.QueryProviders
{
    /// <summary>
    /// PostgreSQL SQL scripts.
    /// </summary>
    internal class PostgreSqlQueryProvider : IMessageQueryProvider
    {
        private const string TableName = "saritasa_messages";

        private readonly IObjectSerializer serializer;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="serializer">Used object serializer.</param>
        public PostgreSqlQueryProvider(IObjectSerializer serializer)
        {
            if (serializer == null)
            {
                throw new ArgumentNullException(nameof(serializer));
            }
            this.serializer = serializer;
        }

        /// <inheritdoc />
        public string GetCreateTableScript()
        {
            return $@"
                CREATE TABLE {TableName} (
                    id bigserial primary key,
                    type smallint not null,
                    content_id uuid not null,
                    content_type varchar(255) not null,
                    content {(serializer.IsText ? "text" : "bytea")} not null,
                    data {(serializer.IsText ? "text" : "bytea")},
                    error_details {(serializer.IsText ? "text" : "bytea")},
                    error_message varchar(255) not null default '',
                    error_type varchar(255) not null default '',
                    created_at timestamp not null,
                    execution_duration int not null,
                    status smallint not null
                );
                CREATE INDEX ix_content_id ON {TableName} (content_id);
                CREATE INDEX ix_content_type ON {TableName} (content_type);
                CREATE INDEX ix_error_type ON {TableName} (error_type);";
        }

        /// <inheritdoc />
        public string GetExistsTableScript()
        {
            return $"SELECT 1 FROM information_schema.tables WHERE table_name = '{TableName}';";
        }

        /// <inheritdoc />
        public string GetInsertMessageScript()
        {
            return $@"
                INSERT INTO {TableName} (type, content_id, content_type, content, data, error_details, error_message, error_type, created_at, execution_duration, status)
                VALUES (@Type, @ContentId::uuid, @ContentType, @Content, @Data, @ErrorDetails, @ErrorMessage, @ErrorType, @CreatedAt, @ExecutionDuration, @Status);
            ";
        }

        private static readonly string[] fieldsList =
        {
            "type",
            "content_id",
            "content_type",
            "content",
            "data",
            "error_details",
            "error_message",
            "error_type",
            "created_at",
            "execution_duration",
            "status",
        };

        /// <inheritdoc />
        public string GetFilterScript(MessageQuery messageQuery)
        {
            if (messageQuery == null)
            {
                throw new ArgumentNullException(nameof(messageQuery));
            }

            return BuildSelectString(messageQuery, new MySqlSelectStringBuilder());
        }

        private static string BuildSelectString(MessageQuery messageQuery, ISelectStringBuilder ssb)
        {
            ssb.SelectAll().From(TableName);

            if (messageQuery.Id != null)
            {
                ssb.Where("content_id").EqualsTo(messageQuery.Id);
            }
            if (messageQuery.CreatedStartDate != null)
            {
                ssb.Where("created_at").GreaterOrEqualsTo(messageQuery.CreatedStartDate);
            }
            if (messageQuery.CreatedEndDate != null)
            {
                ssb.Where("created_at").LessOrEqualsTo(messageQuery.CreatedEndDate);
            }
            if (messageQuery.ContentType != null)
            {
                ssb.Where("content_type").Like(messageQuery.ContentType + "%");
            }
            if (messageQuery.ErrorType != null)
            {
                ssb.Where("error_type").Like(messageQuery.ErrorType + "%");
            }
            if (messageQuery.Status != null)
            {
                ssb.Where("status").EqualsTo((byte)messageQuery.Status);
            }
            if (messageQuery.Type != null)
            {
                ssb.Where("type").EqualsTo(messageQuery.Type);
            }
            if (messageQuery.ExecutionDurationAbove != null)
            {
                ssb.Where("execution_duration").GreaterOrEqualsTo(messageQuery.ExecutionDurationAbove);
            }
            if (messageQuery.ExecutionDurationBelow != null)
            {
                ssb.Where("execution_duration").LessOrEqualsTo(messageQuery.ExecutionDurationBelow);
            }
            if (messageQuery.Skip > 0)
            {
                ssb.Skip(messageQuery.Skip);
            }
            if (messageQuery.Take > 0)
            {
                ssb.Take(messageQuery.Take);
            }

            return ssb.Build();
        }
    }
}
