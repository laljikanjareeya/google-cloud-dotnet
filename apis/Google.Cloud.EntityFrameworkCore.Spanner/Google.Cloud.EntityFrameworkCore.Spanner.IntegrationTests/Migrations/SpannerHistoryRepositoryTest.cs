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

using Google.Cloud.EntityFrameworkCore.Spanner.IntegrationTests;
using System;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Microsoft.EntityFrameworkCore.Internal;

// ReSharper disable InconsistentNaming
namespace Microsoft.EntityFrameworkCore.Migrations
{
    public class SpannerHistoryRepositoryTest
    {
        private static string EOL => Environment.NewLine;

        [Fact]
        public void GetCreateScript_works()
        {
            var sql = CreateHistoryRepository().GetCreateScript();
            Assert.Equal("CREATE TABLE __EFMigrationsHistory (" + EOL +
                "    MigrationId STRING(MAX) NOT NULL," + EOL +
                "    ProductVersion STRING(MAX) NOT NULL" + EOL +
                ")PRIMARY KEY (MigrationId)" + EOL, sql);
        }

        [Fact]
        public void GetCreateIfNotExistsScript_works()
        {
            var sql = CreateHistoryRepository().GetCreateIfNotExistsScript();
            Assert.Equal("CREATE TABLE __EFMigrationsHistory (" + EOL +
                            "    MigrationId STRING(MAX) NOT NULL," + EOL +
                            "    ProductVersion STRING(MAX) NOT NULL" + EOL +
                            ")PRIMARY KEY (MigrationId)" + EOL, sql);
        }

        [Fact]
        public void GetDeleteScript_works()
        {
            var sql = CreateHistoryRepository().GetDeleteScript("Migration1");
            Assert.Equal("DELETE FROM __EFMigrationsHistory" + EOL +
                "WHERE MigrationId = 'Migration1'" + EOL, sql);
        }

        [Fact]
        public void GetInsertScript_works()
        {
            var sql = CreateHistoryRepository().GetInsertScript(
                new HistoryRow("Migration1", "7.0.0"));
            Assert.Equal("INSERT INTO __EFMigrationsHistory (MigrationId, ProductVersion)" + EOL +
                "VALUES ('Migration1', '7.0.0')" + EOL, sql);
        }

        [Fact]
        public void GetBeginIfNotExistsScript_works()
        {
            var repository = CreateHistoryRepository();
            var ex = Assert.Throws<NotSupportedException>(() => repository.GetBeginIfNotExistsScript("Migration1"));

            Assert.Equal("Generating idempotent scripts for migration is not currently supported by Google Cloud Spanner.", ex.Message);
        }

        [Fact]
        public void GetBeginIfExistsScript_works()
        {
            var repository = CreateHistoryRepository();
            var ex = Assert.Throws<NotSupportedException>(() => repository.GetBeginIfExistsScript("Migration1"));

            Assert.Equal("Generating idempotent scripts for migration is not currently supported by Google Cloud Spanner.", ex.Message);
        }

        [Fact]
        public void GetEndIfScript_works()
        {
            var repository = CreateHistoryRepository();
            var ex = Assert.Throws<NotSupportedException>(() => repository.GetEndIfScript());

            Assert.Equal("Generating idempotent scripts for migration is not currently supported by Google Cloud Spanner.", ex.Message);
        }

        private static IHistoryRepository CreateHistoryRepository()
          => SpannerTestHelpers.Instance.CreateContextServices()
              .GetRequiredService<IHistoryRepository>();
    }
}
