/* SPDX-License-Identifier: Apache-2.0
*
* The OpenSearch Contributors require contributions made to
* this file be licensed under the Apache-2.0 license or a
* compatible open source license.
*/
/*
* Modifications Copyright OpenSearch Contributors. See
* GitHub history for details.
*
*  Licensed to Elasticsearch B.V. under one or more contributor
*  license agreements. See the NOTICE file distributed with
*  this work for additional information regarding copyright
*  ownership. Elasticsearch B.V. licenses this file to you under
*  the Apache License, Version 2.0 (the "License"); you may
*  not use this file except in compliance with the License.
*  You may obtain a copy of the License at
*
* 	http://www.apache.org/licenses/LICENSE-2.0
*
*  Unless required by applicable law or agreed to in writing,
*  software distributed under the License is distributed on an
*  "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY
*  KIND, either express or implied.  See the License for the
*  specific language governing permissions and limitations
*  under the License.
*/

using System.Runtime.Serialization;
using System.Text;
using OpenSearch.OpenSearch.Xunit.XunitPlumbing;
using FluentAssertions;
using Tests.Core.Client;

namespace Tests.Reproduce
{
	public class GithubIssue3107
	{
		[U] public void FieldResolverRespectsDataMemberAttributes()
		{
			var client = TestClient.DefaultInMemoryClient;

			var document = new SourceEntity
			{
				Name = "name",
				DisplayName = "display name"
			};

			var indexResponse = client.IndexDocument(document);
			var requestJson = Encoding.UTF8.GetString(indexResponse.ApiCall.RequestBodyInBytes);
			requestJson.Should().Contain("display_name");

			var searchResponse = client.Search<SourceEntity>(s => s
				.Query(q => q
					.Terms(t => t
						.Field(f => f.DisplayName)
						.Terms("term")
					)
				)
			);

			requestJson = Encoding.UTF8.GetString(searchResponse.ApiCall.RequestBodyInBytes);
			requestJson.Should().Contain("display_name");
		}

		[DataContract(Name = "source_entity")]
		public class SourceEntity
		{
			[DataMember(Name = "display_name")]
			public string DisplayName { get; set; }

			[DataMember(Name = "name")]
			public string Name { get; set; }
		}
	}
}
