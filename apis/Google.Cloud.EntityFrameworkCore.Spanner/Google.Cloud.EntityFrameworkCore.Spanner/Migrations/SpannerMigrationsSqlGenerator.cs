// Copyright 2017 Google Inc. All Rights Reserved.
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

using System;
using System.Collections.Generic;
using Google.Api.Gax;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations.Operations;

namespace Microsoft.EntityFrameworkCore.Migrations
{
    /// <summary>
    /// Customizes the default migration sql generator to create Spanner compatible Ddl.
    /// </summary>
    internal class SpannerMigrationsSqlGenerator : MigrationsSqlGenerator
    {
        //When creating tables, we always need to specify "MAX"
        //However, its important that the simple typename remain "STRING" because throughout query operations,
        //the typename is used for CAST and other operations where "MAX" would result in an error.
        private static readonly Dictionary<string, string> s_columnTypeMap = new Dictionary<string, string>
        {
            {"STRING", "STRING(MAX)"},
            {"BYTES", "BYTES(MAX)"}
        };

        /// <summary>
        ///     This API supports the Entity Framework Core infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        /// <param name="dependencies"> Parameter object containing dependencies for this service. </param>
        public SpannerMigrationsSqlGenerator(MigrationsSqlGeneratorDependencies dependencies)
            : base(dependencies)
        {
        }

        /// <summary>
        ///     <para>
        ///         Builds commands for the given <see cref="MigrationOperation" /> by making calls on the given
        ///         <see cref="MigrationCommandListBuilder" />.
        ///     </para>
        ///     <para>
        ///         This method uses a double-dispatch mechanism to call one of the 'Generate' methods that are
        ///         specific to a certain subtype of <see cref="MigrationOperation" />. Typically database providers
        ///         will override these specific methods rather than this method. However, providers can override
        ///         this methods to handle provider-specific operations.
        ///     </para>
        /// </summary>
        /// <param name="operation"> The operation. </param>
        /// <param name="model"> The target model which may be <c>null</c> if the operations exist without a model. </param>
        /// <param name="builder"> The command builder to use to build the commands. </param>
        protected override void Generate(MigrationOperation operation, IModel model,
            MigrationCommandListBuilder builder)
        {
            if (operation is SpannerCreateDatabaseOperation createDatabaseOperation)
            {
                GenerateCreateDatabase(createDatabaseOperation.Name, model, builder);
            }
            else if (operation is SpannerDropDatabaseOperation dropDatabaseOperation)
            {
                GenerateDropDatabase(dropDatabaseOperation.Name, model, builder);
            }
            else
            {
                base.Generate(operation, model, builder);
            }
        }


        /// <summary>
        ///     Builds commands for Drop The Database
        ///     by making calls on the given <see cref="MigrationCommandListBuilder" />.
        /// </summary>
        /// <param name="name"> The operation name. </param>
        /// <param name="model"> The target model which may be <c>null</c> if the operations exist without a model. </param>
        /// <param name="builder"> The command builder to use to build the commands. </param>
        private void GenerateDropDatabase(string name, IModel model,
            MigrationCommandListBuilder builder)
        {
            builder
                .Append("DROP DATABASE ")
                .Append(Dependencies.SqlGenerationHelper.DelimitIdentifier(name));

            builder.AppendLine(Dependencies.SqlGenerationHelper.StatementTerminator);
            EndStatement(builder, true);
        }

        /// <summary>
        ///     Builds commands for Create Database
        ///     by making calls on the given <see cref="MigrationCommandListBuilder" />.
        /// </summary>>
        /// <param name="name">The operation name.</param>
        /// <param name="model"> The target model which may be <c>null</c> if the operations exist without a model. </param>
        /// <param name="builder"> The command builder to use to build the commands. </param>
        private void GenerateCreateDatabase(string name, IModel model,
            MigrationCommandListBuilder builder)
        {
            builder
                .Append("CREATE DATABASE ")
                .Append(Dependencies.SqlGenerationHelper.DelimitIdentifier(name));

            builder.AppendLine(Dependencies.SqlGenerationHelper.StatementTerminator);
            EndStatement(builder, true);
        }

