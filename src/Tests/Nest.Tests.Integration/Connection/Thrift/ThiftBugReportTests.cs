﻿using System;
using System.Linq;
using Elasticsearch.Net.Connection.Thrift;
using Elasticsearch.Net.ConnectionPool;
using FluentAssertions;
using NUnit.Framework;

namespace Nest.Tests.Integration.Core.Bulk
{
	[TestFixture]
	public class ThiftBugReportTests : IntegrationTests
	{
		[Test]
		public void IndexExistShouldNotThrowOn404()
		{
			var isValidThriftConnection = this.ThriftClient.RootNodeInfo().IsValid;
			isValidThriftConnection.Should().BeTrue();

			var unknownIndexResult = this.ThriftClient.IndexExists(i=>i.Index("i-am-running-out-of-clever-index-names"));
			unknownIndexResult.Should().NotBeNull();
			unknownIndexResult.IsValid.Should().BeTrue();

			unknownIndexResult.Exists.Should().BeFalse();

			unknownIndexResult.ConnectionStatus.HttpStatusCode.Should().Be(404);

		}

		[Test]
		public void EmptyResponseShouldNotThrowError()
		{
			var isValidThriftConnection = this.ThriftClient.RootNodeInfo().IsValid;
			isValidThriftConnection.Should().BeTrue();

			var result = this.ThriftClient.Connection.HeadSync(ElasticsearchConfiguration.CreateBaseUri(9500));
			result.Success.Should().BeTrue();
			result.OriginalException.Should().BeNull();
		}

		[Test]
		public void ShouldFailoverOnThriftConnections()
		{
			var uris = new []
			{
				new Uri("http://INVALID_HOST"),
				new Uri("http://INVALID_HOST2"),
				new Uri("http://localhost:9500")
			};
			var connectionPool = new StaticConnectionPool(uris, randomizeOnStartup: false);
			var settings = new ConnectionSettings(connectionPool, ElasticsearchConfiguration.DefaultIndex)
				.ExposeRawResponse()
				.SetTimeout(2000);
			var client = new ElasticClient(settings, new ThriftConnection(settings));

			var results = client.Search<dynamic>(s => s.MatchAll());
			results.IsValid.Should().BeTrue("{0}", results.ConnectionStatus.ToString());
			results.ConnectionStatus.NumberOfRetries.Should().Be(2);

			results = client.Search<dynamic>(s => s.MatchAll());
			results.IsValid.Should().BeTrue("{0}", results.ConnectionStatus.ToString());
			results.ConnectionStatus.NumberOfRetries.Should().Be(0);
		}
	}
}
