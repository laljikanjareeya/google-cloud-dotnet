// Copyright 2020 Google LLC
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

// Generated code. DO NOT EDIT!

namespace Google.Cloud.BigQuery.Storage.V1.Snippets
{
    using Google.Api.Gax.ResourceNames;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    /// <summary>Generated snippets.</summary>
    public sealed class GeneratedBigQueryReadClientSnippets
    {
        /// <summary>Snippet for CreateReadSession</summary>
        public void CreateReadSession_RequestObject()
        {
            // Snippet: CreateReadSession(CreateReadSessionRequest, CallSettings)
            // Create client
            BigQueryReadClient bigQueryReadClient = BigQueryReadClient.Create();
            // Initialize request argument(s)
            CreateReadSessionRequest request = new CreateReadSessionRequest
            {
                ParentAsProjectName = new ProjectName("[PROJECT]"),
                ReadSession = new ReadSession(),
                MaxStreamCount = 0,
            };
            // Make the request
            ReadSession response = bigQueryReadClient.CreateReadSession(request);
            // End snippet
        }

        /// <summary>Snippet for CreateReadSessionAsync</summary>
        public async Task CreateReadSessionAsync_RequestObject()
        {
            // Snippet: CreateReadSessionAsync(CreateReadSessionRequest, CallSettings)
            // Additional: CreateReadSessionAsync(CreateReadSessionRequest, CancellationToken)
            // Create client
            BigQueryReadClient bigQueryReadClient = await BigQueryReadClient.CreateAsync();
            // Initialize request argument(s)
            CreateReadSessionRequest request = new CreateReadSessionRequest
            {
                ParentAsProjectName = new ProjectName("[PROJECT]"),
                ReadSession = new ReadSession(),
                MaxStreamCount = 0,
            };
            // Make the request
            ReadSession response = await bigQueryReadClient.CreateReadSessionAsync(request);
            // End snippet
        }

        /// <summary>Snippet for CreateReadSession</summary>
        public void CreateReadSession()
        {
            // Snippet: CreateReadSession(string, ReadSession, int, CallSettings)
            // Create client
            BigQueryReadClient bigQueryReadClient = BigQueryReadClient.Create();
            // Initialize request argument(s)
            string parent = "projects/[PROJECT]";
            ReadSession readSession = new ReadSession();
            int maxStreamCount = 0;
            // Make the request
            ReadSession response = bigQueryReadClient.CreateReadSession(parent, readSession, maxStreamCount);
            // End snippet
        }

        /// <summary>Snippet for CreateReadSessionAsync</summary>
        public async Task CreateReadSessionAsync()
        {
            // Snippet: CreateReadSessionAsync(string, ReadSession, int, CallSettings)
            // Additional: CreateReadSessionAsync(string, ReadSession, int, CancellationToken)
            // Create client
            BigQueryReadClient bigQueryReadClient = await BigQueryReadClient.CreateAsync();
            // Initialize request argument(s)
            string parent = "projects/[PROJECT]";
            ReadSession readSession = new ReadSession();
            int maxStreamCount = 0;
            // Make the request
            ReadSession response = await bigQueryReadClient.CreateReadSessionAsync(parent, readSession, maxStreamCount);
            // End snippet
        }

        /// <summary>Snippet for CreateReadSession</summary>
        public void CreateReadSession_ResourceNames()
        {
            // Snippet: CreateReadSession(ProjectName, ReadSession, int, CallSettings)
            // Create client
            BigQueryReadClient bigQueryReadClient = BigQueryReadClient.Create();
            // Initialize request argument(s)
            ProjectName parent = new ProjectName("[PROJECT]");
            ReadSession readSession = new ReadSession();
            int maxStreamCount = 0;
            // Make the request
            ReadSession response = bigQueryReadClient.CreateReadSession(parent, readSession, maxStreamCount);
            // End snippet
        }

        /// <summary>Snippet for CreateReadSessionAsync</summary>
        public async Task CreateReadSessionAsync_ResourceNames()
        {
            // Snippet: CreateReadSessionAsync(ProjectName, ReadSession, int, CallSettings)
            // Additional: CreateReadSessionAsync(ProjectName, ReadSession, int, CancellationToken)
            // Create client
            BigQueryReadClient bigQueryReadClient = await BigQueryReadClient.CreateAsync();
            // Initialize request argument(s)
            ProjectName parent = new ProjectName("[PROJECT]");
            ReadSession readSession = new ReadSession();
            int maxStreamCount = 0;
            // Make the request
            ReadSession response = await bigQueryReadClient.CreateReadSessionAsync(parent, readSession, maxStreamCount);
            // End snippet
        }

