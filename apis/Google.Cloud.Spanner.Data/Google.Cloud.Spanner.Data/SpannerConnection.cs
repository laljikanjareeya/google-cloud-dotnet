﻿// Copyright 2017 Google Inc. All Rights Reserved.
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Google.Api.Gax;
using Google.Api.Gax.Grpc;
using Google.Cloud.Spanner.Common.V1;
using Google.Cloud.Spanner.V1;
using Google.Cloud.Spanner.V1.Internal.Logging;
using Google.Protobuf;
using Grpc.Core;
using System;
using System.ComponentModel;
using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

using Transaction = System.Transactions.Transaction;

namespace Google.Cloud.Spanner.Data
{
    /// <summary>
    /// Represents a connection to a single Spanner database.
    /// When opened, <see cref="SpannerConnection" /> will acquire and maintain a session
    /// with the target Spanner database.
    /// <see cref="SpannerCommand" /> instances using this <see cref="SpannerConnection" />
    /// will use this session to execute their operation. Concurrent read operations can
    /// share this session, but concurrent write operations may cause additional sessions
    /// to be opened to the database.
    /// Underlying sessions with the Spanner database are pooled and are closed after a
    /// configurable
    /// <see>
    /// <cref>SpannerOptions.PoolEvictionDelay</cref>
    /// </see>
    /// .
    /// </summary>
    public sealed class SpannerConnection : DbConnection
    {
        // Read/write transaction options; no additional state, so can be reused.
        internal static readonly TransactionOptions s_readWriteTransactionOptions = new TransactionOptions { ReadWrite = new TransactionOptions.Types.ReadWrite() };

        private readonly object _sync = new object();

        // The SessionPool to use to allocate sessions. This is obtained from the SessionPoolManager,
        // and released when the connection is closed/disposed.
        private SessionPool _sessionPool;

        private ConnectionState _state = ConnectionState.Closed;

        // State used for TransactionScope-based transactions.
        private VolatileResourceManager _volatileResourceManager;

        /// <inheritdoc />
        public override string ConnectionString
        {
            get => Builder.ToString();
            set => TrySetNewConnectionInfo(new SpannerConnectionStringBuilder(value, Builder.CredentialOverride, Builder.SessionPoolManager));
        }

        /// <inheritdoc />
        public override string Database => Builder.SpannerDatabase;

        /// <inheritdoc />
        public override string DataSource => Builder.DataSource;

        /// <summary>
        /// The Spanner project name.
        /// </summary>
        [Category("Data")]
        public string Project => Builder.Project;

        /// <inheritdoc />
        public override string ServerVersion => "0.0";

        /// <summary>
        /// The Spanner instance name
        /// </summary>
        [Category("Data")]
        public string SpannerInstance => Builder.SpannerInstance;

        /// <summary>
        /// The logger used by this connection. This is never null.
        /// </summary>
        internal Logger Logger => Builder.SessionPoolManager.Logger;

        /// <inheritdoc />
        public override ConnectionState State
        {
            get
            {
                lock (_sync)
                {
                    return _state;
                }
            }
        }

        internal bool IsClosed => (State & ConnectionState.Open) == 0;

        internal bool IsOpen => (State & ConnectionState.Open) == ConnectionState.Open;

        /// <summary>
        /// Creates a SpannerConnection with no datasource or credential specified.
        /// </summary>
        public SpannerConnection() : this(new SpannerConnectionStringBuilder())
        {
        }

        /// <summary>
        /// Creates a SpannerConnection with a datasource contained in connectionString
        /// and optional credential information supplied in connectionString or the credential
        /// argument.
        /// </summary>
        /// <param name="connectionString">
        /// A Spanner formatted connection string. This is usually of the form
        /// `Data Source=projects/{project}/instances/{instance}/databases/{database};[Host={hostname};][Port={portnumber}]`
        /// </param>
        /// <param name="credentials">An optional credential for operations to be performed on the Spanner database.  May be null.</param>
        public SpannerConnection(string connectionString, ChannelCredentials credentials = null)
            : this(new SpannerConnectionStringBuilder(connectionString, credentials)) { }

        /// <summary>
        /// Creates a SpannerConnection with a datasource contained in connectionString.
        /// </summary>
        /// <param name="connectionStringBuilder">
        /// A SpannerConnectionStringBuilder containing a formatted connection string.  Must not be null.
        /// </param>
        public SpannerConnection(SpannerConnectionStringBuilder connectionStringBuilder)
        {
            GaxPreconditions.CheckNotNull(connectionStringBuilder, nameof(connectionStringBuilder));
            TrySetNewConnectionInfo(connectionStringBuilder);
        }

        /// <summary>
        /// Begins a read-only transaction using the optionally provided <see cref="CancellationToken" />.
        /// Read transactions are preferred if possible because they do not impose locks internally.
        /// ReadOnly transactions run with strong consistency and return the latest copy of data.
        /// This method is thread safe.
        /// </summary>
        /// <param name="cancellationToken">An optional token for canceling the call. May be null.</param>
        /// <returns>The newly created <see cref="SpannerTransaction"/>.</returns>
        public Task<SpannerTransaction> BeginReadOnlyTransactionAsync(
            CancellationToken cancellationToken = default) => BeginReadOnlyTransactionAsync(
            TimestampBound.Strong, cancellationToken);

