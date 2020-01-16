﻿// Copyright 2018 Google LLC
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     https://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Google.Api.Gax;
using Google.Cloud.Spanner.Admin.Database.V1;
using Google.Cloud.Spanner.Common.V1;
using Google.Cloud.Spanner.V1;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static Google.Cloud.Spanner.V1.TransactionOptions.Types;

namespace Google.Cloud.Spanner.Data
{
    public sealed partial class SpannerCommand
    {
        /// <summary>
        /// Class that effectively contains a copy of the parameters of a SpannerCommand, but in a shallow-immutable way.
        /// This means we can validate various things and not worry about them changing. The parameter collection may be modified
        /// externally, along with the SpannerConnection, but other objects should be fine.
        /// 
        /// This class is an implementation detail, used to keep "code required to execute Spanner commands" separate from the ADO
        /// API surface with its mutable properties and many overloads.
        /// </summary>
        private class ExecutableCommand
        {
            private static readonly TransactionOptions s_partitionedDmlTransactionOptions = new TransactionOptions { PartitionedDml = new PartitionedDml() };
            private static readonly TransactionOptions s_readWriteOptions = new TransactionOptions { ReadWrite = new ReadWrite() };

            internal SpannerConnection Connection { get; }
            internal SpannerCommandTextBuilder CommandTextBuilder { get; }
            internal int CommandTimeout { get; }
            internal SpannerTransaction Transaction { get; }
            internal CommandPartition Partition { get; }
            internal SpannerParameterCollection Parameters { get; }

            public ExecutableCommand(SpannerCommand command)
            {
                Connection = command.SpannerConnection;
                CommandTextBuilder = command.SpannerCommandTextBuilder;
                CommandTimeout = command.CommandTimeout;
                Partition = command.Partition;
                Parameters = command.Parameters;
                Transaction = command._transaction;
            }

            // ExecuteScalar is simply implemented in terms of ExecuteReader.
            internal async Task<T> ExecuteScalarAsync<T>(CancellationToken cancellationToken)
            {
                // Duplication of later checks, but this means we can report the right method name.
                ValidateConnectionAndCommandTextBuilder();
                if (CommandTextBuilder.SpannerCommandType != SpannerCommandType.Select)
                {
                    throw new InvalidOperationException("ExecuteScalar functionality is only available for queries.");
                }

                using (var reader = await ExecuteReaderAsync(CommandBehavior.SingleRow, null, cancellationToken).ConfigureAwait(false))
                {
                    bool readValue = await reader.ReadAsync(cancellationToken).ConfigureAwait(false) && reader.FieldCount > 0;
                    return readValue ? reader.GetFieldValue<T>(0) : default;
                }
            }

            // Convenience method for upcasting the from SpannerDataReader to DbDataReader.
            internal async Task<DbDataReader> ExecuteDbDataReaderAsync(CommandBehavior behavior, TimestampBound singleUseReadSettings, CancellationToken cancellationToken) =>
                await ExecuteReaderAsync(behavior, singleUseReadSettings, cancellationToken).ConfigureAwait(false);

