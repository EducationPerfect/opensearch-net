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

using OpenSearch.Client;
using Tests.Core.ManagedOpenSearch.Clusters;
using Tests.Domain;
using Tests.Framework.EndpointTests.TestState;

namespace Tests.QueryDsl.Span.Term
{
	public class SpanTermUsageTests : QueryDslUsageTestsBase
	{
		public SpanTermUsageTests(ReadOnlyCluster i, EndpointUsage usage) : base(i, usage) { }

		protected override ConditionlessWhen ConditionlessWhen => new ConditionlessWhen<ISpanTermQuery>(a => a.SpanTerm)
		{
			q => q.Value = null,
			q => q.Value = string.Empty,
			q => q.Field = null,
		};

		protected override QueryContainer QueryInitializer => new SpanTermQuery
		{
			Name = "named_query",
			Boost = 1.1,
			Value = "kimchy",
			Field = "user"
		};

		protected override object QueryJson => new
		{
			span_term = new
			{
				user = new
				{
					_name = "named_query",
					boost = 1.1,
					value = "kimchy"
				}
			}
		};

		protected override QueryContainer QueryFluent(QueryContainerDescriptor<Project> q) => q
			.SpanTerm(c => c
				.Name("named_query")
				.Boost(1.1)
				.Field("user")
				.Value("kimchy")
			);
	}
}