        /// <summary>
        ///     Builds commands for the given <see cref="CreateTableOperation" /> by making calls on the given
        ///     <see cref="MigrationCommandListBuilder" />
        /// </summary>
        /// <param name="operation"> The operation. </param>
        /// <param name="model"> The target model which may be <c>null</c> if the operations exist without a model. </param>
        /// <param name="builder"> The command builder to use to build the commands. </param>
        /// <param name="terminate"></param>
        protected override void Generate(
            CreateTableOperation operation,
            IModel model,
            MigrationCommandListBuilder builder,
            bool terminate)
        {
            builder
                .Append("CREATE TABLE ")
                .Append(Dependencies.SqlGenerationHelper.DelimitIdentifier(operation.Name, operation.Schema))
                .AppendLine(" (");

            using (builder.Indent())
            {
                for (var i = 0; i < operation.Columns.Count; i++)
                {
                    var column = operation.Columns[i];
                    ColumnDefinition(column, model, builder);

                    if (i != operation.Columns.Count - 1)
                    {
                        builder.AppendLine(",");
                    }
                }
                builder.AppendLine();
            }

            builder.Append(")");
            if (operation.PrimaryKey != null)
            {
                PrimaryKeyConstraint(operation.PrimaryKey, model, builder);
            }

            if (terminate)
            {
                builder.AppendLine(Dependencies.SqlGenerationHelper.StatementTerminator);
                EndStatement(builder, true);
            }
        }

        /// <summary>
        ///     Throws <see cref="NotSupportedException" /> since this operation requires table rebuilds, which
        ///     are not yet supported.
        /// </summary>
        /// <param name="operation"> The operation. </param>
        /// <param name="model"> The target model which may be <c>null</c> if the operations exist without a model. </param>
        /// <param name="builder"> The command builder to use to build the commands. </param>
        protected override void Generate(RenameTableOperation operation, IModel model,
           MigrationCommandListBuilder builder)
        {
            throw new NotSupportedException("Google Cloud Spanner does not support this migration operation 'RenameTableOperation'.");
        }

        /// <summary>
        ///     Builds commands for the given <see cref="CreateTableOperation" /> by making calls on the given
        ///     <see cref="MigrationCommandListBuilder" />, and then terminates the final command.
        /// </summary>
        /// <param name="operation"> The operation. </param>
        /// <param name="model"> The target model which may be <c>null</c> if the operations exist without a model. </param>
        /// <param name="builder"> The command builder to use to build the commands. </param>
        protected override void Generate(CreateTableOperation operation, IModel model,
           MigrationCommandListBuilder builder)
           => Generate(operation, model, builder, terminate: true);

        /// <summary>
        /// Foreign keys are not supported by spanner and are ignored.
        /// </summary>
        /// <param name="operation"> The operation. </param>
        /// <param name="model"> The target model which may be <c>null</c> if the operations exist without a model. </param>
        /// <param name="builder"> The command builder to use to build the commands. </param>
        protected override void Generate(DropForeignKeyOperation operation, IModel model,
            MigrationCommandListBuilder builder)
        {
            // Foreign keys are not supported by spanner and are ignored.
        }

        /// <summary>
        /// Foreign keys are not supported by spanner and are ignored.
        /// </summary>
        /// <param name="operation"> The operation. </param>
        /// <param name="model"> The target model which may be <c>null</c> if the operations exist without a model. </param>
        /// <param name="builder"> The command builder to use to build the commands. </param>
        protected override void Generate(AddForeignKeyOperation operation, IModel model,
           MigrationCommandListBuilder builder)
        {
            // Foreign keys are not supported by spanner and are ignored.
        }

        /// <summary>
        /// Foreign keys are not supported by spanner and are ignored.
        /// </summary>
        /// <param name="operation"> The operation. </param>
        /// <param name="model"> The target model which may be <c>null</c> if the operations exist without a model. </param>
        /// <param name="builder"> The command builder to use to build the commands. </param>
        /// <param name="terminate"></param>
        protected override void Generate(AddForeignKeyOperation operation, IModel model,
            MigrationCommandListBuilder builder, bool terminate)
        {
            // Foreign keys are not supported by spanner and are ignored.
        }

        /// <summary>
        ///     Builds commands for the given <see cref="AddPrimaryKeyOperation" /> by making calls on the given
        ///     <see cref="MigrationCommandListBuilder" />
        /// </summary>
        /// <param name="operation"> The operation. </param>
        /// <param name="model"> The target model which may be <c>null</c> if the operations exist without a model. </param>
        /// <param name="builder"> The command builder to use to build the commands. </param>
        protected override void PrimaryKeyConstraint(
            AddPrimaryKeyOperation operation,
            IModel model,
            MigrationCommandListBuilder builder)
        {
            builder.Append("PRIMARY KEY ");
            IndexTraits(operation, model, builder);
            builder.Append("(")
                .Append(ColumnList(operation.Columns))
                .Append(")");
        }