        /// <summary>
        /// Begins a read-only transaction using the optionally provided <see cref="CancellationToken" />
        /// and provided <see cref="TimestampBound" /> to control the read timestamp and/or staleness
        /// of data.
        /// Read transactions are preferred if possible because they do not impose locks internally.
        /// Stale read-only transactions can execute more quickly than strong or read-write transactions,.
        /// This method is thread safe.
        /// </summary>
        /// <param name="targetReadTimestamp">Specifies the timestamp or allowed staleness of data. Must not be null.</param>
        /// <param name="cancellationToken">An optional token for canceling the call.</param>
        /// <returns>The newly created <see cref="SpannerTransaction"/>.</returns>
        public Task<SpannerTransaction> BeginReadOnlyTransactionAsync(
            TimestampBound targetReadTimestamp,
            CancellationToken cancellationToken = default)
        {
            GaxPreconditions.CheckNotNull(targetReadTimestamp, nameof(targetReadTimestamp));
            if (targetReadTimestamp.Mode == TimestampBoundMode.MinReadTimestamp
                || targetReadTimestamp.Mode == TimestampBoundMode.MaxStaleness)
            {
                throw new ArgumentException(
                    nameof(targetReadTimestamp),
                    $"{nameof(TimestampBoundMode.MinReadTimestamp)} and "
                    + $"{nameof(TimestampBoundMode.MaxStaleness)} can only be used in a single-use"
                    + " transaction as an argument to SpannerCommand.ExecuteReader().");
            }

            return BeginTransactionImplAsync(
                targetReadTimestamp.ToTransactionOptions(),
                TransactionMode.ReadOnly,
                cancellationToken,
                targetReadTimestamp);
        }

        /// <summary>
        /// Begins a read-only transaction.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Read-only transactions are preferred if possible because they do not impose locks internally.
        /// Read-only transactions run with strong consistency and return the latest copy of data.
        /// </para>
        /// <para>This method is thread safe.</para>
        /// </remarks>
        /// <returns>The newly created <see cref="SpannerTransaction"/>.</returns>
        public SpannerTransaction BeginReadOnlyTransaction() => BeginReadOnlyTransaction(TimestampBound.Strong);

        /// <summary>
        /// Begins a read-only transaction using the provided <see cref="TimestampBound"/> to control the read timestamp
        /// and/or staleness of data.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Read-only transactions are preferred if possible because they do not impose locks internally.
        /// Read-only transactions run with strong consistency and return the latest copy of data.
        /// </para>
        /// <para>This method is thread safe.</para>
        /// </remarks>
        /// <param name="targetReadTimestamp">Specifies the timestamp or allowed staleness of data. Must not be null.</param>
        /// <returns>The newly created <see cref="SpannerTransaction"/>.</returns>
        public SpannerTransaction BeginReadOnlyTransaction(TimestampBound targetReadTimestamp) =>
            Task.Run(() => BeginReadOnlyTransactionAsync(targetReadTimestamp)).ResultWithUnwrappedExceptions();

        /// <summary>
        /// Begins a read-only transaction using the provided <see cref="TransactionId" /> to refer to an existing server-side transaction.
        /// </summary>
        /// <remarks>
        /// Read-only transactions are preferred if possible because they do not impose locks internally.
        /// Providing a transaction ID will connect to an already created transaction which is useful
        /// for batch reads. This method differs from <see cref="BeginReadOnlyTransaction()">the parameterless overload</see>
        /// and <see cref="BeginReadOnlyTransaction(TimestampBound)">the overload accepting a TimestampBound</see> as it
        /// uses an existing transaction rather than creating a new server-side transaction.
        /// </remarks>
        /// <param name="transactionId">Specifies the transaction ID of an existing read-only transaction.</param>
        /// <returns>A <see cref="SpannerTransaction"/> attached to the existing transaction represented by
        /// <paramref name="transactionId"/>.</returns>
        public SpannerTransaction BeginReadOnlyTransaction(TransactionId transactionId)
        {
            Open();

            GaxPreconditions.CheckNotNull(transactionId, nameof(transactionId));
            SessionName sessionName = SessionName.Parse(transactionId.Session);
            ByteString transactionIdBytes = ByteString.FromBase64(transactionId.Id);
            var session = _sessionPool.CreateDetachedSession(sessionName, transactionIdBytes, TransactionOptions.ModeOneofCase.ReadOnly);
            // This transaction is coming from another process potentially, so we don't auto close it.
            return new SpannerTransaction(this, TransactionMode.ReadOnly, session, transactionId.TimestampBound)
            {
                Shared = true,
                DisposeBehavior = DisposeBehavior.Detach
            };
        }