            internal async Task<SpannerDataReader> ExecuteReaderAsync(CommandBehavior behavior, TimestampBound singleUseReadSettings, CancellationToken cancellationToken)
            {
                ValidateConnectionAndCommandTextBuilder();
                ValidateCommandBehavior(behavior);

                if (CommandTextBuilder.SpannerCommandType != SpannerCommandType.Select)
                {
                    throw new InvalidOperationException("ExecuteReader functionality is only available for queries.");
                }

                await Connection.EnsureIsOpenAsync(cancellationToken).ConfigureAwait(false);

                // Three transaction options:
                // - A single-use transaction. This doesn't go through a BeginTransaction request; instead, the transaction options are in the request.
                // - One specified in the command
                // - The default based on the connection (may be ephemeral, may be implicit via TransactionScope)

                ISpannerTransaction effectiveTransaction = Transaction ?? Connection.AmbientTransaction;
                if (singleUseReadSettings != null && Transaction != null)
                {
                    throw new InvalidOperationException("singleUseReadSettings cannot be used within another transaction.");
                }
                effectiveTransaction = effectiveTransaction ?? new EphemeralTransaction(Connection, null);

                ExecuteSqlRequest request = GetExecuteSqlRequest();

                if (singleUseReadSettings != null)
                {
                    request.Transaction = new TransactionSelector { SingleUse = singleUseReadSettings.ToTransactionOptions() };
                }

                Connection.Logger.SensitiveInfo(() => $"SpannerCommand.ExecuteReader.Query={request.Sql}");

                // Execute the command. Note that the command timeout here is only used for ambient transactions where we need to set a commit timeout.
                var resultSet = await effectiveTransaction.ExecuteQueryAsync(request, cancellationToken, CommandTimeout)
                    .ConfigureAwait(false);
                var conversionOptions = SpannerConversionOptions.ForConnection(Connection);
                var enableGetSchemaTable = Connection.Builder.EnableGetSchemaTable;
                // When the data reader is closed, we may need to dispose of the connection.
                IDisposable resourceToClose = (behavior & CommandBehavior.CloseConnection) == CommandBehavior.CloseConnection ? Connection : null;

                return new SpannerDataReader(Connection.Logger, resultSet, resourceToClose, conversionOptions, enableGetSchemaTable, CommandTimeout);
            }

            internal async Task<IReadOnlyList<CommandPartition>> GetReaderPartitionsAsync(long? partitionSizeBytes, long? maxPartitions, CancellationToken cancellationToken)
            {
                ValidateConnectionAndCommandTextBuilder();

                GaxPreconditions.CheckState(Transaction?.Mode == TransactionMode.ReadOnly,
                    "GetReaderPartitions can only be executed within an explicitly created read-only transaction.");

                await Connection.EnsureIsOpenAsync(cancellationToken).ConfigureAwait(false);

                ExecuteSqlRequest executeSqlRequest = GetExecuteSqlRequest();
                var tokens = await Transaction.GetPartitionTokensAsync(executeSqlRequest, partitionSizeBytes, maxPartitions, cancellationToken, CommandTimeout).ConfigureAwait(false);
                return tokens.Select(
                    x => {
                        var request = executeSqlRequest.Clone();
                        request.PartitionToken = x;
                        return new CommandPartition(request);
                    }).ToList();
            }

            internal Task<int> ExecuteNonQueryAsync(CancellationToken cancellationToken)
            {
                ValidateConnectionAndCommandTextBuilder();

                switch (CommandTextBuilder.SpannerCommandType)
                {
                    case SpannerCommandType.Ddl:
                        return ExecuteDdlAsync(cancellationToken);
                    case SpannerCommandType.Delete:
                    case SpannerCommandType.Insert:
                    case SpannerCommandType.InsertOrUpdate:
                    case SpannerCommandType.Update:
                        return ExecuteMutationsAsync(cancellationToken);
                    case SpannerCommandType.Dml:
                        return ExecuteDmlAsync(cancellationToken);
                    default:
                        throw new InvalidOperationException("ExecuteNonQuery functionality is only available for DML and DDL commands");
                }
            }

            internal async Task<long> ExecutePartitionedUpdateAsync(CancellationToken cancellationToken)
            {
                ValidateConnectionAndCommandTextBuilder();
                GaxPreconditions.CheckState(Transaction is null && Connection.AmbientTransaction is null, "Partitioned updates cannot be executed within another transaction");
                GaxPreconditions.CheckState(CommandTextBuilder.SpannerCommandType == SpannerCommandType.Dml, "Only general DML commands can be executed in as partitioned updates");
                await Connection.EnsureIsOpenAsync(cancellationToken).ConfigureAwait(false);
                ExecuteSqlRequest request = GetExecuteSqlRequest();

                var transaction = new EphemeralTransaction(Connection, s_partitionedDmlTransactionOptions);
                // Note: no commit here. PDML transactions are implicitly committed as they go along.
                return await transaction.ExecuteDmlAsync(request, cancellationToken, CommandTimeout).ConfigureAwait(false);
            }

            

