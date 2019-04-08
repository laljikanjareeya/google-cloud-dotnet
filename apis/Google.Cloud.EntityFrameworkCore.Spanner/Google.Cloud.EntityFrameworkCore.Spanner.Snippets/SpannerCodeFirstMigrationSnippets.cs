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

using Google.Cloud.Spanner.Data;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

// ReSharper disable once CheckNamespace
namespace Microsoft.EntityFrameworkCore
{
    public class SpannerCodeFirstMigrationSnippets
    {
        public class StudentContext : DbContext
        {
            private readonly string _databaseName;
            private static IConfiguration Config { get; set; }
            public StudentContext(string databaseName)
            {
                _databaseName = databaseName;
            }

            public StudentContext(DbContextOptions options, string databaseName) : base(options)
            {
                _databaseName = databaseName;
            }

            public static string DefaultConnection =>
            $"Data Source=projects/{Config["TEST_PROJECT"]}/instances/spannerinstance/databases/migrationtest";

            public DbSet<Student> Students { get; set; }


            protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
            {
                optionsBuilder
                    .UseSpanner(CreateConnectionString(_databaseName), x => x.MigrationsHistoryTable("MigrationsHistory"));
            }

            private string CreateConnectionString(string name)
            {
                var builder = new SpannerConnectionStringBuilder(DefaultConnection);
                builder.DataSource = string.IsNullOrEmpty(name) ?
                    $"projects/{builder.Project}/instances/{builder.SpannerInstance}"
                    : $"projects/{builder.Project}/instances/{builder.SpannerInstance}/databases/{name}";
                return builder.ConnectionString;
            }
        }

        public class Student
        {
            public int Id { get; set; }
            public string FirstName { get; set; }
            public string LastName { get; set; }
        }

        public class StudentContextFactory : IDesignTimeDbContextFactory<StudentContext>
        {
            public StudentContext CreateDbContext(string[] args)
            {
                var optionsBuilder = new DbContextOptionsBuilder<StudentContext>();
                optionsBuilder.UseSpanner("Data Source=migrationtest");

                return new StudentContext(optionsBuilder.Options, "migrationtest");
            }
        }
    }

   
}