        /// <summary>Snippet for ReadRows</summary>
        public async Task ReadRows_RequestObject()
        {
            // Snippet: ReadRows(ReadRowsRequest, CallSettings)
            // Create client
            BigQueryReadClient bigQueryReadClient = BigQueryReadClient.Create();
            // Initialize request argument(s)
            ReadRowsRequest request = new ReadRowsRequest
            {
                ReadStreamAsReadStreamName = new ReadStreamName("[PROJECT]", "[LOCATION]", "[SESSION]", "[STREAM]"),
                Offset = 0L,
            };
            // Make the request, returning a streaming response
            BigQueryReadClient.ReadRowsStream response = bigQueryReadClient.ReadRows(request);

            // Read streaming responses from server until complete
            IAsyncEnumerator<ReadRowsResponse> responseStream = response.ResponseStream;
            while (await responseStream.MoveNext())
            {
                ReadRowsResponse responseItem = responseStream.Current;
                // Do something with streamed response
            }
            // The response stream has completed
            // End snippet
        }

        /// <summary>Snippet for ReadRows</summary>
        public async Task ReadRows()
        {
            // Snippet: ReadRows(string, long, CallSettings)
            // Create client
            BigQueryReadClient bigQueryReadClient = BigQueryReadClient.Create();
            // Initialize request argument(s)
            string readStream = "projects/[PROJECT]/locations/[LOCATION]/sessions/[SESSION]/streams/[STREAM]";
            long offset = 0L;
            // Make the request, returning a streaming response
            BigQueryReadClient.ReadRowsStream response = bigQueryReadClient.ReadRows(readStream, offset);

            // Read streaming responses from server until complete
            IAsyncEnumerator<ReadRowsResponse> responseStream = response.ResponseStream;
            while (await responseStream.MoveNext())
            {
                ReadRowsResponse responseItem = responseStream.Current;
                // Do something with streamed response
            }
            // The response stream has completed
            // End snippet
        }

        /// <summary>Snippet for ReadRows</summary>
        public async Task ReadRows_ResourceNames()
        {
            // Snippet: ReadRows(ReadStreamName, long, CallSettings)
            // Create client
            BigQueryReadClient bigQueryReadClient = BigQueryReadClient.Create();
            // Initialize request argument(s)
            ReadStreamName readStream = new ReadStreamName("[PROJECT]", "[LOCATION]", "[SESSION]", "[STREAM]");
            long offset = 0L;
            // Make the request, returning a streaming response
            BigQueryReadClient.ReadRowsStream response = bigQueryReadClient.ReadRows(readStream, offset);

            // Read streaming responses from server until complete
            IAsyncEnumerator<ReadRowsResponse> responseStream = response.ResponseStream;
            while (await responseStream.MoveNext())
            {
                ReadRowsResponse responseItem = responseStream.Current;
                // Do something with streamed response
            }
            // The response stream has completed
            // End snippet
        }

        /// <summary>Snippet for SplitReadStream</summary>
        public void SplitReadStream_RequestObject()
        {
            // Snippet: SplitReadStream(SplitReadStreamRequest, CallSettings)
            // Create client
            BigQueryReadClient bigQueryReadClient = BigQueryReadClient.Create();
            // Initialize request argument(s)
            SplitReadStreamRequest request = new SplitReadStreamRequest
            {
                ReadStreamName = new ReadStreamName("[PROJECT]", "[LOCATION]", "[SESSION]", "[STREAM]"),
                Fraction = 0,
            };
            // Make the request
            SplitReadStreamResponse response = bigQueryReadClient.SplitReadStream(request);
            // End snippet
        }

        /// <summary>Snippet for SplitReadStreamAsync</summary>
        public async Task SplitReadStreamAsync_RequestObject()
        {
            // Snippet: SplitReadStreamAsync(SplitReadStreamRequest, CallSettings)
            // Additional: SplitReadStreamAsync(SplitReadStreamRequest, CancellationToken)
            // Create client
            BigQueryReadClient bigQueryReadClient = await BigQueryReadClient.CreateAsync();
            // Initialize request argument(s)
            SplitReadStreamRequest request = new SplitReadStreamRequest
            {
                ReadStreamName = new ReadStreamName("[PROJECT]", "[LOCATION]", "[SESSION]", "[STREAM]"),
                Fraction = 0,
            };
            // Make the request
            SplitReadStreamResponse response = await bigQueryReadClient.SplitReadStreamAsync(request);
            // End snippet
        }
    }
}