            private void ValidateConnectionAndCommandTextBuilder()
            {
                GaxPreconditions.CheckState(Connection != null, "SpannerCommand can only be executed when a connection is assigned.");
                GaxPreconditions.CheckState(CommandTextBuilder != null, "SpannerCommand can only be executed when command text is assigned.");
            }

            private async Task<int> ExecuteDmlAsync(CancellationToken cancellationToken)
            {
                await Connection.EnsureIsOpenAsync(cancellationToken).ConfigureAwait(false);
                var transaction = Transaction ?? Connection.AmbientTransaction ?? new EphemeralTransaction(Connection, s_readWriteOptions);
                ExecuteSqlRequest request = GetExecuteSqlRequest();
                long count = await transaction.ExecuteDmlAsync(request, cancellationToken, CommandTimeout).ConfigureAwait(false);
                // This cannot currently exceed int.MaxValue due to Spanner commit limitations anyway.
                return checked((int)count);
            }

            private async Task<int> ExecuteDdlAsync(CancellationToken cancellationToken)
            {
                string commandText = CommandTextBuilder.CommandText;
                var builder = Connection.Builder;
                var channelOptions = new SpannerClientCreationOptions(builder);
                var credentials = await channelOptions.GetCredentialsAsync().ConfigureAwait(false);
                var channel = new Channel(channelOptions.Endpoint.Host, channelOptions.Endpoint.Port, credentials);
                try
                {
                    var databaseAdminClient = DatabaseAdminClient.Create(channel);
                    if (CommandTextBuilder.IsCreateDatabaseCommand)
                    {
                        var parent = new InstanceName(Connection.Project, Connection.SpannerInstance);
                        var request = new CreateDatabaseRequest
                        {
                            ParentAsInstanceName = parent,
                            CreateStatement = CommandTextBuilder.CommandText,
                            ExtraStatements = { CommandTextBuilder.ExtraStatements ?? new string[0] }
                        };
                        var response = await databaseAdminClient.CreateDatabaseAsync(request).ConfigureAwait(false);
                        response = await response.PollUntilCompletedAsync().ConfigureAwait(false);
                        if (response.IsFaulted)
                        {
                            throw SpannerException.FromOperationFailedException(response.Exception);
                        }
                    }
                    else if (CommandTextBuilder.IsDropDatabaseCommand)
                    {
                        if (CommandTextBuilder.ExtraStatements?.Count > 0)
                        {
                            throw new InvalidOperationException(
                                "Drop database commands do not support additional ddl statements");
                        }
                        var dbName = new DatabaseName(Connection.Project, Connection.SpannerInstance, CommandTextBuilder.DatabaseToDrop);
                        await databaseAdminClient.DropDatabaseAsync(dbName, cancellationToken).ConfigureAwait(false);
                    }
                    else
                    {
                        if (builder.DatabaseName == null)
                        {
                            throw new InvalidOperationException(
                                "DDL commands other than CREATE/DROP DATABASE require a database in the data source");
                        }

                        var request = new UpdateDatabaseDdlRequest
                        {
                            DatabaseAsDatabaseName = builder.DatabaseName,
                            Statements = { commandText, CommandTextBuilder.ExtraStatements ?? Enumerable.Empty<string>() }
                        };

                        var response = await databaseAdminClient.UpdateDatabaseDdlAsync(request).ConfigureAwait(false);
                        response = await response.PollUntilCompletedAsync().ConfigureAwait(false);
                        if (response.IsFaulted)
                        {
                            throw SpannerException.FromOperationFailedException(response.Exception);
                        }
                    }
                }
                catch (RpcException gRpcException)
                {
                    //we translate rpc errors into a spanner exception
                    throw new SpannerException(gRpcException);
                }
                finally
                {
                    await channel.ShutdownAsync().ConfigureAwait(false);
                }

                return 0;
            }