        /// <summary>
        /// Foreign keys are not supported by spanner and are ignored.
        /// </summary>
        /// <param name="operation"> The operation. </param>
        /// <param name="model"> The target model which may be <c>null</c> if the operations exist without a model. </param>
        /// <param name="builder"> The command builder to use to build the commands. </param>
        /// <param name="terminate"></param>
        protected override void Generate(DropForeignKeyOperation operation, IModel model,
            MigrationCommandListBuilder builder, bool terminate)
        {
            // Foreign keys are not supported by spanner and are ignored.
        }


        /// <summary>
        ///     Builds commands for the given <see cref="DropIndexOperation" />
        ///     by making calls on the given <see cref="MigrationCommandListBuilder" />, and then terminates the final command.
        /// </summary>
        /// <param name="operation"> The operation. </param>
        /// <param name="model"> The target model which may be <c>null</c> if the operations exist without a model. </param>
        /// <param name="builder"> The command builder to use to build the commands. </param>
        protected override void Generate(DropIndexOperation operation, IModel model,
            MigrationCommandListBuilder builder)
        {
            Generate(operation, model, builder, true);
        }

        /// <summary>
        ///     Builds commands for the given <see cref="DropIndexOperation" />
        ///     by making calls on the given <see cref="MigrationCommandListBuilder" />.
        /// </summary>
        /// <param name="operation"> The operation. </param>
        /// <param name="model"> The target model which may be <c>null</c> if the operations exist without a model. </param>
        /// <param name="builder"> The command builder to use to build the commands. </param>
        /// <param name="terminate"> Indicates whether or not to terminate the command after generating SQL for the operation. </param>
        protected virtual void Generate(DropIndexOperation operation, IModel model,
            MigrationCommandListBuilder builder, bool terminate)
        {
            builder.Append(" DROP INDEX ")
                     .Append(Dependencies.SqlGenerationHelper.DelimitIdentifier(operation.Name));

            if (terminate)
            {
                builder
                    .AppendLine(Dependencies.SqlGenerationHelper.StatementTerminator)
                    .EndCommand();
            }
        }


        /// <summary>
        ///     Throws <see cref="NotSupportedException" /> since this operation requires table rebuilds, which
        ///     are not yet supported.
        /// </summary>
        /// <param name="operation"> The operation. </param>
        /// <param name="model"> The target model which may be <c>null</c> if the operations exist without a model. </param>
        /// <param name="builder"> The command builder to use to build the commands. </param>
        protected override void Generate(
           RenameColumnOperation operation,
           IModel model,
           MigrationCommandListBuilder builder)
        {
            throw new NotSupportedException($"Google Cloud Spanner does not support this migration operation 'RenameColumnOperation'.");
        }

        private static string GetCorrectedColumnType(string columnType)
        {
            columnType = columnType.ToUpperInvariant();
            return s_columnTypeMap.TryGetValue(columnType, out string convertedColumnType)
                ? convertedColumnType
                : columnType;
        }

        protected override void ColumnDefinition(
            string schema,
            string table,
            string name,
            System.Type clrType,
            string type,
            bool? unicode,
            int? maxLength,
            bool rowVersion,
            bool nullable,
            object defaultValue,
            string defaultValueSql,
            string computedColumnSql,
            IAnnotatable annotatable,
            IModel model,
            MigrationCommandListBuilder builder)
        {
            GaxPreconditions.CheckNotNullOrEmpty(name, nameof(name));
            GaxPreconditions.CheckNotNull(clrType, nameof(clrType));
            GaxPreconditions.CheckNotNull(annotatable, nameof(annotatable));
            GaxPreconditions.CheckNotNull(builder, nameof(builder));

            builder
                .Append(Dependencies.SqlGenerationHelper.DelimitIdentifier(name))
                .Append(" ")
                .Append(GetCorrectedColumnType(
                    type ?? GetColumnType(schema, table, name, clrType, unicode, maxLength, rowVersion, model)));

            if (!nullable)
            {
                builder.Append(" NOT NULL");
            }
        }
    }
}