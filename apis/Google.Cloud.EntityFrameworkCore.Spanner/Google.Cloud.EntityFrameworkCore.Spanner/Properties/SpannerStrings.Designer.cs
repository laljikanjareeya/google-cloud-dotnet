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
using System.Reflection;
using System.Resources;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Microsoft.EntityFrameworkCore.Internal
{
    /// <summary>
    ///		This API supports the Entity Framework Core infrastructure and is not intended to be used
    ///     directly from your code. This API may change or be removed in future releases.
    /// </summary>
    public static class SpannerStrings
    {
        private static readonly ResourceManager _resourceManager
            = new ResourceManager("Google.Cloud.EntityFrameworkCore.Spanner.Properties.SpannerStrings", typeof(SpannerStrings).GetTypeInfo().Assembly);

        /// <summary>
        ///     Google Cloud Spanner does not support this migration operation '{operation}'.
        /// </summary>
        public static string InvalidMigrationOperation(object operation)
            => string.Format(
                GetString("InvalidMigrationOperation", nameof(operation)),
                operation);

        /// <summary>
        ///     Generating idempotent scripts for migration is not currently supported by Google Cloud Spanner.
        /// </summary>
        public static string MigrationScriptGenerationNotSupported
            => GetString("MigrationScriptGenerationNotSupported");

        private static string GetString(string name, params string[] formatterNames)
        {
            var value = _resourceManager.GetString(name);
            for (var i = 0; i < formatterNames.Length; i++)
            {
                value = value.Replace("{" + formatterNames[i] + "}", "{" + i + "}");
            }

            return value;
        }
    }
}