            private async Task<int> ExecuteMutationsAsync(CancellationToken cancellationToken)
            {
                await Connection.EnsureIsOpenAsync(cancellationToken).ConfigureAwait(false);
                var mutations = GetMutations();
                var transaction = Transaction ?? Connection.AmbientTransaction ?? new EphemeralTransaction(Connection, s_readWriteOptions);
                // Make the request. This will commit immediately or not depending on whether a transaction was explicitly created.
                await transaction.ExecuteMutationsAsync(mutations, cancellationToken, CommandTimeout).ConfigureAwait(false);
                // Return the number of records affected.
                return mutations.Count;
            }

            private List<Mutation> GetMutations()
            {
                // Currently, ToProtobufValue doesn't use the options it's provided. They're only
                // required to prevent us from accidentally adding call sites that wouldn't be able to obtain
                // valid options. For efficiency, we just pass in null for now. If we ever need real options
                // from the connection string, uncomment the following line to initialize the options from the connection.
                // SpannerConversionOptions options = SpannerConversionOptions.ForConnection(SpannerConnection);
                SpannerConversionOptions conversionOptions = null;

                // Whatever we do with the parameters, we'll need them in a ListValue.
                var listValue = new ListValue
                {
                    Values = { Parameters.Select(x => x.SpannerDbType.ToProtobufValue(x.GetValidatedValue(), conversionOptions)) }
                };

                if (CommandTextBuilder.SpannerCommandType != SpannerCommandType.Delete)
                {
                    var w = new Mutation.Types.Write
                    {
                        Table = CommandTextBuilder.TargetTable,
                        Columns = { Parameters.Select(x => x.SourceColumn ?? x.ParameterName) },
                        Values = { listValue }
                    };
                    switch (CommandTextBuilder.SpannerCommandType)
                    {
                        case SpannerCommandType.Update:
                            return new List<Mutation> { new Mutation { Update = w } };
                        case SpannerCommandType.Insert:
                            return new List<Mutation> { new Mutation { Insert = w } };
                        case SpannerCommandType.InsertOrUpdate:
                            return new List<Mutation> { new Mutation { InsertOrUpdate = w } };
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
                else
                {
                    var w = new Mutation.Types.Delete
                    {
                        Table = CommandTextBuilder.TargetTable,
                        KeySet = new KeySet { Keys = { listValue } }
                    };
                    return new List<Mutation> { new Mutation { Delete = w } };
                }
            }

            private ExecuteSqlRequest GetExecuteSqlRequest()
            {
                if (Partition != null)
                {
                    return Partition.ExecuteSqlRequest;
                }

                var request = new ExecuteSqlRequest
                {
                    Sql = CommandTextBuilder.ToString()
                };

                // See comment at the start of GetMutations.
                SpannerConversionOptions options = null;
                Parameters.FillSpannerCommandParams(out var parameters, request.ParamTypes, options);
                request.Params = parameters;

                return request;
            }

            private static void ValidateCommandBehavior(CommandBehavior behavior)
            {
                if ((behavior & CommandBehavior.KeyInfo) == CommandBehavior.KeyInfo)
                {
                    throw new NotSupportedException(
                        $"{nameof(CommandBehavior.KeyInfo)} is not supported by Cloud Spanner.");
                }
                if ((behavior & CommandBehavior.SchemaOnly) == CommandBehavior.SchemaOnly)
                {
                    throw new NotSupportedException(
                        $"{nameof(CommandBehavior.SchemaOnly)} is not supported by Cloud Spanner.");
                }
            }
        }
    }
}
