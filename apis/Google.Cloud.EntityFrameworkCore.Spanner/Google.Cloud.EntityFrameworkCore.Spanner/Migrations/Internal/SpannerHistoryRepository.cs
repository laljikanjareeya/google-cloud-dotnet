using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.EntityFrameworkCore.Storage;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.EntityFrameworkCore.Migrations.Internal
{
    /// <summary>
    ///     This API supports the Entity Framework Core infrastructure and is not intended to be used
    ///     directly from your code. This API may change or be removed in future releases.
    /// </summary>
    public class SpannerHistoryRepository : HistoryRepository
    {
        /// <summary>
        ///     This API supports the Entity Framework Core infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public SpannerHistoryRepository(HistoryRepositoryDependencies dependencies)
            : base(dependencies)
        {
        }

        /// <summary>
        ///     This API supports the Entity Framework Core infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        protected override string ExistsSql
        {
            get
            {
                var builder = new StringBuilder();
                builder.Append("SELECT table_name from information_schema.tables AS t where t.table_name = '");

                if (TableSchema != null)
                {
                    builder
                        .Append(SqlGenerationHelper.EscapeLiteral(TableSchema))
                        .Append(".");
                }

                builder
                   .Append(SqlGenerationHelper.EscapeLiteral(TableName))
                   .Append("';");

                return builder.ToString();
            }
        }

        /// <summary>
        ///     This API supports the Entity Framework Core infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        protected override bool InterpretExistsResult(object value)
        {
            return value != DBNull.Value && value != null;
        }

        /// <summary>
        ///     This API supports the Entity Framework Core infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public override string GetCreateIfNotExistsScript()
        {
            var builder = new IndentedStringBuilder();

            builder.Append("IF OBJECT_ID(N'");

            if (TableSchema != null)
            {
                builder
                    .Append(SqlGenerationHelper.EscapeLiteral(TableSchema))
                    .Append(".");
            }

            builder
                .Append(SqlGenerationHelper.EscapeLiteral(TableName))
                .AppendLine("') IS NULL")
                .AppendLine("BEGIN");
            using (builder.Indent())
            {
                builder.AppendLines(GetCreateScript());
            }
            builder.AppendLine("END;");

            return builder.ToString();
        }

        /// <summary>
        ///     This API supports the Entity Framework Core infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public override string GetBeginIfNotExistsScript(string migrationId)
        {
            return new StringBuilder()
                .Append("IF NOT EXISTS(SELECT * FROM ")
                .Append(SqlGenerationHelper.DelimitIdentifier(TableName, TableSchema))
                .Append(" WHERE ")
                .Append(SqlGenerationHelper.DelimitIdentifier(MigrationIdColumnName))
                .Append(" = N'")
                .Append(SqlGenerationHelper.EscapeLiteral(migrationId))
                .AppendLine("')")
                .Append("BEGIN")
                .ToString();
        }

        /// <summary>
        ///     This API supports the Entity Framework Core infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public override string GetBeginIfExistsScript(string migrationId)
        {
            //Check.NotEmpty(migrationId, nameof(migrationId));

            return new StringBuilder()
                 .Append("IF EXISTS(SELECT * FROM ")
                 .Append(SqlGenerationHelper.DelimitIdentifier(TableName, TableSchema))
                 .Append(" WHERE ")
                 .Append(SqlGenerationHelper.DelimitIdentifier(MigrationIdColumnName))
                 .Append(" = N'")
                 .Append(SqlGenerationHelper.EscapeLiteral(migrationId))
                 .AppendLine("')")
                 .Append("BEGIN")
                 .ToString();
        }

        /// <summary>
        ///     This API supports the Entity Framework Core infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public override string GetEndIfScript()
            => new StringBuilder()
                .Append("END")
                .AppendLine(SqlGenerationHelper.StatementTerminator)
                .ToString();
    }
}