        /// <summary>
        /// Begins a new Spanner transaction synchronously. This method hides <see cref="DbConnection.BeginTransaction()"/>, but behaves
        /// the same way, just with a more specific return type.
        /// </summary>
        public new SpannerTransaction BeginTransaction() => (SpannerTransaction)base.BeginTransaction();

        /// <summary>
        /// Begins a new read/write transaction.
        /// This method is thread safe.
        /// </summary>
        /// <param name="cancellationToken">An optional token for canceling the call.</param>
        /// <returns>A new <see cref="SpannerTransaction" /></returns>
        public Task<SpannerTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default) =>
            BeginTransactionImplAsync(s_readWriteTransactionOptions, TransactionMode.ReadWrite, cancellationToken);

        /// <summary>
        /// Executes a read-write transaction, with retries as necessary.
        /// The work to perform in each transaction attempt is defined by <paramref name="asyncWork"/>.
        /// </summary>
        /// <remarks><paramref name="asyncWork"/> will be fully retried whenever the <see cref="SpannerTransaction"/>
        /// that it receives as a parameter aborts. <paramref name="asyncWork"/> won't be retried if any other errors occur.
        /// <paramref name="asyncWork"/> must be prepared to be called more than once.
        /// A new <see cref="SpannerTransaction"/> will be passed to <paramref name="asyncWork"/>
        /// each time it is rerun.
        /// <paramref name="asyncWork"/> doesn't need to handle the lifecycle of the <see cref="SpannerTransaction"/>,
        /// it will be automatically committed after <paramref name="asyncWork"/> has finished or rollbacked if an 
        /// <see cref="Exception"/> (other than because the transaction commit aborted) is thrown by <paramref name="asyncWork"/>.</remarks>
        /// <param name="asyncWork">The work to perform in each transaction attempt.</param>
        /// <param name="cancellationToken">An optional token for canceling the call.</param>
        /// <returns>The value returned by <paramref name="asyncWork"/> if the transaction commits successfully.</returns>
        public async Task<TResult> RunWithRetriableTransactionAsync<TResult>(Func<SpannerTransaction, Task<TResult>> asyncWork, CancellationToken cancellationToken = default)
        {
            GaxPreconditions.CheckNotNull(asyncWork, nameof(asyncWork));

            await OpenAsync(cancellationToken).ConfigureAwait(false);
            RetriableTransaction transaction = new RetriableTransaction(
                this,
                Builder.SessionPoolManager.SpannerSettings.Clock ?? SystemClock.Instance,
                Builder.SessionPoolManager.SpannerSettings.Scheduler ?? SystemScheduler.Instance);
            return await transaction.RunAsync(asyncWork, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Executes a read-write transaction, with retries as necessary.
        /// The work to perform in each transaction attempt is defined by <paramref name="asyncWork"/>.
        /// </summary>
        /// <remarks><paramref name="asyncWork"/> will be fully retried whenever the <see cref="SpannerTransaction"/>
        /// that it receives as a parameter aborts. <paramref name="asyncWork"/> won't be retried if any other errors occur.
        /// <paramref name="asyncWork"/> must be prepared to be called more than once.
        /// A new <see cref="SpannerTransaction"/> will be passed to <paramref name="asyncWork"/>
        /// each time it is rerun.
        /// <paramref name="asyncWork"/> doesn't need to handle the lifecycle of the <see cref="SpannerTransaction"/>,
        /// it will be automatically committed after <paramref name="asyncWork"/> has finished or rollbacked if an 
        /// <see cref="Exception"/> (other than because the transaction commit aborted) is thrown by <paramref name="asyncWork"/>.</remarks>
        /// <param name="asyncWork">The work to perform in each transaction attempt.</param>
        /// <param name="cancellationToken">An optional token for canceling the call.</param>
        /// <returns>A task that when completed will signal that the work is done.</returns>
        public async Task RunWithRetriableTransactionAsync(Func<SpannerTransaction, Task> asyncWork, CancellationToken cancellationToken = default)
        {
            GaxPreconditions.CheckNotNull(asyncWork, nameof(asyncWork));
            await RunWithRetriableTransactionAsync(async transaction =>
            {
                await asyncWork(transaction).ConfigureAwait(false);
                return true;
            }, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Executes a read-write transaction, with retries as necessary.
        /// The work to perform in each transaction attempt is defined by <paramref name="work"/>.
        /// </summary>
        /// <remarks><paramref name="work"/> will be fully retried whenever the <see cref="SpannerTransaction"/>
        /// that it receives as a parameter aborts. <paramref name="work"/> won't be retried if any other errors occur.
        /// <paramref name="work"/> must be prepared to be called more than once.
        /// A new <see cref="SpannerTransaction"/> will be passed to <paramref name="work"/>
        /// each time it is rerun.
        /// <paramref name="work"/> doesn't need to handle the lifecycle of the <see cref="SpannerTransaction"/>,
        /// it will be automatically committed after <paramref name="work"/> has finished or rollbacked if an 
        /// <see cref="Exception"/> (other than because the transaction aborted) is thrown by <paramref name="work"/>.</remarks>
        /// <param name="work">The work to perform in each transaction attempt.</param>
        /// <returns>The value returned by <paramref name="work"/> if the transaction commits successfully.</returns>
        public TResult RunWithRetriableTransaction<TResult>(Func<SpannerTransaction, TResult> work)
        {
            GaxPreconditions.CheckNotNull(work, nameof(work));
            return Task.Run(() => RunWithRetriableTransactionAsync(
                transaction => Task.FromResult(work(transaction)),
                CancellationToken.None)).ResultWithUnwrappedExceptions();
        }

        /// <summary>
        /// Executes a read-write transaction, with retries as necessary.
        /// The work to perform in each transaction attempt is defined by <paramref name="work"/>.
        /// </summary>
        /// <remarks><paramref name="work"/> will be fully retried whenever the <see cref="SpannerTransaction"/>
        /// that it receives as a parameter aborts. <paramref name="work"/> won't be retried if any other errors occur.
        /// <paramref name="work"/> must be prepared to be called more than once.
        /// A new <see cref="SpannerTransaction"/> will be passed to <paramref name="work"/>
        /// each time it is rerun.
        /// <paramref name="work"/> doesn't need to handle the lifecycle of the <see cref="SpannerTransaction"/>,
        /// it will be automatically committed after <paramref name="work"/> has finished or rollbacked if an 
        /// <see cref="Exception"/> (other than because the transaction aborted) is thrown by <paramref name="work"/>.</remarks>
        /// <param name="work">The work to perform in each transaction attempt.</param>
        public void RunWithRetriableTransaction(Action<SpannerTransaction> work)
        {
            GaxPreconditions.CheckNotNull(work, nameof(work));
            Task.Run(() => RunWithRetriableTransactionAsync(transaction =>
            {
                work(transaction);
                return Task.FromResult(true);
            }, CancellationToken.None)).WaitWithUnwrappedExceptions();
        }

        /// <inheritdoc />
        public override void ChangeDatabase(string newDataSource)
        {
            if (IsOpen)
            {
                Close();
            }

            TrySetNewConnectionInfo(Builder.CloneWithNewDataSource(newDataSource));
        }

        /// <inheritdoc />
        public override void Close()
        {
            SessionPool sessionPool;

            ConnectionState oldState;
            lock (_sync)
            {
                if (IsClosed)
                {
                    return;
                }

                oldState = _state;
                sessionPool = _sessionPool;

                _sessionPool = null;
                _state = ConnectionState.Closed;
            }

            if (sessionPool != null)
            {
                // Note: if we're in an implicit transaction using TransactionScope, this will "release" the session pool
                // back to the session pool manager before we're really done with it, but that's okay - it will just report
                // inaccurate connection counts temporarily. This is an inherent problem with implicit transactions.
                Builder.SessionPoolManager.Release(sessionPool);
            }

            if (oldState != _state)
            {
                OnStateChange(new StateChangeEventArgs(oldState, _state));
            }
        }

        /// <summary>
        /// Creates a new <see cref="SpannerCommand" /> to delete rows from a Spanner database table.
        /// This method is thread safe.
        /// </summary>
        /// <param name="databaseTable">The name of the table from which to delete rows. Must not be null.</param>
        /// <param name="primaryKeys">The set of columns that form the primary key of the table.</param>
        /// <returns>A configured <see cref="SpannerCommand" /></returns>
        public SpannerCommand CreateDeleteCommand(
            string databaseTable,
            SpannerParameterCollection primaryKeys = null) => new SpannerCommand(
            SpannerCommandTextBuilder.CreateDeleteTextBuilder(databaseTable), this, null,
            primaryKeys);

        /// <summary>
        /// Creates a new <see cref="SpannerCommand" /> to insert rows into a Spanner database table.
        /// This method is thread safe.
        /// </summary>
        /// <param name="databaseTable">The name of the table to insert rows into. Must not be null.</param>
        /// <param name="insertedColumns">
        /// A collection of <see cref="SpannerParameter" />
        /// where each instance represents a column in the Spanner database table being set.
        /// May be null.
        /// </param>
        /// <returns>A configured <see cref="SpannerCommand" /></returns>
        public SpannerCommand CreateInsertCommand(
            string databaseTable,
            SpannerParameterCollection insertedColumns = null) => new SpannerCommand(
            SpannerCommandTextBuilder.CreateInsertTextBuilder(databaseTable), this, null,
            insertedColumns);

        /// <summary>
        /// Creates a new <see cref="SpannerCommand" /> to insert or update rows into a Spanner database table.
        /// This method is thread safe.
        /// </summary>
        /// <param name="databaseTable">The name of the table to insert or updates rows. Must not be null.</param>
        /// <param name="insertUpdateColumns">
        /// A collection of <see cref="SpannerParameter" />
        /// where each instance represents a column in the Spanner database table being set.
        /// May be null
        /// </param>
        /// <returns>A configured <see cref="SpannerCommand" /></returns>
        public SpannerCommand CreateInsertOrUpdateCommand(
            string databaseTable,
            SpannerParameterCollection insertUpdateColumns = null) => new SpannerCommand(
            SpannerCommandTextBuilder.CreateInsertOrUpdateTextBuilder(databaseTable), this,
            null, insertUpdateColumns);

        /// <summary>
        /// Creates a new <see cref="SpannerCommand" /> to select rows using a SQL query statement.
        /// This method is thread safe.
        /// </summary>
        /// <param name="sqlQueryStatement">
        /// A full SQL query statement that may optionally have
        /// replacement parameters. Must not be null.
        /// </param>
        /// <param name="selectParameters">
        /// Optionally supplied set of <see cref="SpannerParameter" />
        /// that correspond to the parameters used in the SQL query. May be null.
        /// </param>
        /// <returns>A configured <see cref="SpannerCommand" /></returns>
        public SpannerCommand CreateSelectCommand(string sqlQueryStatement, SpannerParameterCollection selectParameters = null) =>
            new SpannerCommand(SpannerCommandTextBuilder.CreateSelectTextBuilder(sqlQueryStatement), this, null, selectParameters);

        /// <summary>
        /// Creates a new <see cref="SpannerCommand" /> from a <see cref="CommandPartition"/>.
        /// The newly created command will execute on a subset of data defined by the <see cref="CommandPartition.PartitionId"/>
        /// </summary>
        /// <param name="partition">
        /// Information that represents a command to execute against a subset of data.
        /// </param>
        /// <param name="transaction">The <see cref="SpannerTransaction"/> used when
        /// creating the <see cref="CommandPartition"/>.  See <see cref="SpannerConnection.BeginReadOnlyTransaction(TransactionId)"/>.</param>
        /// <returns>A configured <see cref="SpannerCommand" /></returns>
        public SpannerCommand CreateCommandWithPartition(CommandPartition partition, SpannerTransaction transaction) =>
            new SpannerCommand(this, transaction, partition);

        /// <summary>
        /// Creates a new <see cref="SpannerCommand" /> to update rows in a Spanner database table.
        /// This method is thread safe.
        /// </summary>
        /// <param name="databaseTable">The name of the table to update rows. Must not be null.</param>
        /// <param name="updateColumns">
        /// A collection of <see cref="SpannerParameter" />
        /// where each instance represents a column in the Spanner database table being set.
        /// Primary keys of the rows to be updated must also be included.
        /// May be null.
        /// </param>
        /// <returns>A configured <see cref="SpannerCommand" /></returns>
        public SpannerCommand CreateUpdateCommand(string databaseTable, SpannerParameterCollection updateColumns = null) =>
            new SpannerCommand(SpannerCommandTextBuilder.CreateUpdateTextBuilder(databaseTable), this, null, updateColumns);

        /// <summary>
        /// Creates a new <see cref="SpannerCommand" /> to execute a DDL (CREATE/DROP TABLE, etc) statement.
        /// This method is thread safe.
        /// </summary>
        /// <param name="ddlStatement">The DDL statement (eg 'CREATE TABLE MYTABLE ...').  Must not be null.</param>
        /// <param name="extraDdlStatements">An optional set of additional DDL statements to execute after
        /// the first statement.  Extra Ddl statements cannot be used to create additional databases.</param>
        /// <returns>A configured <see cref="SpannerCommand" /></returns>
        public SpannerCommand CreateDdlCommand(
            string ddlStatement, params string[] extraDdlStatements) =>
            new SpannerCommand(SpannerCommandTextBuilder.CreateDdlTextBuilder(ddlStatement, extraDdlStatements), this);

        /// <summary>
        /// Creates a new <see cref="SpannerCommand" /> to execute a general DML (UPDATE, INSERT, DELETE) statement.
        /// This method is thread safe.
        /// </summary>
        /// <remarks>
        /// To insert, update, delete or "insert or update" a single row, the operation-specific methods
        /// (<see cref="CreateUpdateCommand(string, SpannerParameterCollection)"/> etc) are preferred as they are more efficient.
        /// This method is more appropriate for general-purpose DML which can perform modifications based on query results.
        /// </remarks>
        /// <param name="dmlStatement">The DML statement (eg 'DELETE FROM MYTABLE WHERE ...').  Must not be null.</param>
        /// <param name="dmlParameters">
        /// Optionally supplied set of <see cref="SpannerParameter" />
        /// that correspond to the parameters used in the SQL query. May be null.
        /// </param>
        /// <returns>A configured <see cref="SpannerCommand" /></returns>
        public SpannerCommand CreateDmlCommand(string dmlStatement, SpannerParameterCollection dmlParameters = null) =>
            new SpannerCommand(SpannerCommandTextBuilder.CreateDmlTextBuilder(dmlStatement), this, null, dmlParameters);

        /// <summary>
        /// Creates a new <see cref="SpannerBatchCommand"/> to execute batched DML statements with this connection, without using a transaction.
        /// You can add commands to the batch by using <see cref="SpannerBatchCommand.Add(SpannerCommand)"/>,
        /// <see cref="SpannerBatchCommand.Add(SpannerCommandTextBuilder, SpannerParameterCollection)"/>
        /// and <see cref="SpannerBatchCommand.Add(string, SpannerParameterCollection)"/>.
        /// </summary>
        public SpannerBatchCommand CreateBatchDmlCommand() => new SpannerBatchCommand(this);

        /// <inheritdoc />
        public override void Open()
        {
            if (IsOpen)
            {
                return;
            }
            Open(GetTransactionEnlister());
        }

        private void Open(Action transactionEnlister)
        {
            Func<Task> taskRunner = () => OpenAsyncImpl(transactionEnlister, CancellationToken.None);

            // This is slightly annoying, but hard to get round: most of our timeouts use Expiration, but this is more of
            // a BCL-oriented timeout.
            int timeoutSeconds = Builder.Timeout;
            TimeSpan timeout = Builder.AllowImmediateTimeouts && timeoutSeconds == 0
                ? TimeSpan.FromMilliseconds(-1)
                : TimeSpan.FromSeconds(timeoutSeconds);
            if (!Task.Run(taskRunner).WaitWithUnwrappedExceptions(timeout))
            {
                throw new SpannerException(ErrorCode.DeadlineExceeded, "Timed out opening connection");
            }
        }

        /// <inheritdoc />
        public override Task OpenAsync(CancellationToken cancellationToken) => OpenAsyncImpl(GetTransactionEnlister(), cancellationToken);

        /// <summary>
        /// Returns a task indicating when the session pool associated with the connection is populated up to its minimum size.
        /// </summary>
        /// <remarks>
        /// If the pool is unhealthy or becomes unhealthy before it reaches its minimum size,
        /// the returned task will be faulted with an <see cref="RpcException"/>.
        /// </remarks>
        /// <param name="cancellationToken">An optional token for canceling the call.</param>
        /// <returns>A task which will complete when the session pool has reached its minimum size.</returns>
        public async Task WhenSessionPoolReady(CancellationToken cancellationToken = default)
        {
            DatabaseName databaseName = Builder.DatabaseName;
            GaxPreconditions.CheckState(databaseName != null, $"{nameof(WhenSessionPoolReady)} cannot be used without a database.");
            await OpenAsync(cancellationToken).ConfigureAwait(false);
            await _sessionPool.WhenPoolReady(databaseName, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Shuts down the session pool associated with the connection. Further attempts to acquire sessions will fail immediately.
        /// </summary>
        /// <remarks>
        /// This call will delete all pooled sessions, and wait for all active sessions to be released back to the pool
        /// and also deleted.
        /// </remarks>
        /// <param name="cancellationToken">An optional token for canceling the returned task. This does not cancel the shutdown itself.</param>
        /// <returns>A task which will complete when the session pool has finished shutting down.</returns>
        public async Task ShutdownSessionPoolAsync(CancellationToken cancellationToken = default)
        {
            DatabaseName databaseName = Builder.DatabaseName;
            GaxPreconditions.CheckState(databaseName != null, $"{nameof(ShutdownSessionPoolAsync)} cannot be used without a database.");
            await OpenAsync(cancellationToken).ConfigureAwait(false);
            await _sessionPool.ShutdownPoolAsync(databaseName, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Retrieves the database-specific statistics for the session pool associated with the connection string. The connection string must
        /// include a database name.
        /// </summary>
        /// <returns>The session pool statistics, or <c>null</c> if there is no current session pool
        /// for the database specified in the connection string.</returns>
        public SessionPool.DatabaseStatistics GetSessionPoolDatabaseStatistics()
        {
            DatabaseName databaseName = Builder.DatabaseName;
            GaxPreconditions.CheckState(databaseName != null, $"{nameof(GetSessionPoolDatabaseStatistics)} cannot be used without a database.");
            return Builder.SessionPoolManager.GetDatabaseStatistics(new SpannerClientCreationOptions(Builder), databaseName);
        }

        /// <summary>
        /// Opens the connection, which involves acquiring a SessionPool,
        /// and potentially enlists the connection in the current transaction.
        /// </summary>
        /// <param name="transactionEnlister">Enlistment delegate; may be null.</param>
        /// <param name="cancellationToken">Cancellation token; may be None</param>
        private Task OpenAsyncImpl(Action transactionEnlister, CancellationToken cancellationToken)
        {
            // TODO: Use the cancellation token. We can't at the moment, as the only reason for this being async is
            // due to credential fetching, and we can't pass a cancellation token to any of that.
            return ExecuteHelper.WithErrorTranslationAndProfiling(
                async () =>
                {
                    ConnectionState previousState;
                    lock (_sync)
                    {
                        previousState = _state;
                        if (IsOpen)
                        {
                            return;
                        }

                        if (previousState == ConnectionState.Connecting)
                        {
                            throw new InvalidOperationException("The SpannerConnection is already being opened.");
                        }

                        _state = ConnectionState.Connecting;
                    }
                    OnStateChange(new StateChangeEventArgs(previousState, ConnectionState.Connecting));
                    try
                    {
                        _sessionPool = await Builder.AcquireSessionPoolAsync().ConfigureAwait(false);
                    }
                    finally
                    {
                        // Note: the code could be simplified if we don't mind the ordering of "change state, enlist, fire OnStateChange" -
                        // but it's not clear whether or not that's a problem.
                        lock (_sync)
                        {
                            _state = _sessionPool != null ? ConnectionState.Open : ConnectionState.Broken;
                        }
                        if (IsOpen)
                        {
                            transactionEnlister?.Invoke();
                        }
                        OnStateChange(new StateChangeEventArgs(ConnectionState.Connecting, _state));
                    }
                }, "SpannerConnection.OpenAsync", Logger);
        }

        /// <inheritdoc />
        protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel)
        {
            if (isolationLevel != IsolationLevel.Unspecified
                && isolationLevel != IsolationLevel.Serializable)
            {
                throw new NotSupportedException(
                    $"Cloud Spanner only supports isolation levels {IsolationLevel.Serializable} and {IsolationLevel.Unspecified}.");
            }
            return Task.Run(() => BeginTransactionAsync()).ResultWithUnwrappedExceptions();
        }

        /// <inheritdoc />
        protected override DbCommand CreateDbCommand() => new SpannerCommand(this);

        /// <inheritdoc />
        protected override void Dispose(bool disposing)
        {
            if (IsOpen)
            {
                Close();
            }
        }

        /// <summary>
        /// Returns the current ambient transaction (from TransactionScope), if any.
        /// The .NET Standard 1.x version will always return null, as TransactionScope is not supported in .NET Core 1.x.
        /// </summary>
        internal ISpannerTransaction AmbientTransaction => _volatileResourceManager;

        /// <summary>
        /// The current connection string builder. The object is never mutated and never exposed to consumers.
        /// The value may be changed to a new builder by setting the <see cref="ConnectionString"/>
        /// property, or within this class via the <see cref="TrySetNewConnectionInfo(SpannerConnectionStringBuilder)"/> method.
        /// This value is never null.
        /// </summary>
        internal SpannerConnectionStringBuilder Builder { get; private set; }

        private void AssertClosed(string message)
        {
            if (!IsClosed)
            {
                throw new InvalidOperationException("The connection must be closed. Failed to " + message);
            }
        }

        private void AssertOpen(string message)
        {
            if (!IsOpen)
            {
                throw new InvalidOperationException("The connection must be open. Failed to " + message);
            }
        }

        internal async Task EnsureIsOpenAsync(CancellationToken cancellationToken)
        {
            if (!IsOpen)
            {
                await OpenAsync(cancellationToken).ConfigureAwait(false);
            }

            if (!IsOpen)
            {
                throw new InvalidOperationException("Unable to open the Spanner connection to the database.");
            }
        }

        internal CallSettings CreateCallSettings(Func<SpannerSettings, CallSettings> settingsProvider, CancellationToken cancellationToken) =>
            settingsProvider(Builder.SessionPoolManager.SpannerSettings).WithCancellationToken(cancellationToken);

        internal CallSettings CreateCallSettings(Func<SpannerSettings, CallSettings> settingsProvider, int timeoutSeconds, CancellationToken cancellationToken)
        {
            var originalSettings = settingsProvider(Builder.SessionPoolManager.SpannerSettings);
            var expiration = timeoutSeconds == 0 && !Builder.AllowImmediateTimeouts ? Expiration.None : Expiration.FromTimeout(TimeSpan.FromSeconds(timeoutSeconds));
            return originalSettings.WithExpiration(expiration).WithCancellationToken(cancellationToken);
        }

        internal async Task<PooledSession> AcquireReadWriteSessionAsync(CancellationToken cancellationToken) =>
            await AcquireSessionAsync(s_readWriteTransactionOptions, cancellationToken).ConfigureAwait(false);

        internal Task<PooledSession> AcquireSessionAsync(TransactionOptions options, CancellationToken cancellationToken)
        {
            SessionPool pool;
            DatabaseName databaseName;
            lock (_sync)
            {
                AssertOpen("acquire session.");
                pool = _sessionPool;
                databaseName = Builder.DatabaseName;
            }
            if (databaseName is null)
            {
                throw new InvalidOperationException("Unable to acquire session on connection with no database name");
            }
            return pool.AcquireSessionAsync(databaseName, options, cancellationToken);
        }

        internal Task<SpannerTransaction> BeginTransactionImplAsync(
            TransactionOptions transactionOptions,
            TransactionMode transactionMode,
            CancellationToken cancellationToken,
            TimestampBound targetReadTimestamp = null)
        {
            return ExecuteHelper.WithErrorTranslationAndProfiling(
                async () =>
                {
                    await OpenAsync(cancellationToken).ConfigureAwait(false);
                    var session = await AcquireSessionAsync(transactionOptions, cancellationToken).ConfigureAwait(false);
                    return new SpannerTransaction(this, transactionMode, session, targetReadTimestamp);
                }, "SpannerConnection.BeginTransaction", Logger);
        }

        private void TrySetNewConnectionInfo(SpannerConnectionStringBuilder newBuilder)
        {
            AssertClosed("change connection information.");
            // We will never allow our internal SpannerConnectionStringBuilder to be touched from the outside, so it's cloned.
            Builder = newBuilder.Clone();
        }

        /// <summary>
        /// Returns a delegate to enlist the current transaction (as detected on the executing thread *now*)
        /// when opening the connection.
        /// </summary>
        private Action GetTransactionEnlister()
        {
            Transaction current = Transaction.Current;
            return current == null ? (Action) null : () => EnlistTransaction(current);
        }

        /// <summary>
        /// Call OpenAsReadOnly within a <see cref="System.Transactions.TransactionScope" /> to open the connection
        /// with a read-only transaction with the given <see cref="TimestampBound" /> settings
        /// </summary>
        /// <param name="timestampBound">Specifies the timestamp or maximum staleness of a read operation. May be null.</param>
        public void OpenAsReadOnly(TimestampBound timestampBound = null)
        {
            // Note: This has to be checked on the current thread, which is why we don't just use Task.Run
            // and delegate to OpenAsReadOnlyAsync
            var transaction = Transaction.Current;
            if (transaction == null)
            {
                throw new InvalidOperationException($"{nameof(OpenAsReadOnlyAsync)} should only be called within a TransactionScope.");
            }
            if (!EnlistInTransaction)
            {
                throw new InvalidOperationException($"{nameof(OpenAsReadOnlyAsync)} should only be called with ${nameof(EnlistInTransaction)} set to true.");
            }
            Open(() => EnlistTransaction(transaction, timestampBound ?? TimestampBound.Strong, null));
        }

        /// <summary>
        /// If this connection is being opened within a <see cref="System.Transactions.TransactionScope" />, this
        /// will connect to an existing transaction identified by <paramref name="transactionId"/>.
        /// </summary>
        /// <param name="transactionId">The <see cref="TransactionId"/> representing an active readonly <see cref="SpannerTransaction"/>.</param>
        public void OpenAsReadOnly(TransactionId transactionId)
        {
            GaxPreconditions.CheckNotNull(transactionId, nameof(transactionId));
            var transaction = Transaction.Current;
            if (transaction == null)
            {
                throw new InvalidOperationException($"{nameof(OpenAsReadOnlyAsync)} should only be called within a TransactionScope.");
            }
            if (!EnlistInTransaction)
            {
                throw new InvalidOperationException($"{nameof(OpenAsReadOnlyAsync)} should only be called with ${nameof(EnlistInTransaction)} set to true.");
            }
            Open(() => EnlistTransaction(transaction, null, transactionId));
        }

        /// <summary>
        /// If this connection is being opened within a <see cref="System.Transactions.TransactionScope" />, this forces
        /// the created Cloud Spanner transaction to be a read-only transaction with the given
        /// <see cref="TimestampBound" /> settings.
        /// </summary>
        /// <param name="timestampBound">Specifies the timestamp or maximum staleness of a read operation. May be null.</param>
        /// <param name="cancellationToken">An optional token for canceling the call.</param>
        public Task OpenAsReadOnlyAsync(TimestampBound timestampBound = null, CancellationToken cancellationToken = default)
        {
            var transaction = Transaction.Current;
            if (transaction == null)
            {
                throw new InvalidOperationException($"{nameof(OpenAsReadOnlyAsync)} should only be called within a TransactionScope.");
            }
            if (!EnlistInTransaction)
            {
                throw new InvalidOperationException($"{nameof(OpenAsReadOnlyAsync)} should only be called with ${nameof(EnlistInTransaction)} set to true.");
            }
            Action transactionEnlister = () => EnlistTransaction(transaction, timestampBound ?? TimestampBound.Strong, null);
            return OpenAsyncImpl(transactionEnlister, cancellationToken);
        }

        /// <summary>
        /// Gets or Sets whether to participate in the active <see cref="System.Transactions.TransactionScope" />
        /// </summary>
        public bool EnlistInTransaction { get; set; } = true;

        /// <inheritdoc />
        public override void EnlistTransaction(Transaction transaction) => EnlistTransaction(transaction, null, null);

        private void EnlistTransaction(Transaction transaction, TimestampBound timestampBound, TransactionId transactionId)
        {
            if (!EnlistInTransaction)
            {
                return;
            }
            if (_volatileResourceManager != null)
            {
                throw new InvalidOperationException("This connection is already enlisted to a transaction.");
            }
            _volatileResourceManager = new VolatileResourceManager(this, timestampBound, transactionId);
            transaction.EnlistVolatile(_volatileResourceManager, System.Transactions.EnlistmentOptions.None);
        }

        /// <inheritdoc />
        protected override DbProviderFactory DbProviderFactory => SpannerProviderFactory.Instance;

    }
}
