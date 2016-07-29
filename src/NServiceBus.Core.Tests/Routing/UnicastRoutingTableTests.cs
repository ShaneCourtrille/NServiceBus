namespace NServiceBus.Core.Tests.Routing
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Extensibility;
    using NServiceBus.Routing;
    using NUnit.Framework;

    [TestFixture]
    class UnicastRoutingTableTests
    {
        [Test]
        public async Task When_registering_multiple_static_routes_for_same_type_should_only_use_last_route()
        {
            var routingTable = new UnicastRoutingTable();
            routingTable.RouteToAddress(typeof(Command), "address@somewhere");
            routingTable.RouteToInstance(typeof(Command), new EndpointInstance("billing"));
            routingTable.RouteToEndpoint(typeof(Command), "sales");

            var routes = await routingTable.GetDestinationsFor(typeof(Command), new ContextBag());

            Assert.That(routes, Has.Count.EqualTo(1));
            var result = routes.Select(async r => await r.Resolve(x => Task.FromResult<IEnumerable<EndpointInstance>>(new[]
            {
                new EndpointInstance(x)
            }))).SelectMany(x => x.Result);
            Assert.That(result.Single().Endpoint, Is.EqualTo("sales"));
        }

        [Test]
        public async Task When_returning_multiple_dynamic_routes_for_same_type_should_return_all_routes()
        {
            var routingTable = new UnicastRoutingTable();
            routingTable.AddDynamic((t, c) => new[]
            {
                UnicastRoute.CreateFromPhysicalAddress("a")
            });
            routingTable.AddDynamic((t, c) => new[]
            {
                UnicastRoute.CreateFromPhysicalAddress("b")
            });
            routingTable.AddDynamic((t, c) => Task.FromResult<IEnumerable<IUnicastRoute>>(new[]
            {
                UnicastRoute.CreateFromPhysicalAddress("c")
            }));

            var routes = await routingTable.GetDestinationsFor(typeof(Command), new ContextBag());

            Assert.That(routes, Has.Count.EqualTo(3));
        }

        [Test]
        public async Task When_static_and_dynamic_routes_found_for_same_type_should_return_static_and_dynamic_routes()
        {
            var routingTable = new UnicastRoutingTable();
            routingTable.RouteToAddress(typeof(Command), "a");
            routingTable.AddDynamic((t, c) => new[]
            {
                UnicastRoute.CreateFromPhysicalAddress("b")
            });
            routingTable.AddDynamic((t, c) => Task.FromResult<IEnumerable<IUnicastRoute>>(new[]
            {
                UnicastRoute.CreateFromPhysicalAddress("c")
            }));

            var routes = await routingTable.GetDestinationsFor(typeof(Command), new ContextBag());

            Assert.That(routes, Has.Count.EqualTo(3));
        }

        class Command
        {
        }
    }
}