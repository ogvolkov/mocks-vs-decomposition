using System;
using System.Linq;
using System.Threading.Tasks;
using Calculator.Split;
using NUnit.Framework;

namespace Calculator.Tests
{
    [TestFixture]
    public class CalculatorSplitTests
    {
        private Split.Calculator _calculator;

        [SetUp]
        public void SetUp()
        {
            _calculator = new Split.Calculator(new ApiStub());
        }

        [Test]
        public void DoesNotCallApiForSpecialProducts()
        {
            // arrange
            var products = new[]
            {
                new Product { IsSpecial = true, Category = "Special", Group = "Very special" },
                new Product { IsSpecial = false, Category = "Normal", Group = "Usual" }
            };

            // act 
            var requests = _calculator.PrepareApiRequests(products);

            // assert
            var specialRequest = requests.FirstOrDefault(r => r.Category == "Special");
            Assert.That(specialRequest, Is.Null);
        }

        [Test]
        public void CallsApiForNormalProducts()
        {
            // arrange
            var products = new[]
            {
                new Product { IsSpecial = false, Category = "Normal", Group = "Usual" }
            };

            // act 
            var requests = _calculator.PrepareApiRequests(products);

            // assert
            Assert.That(requests.Count, Is.EqualTo(1));
            Assert.That(requests.First().Category, Is.EqualTo("Normal"));
            Assert.That(requests.First().Group, Is.EqualTo("Usual"));
        }

        [Test]
        public void GroupsRequestsByCategoryAndGroup()
        {
            // arrange
            var products = new[]
            {
                new Product { IsSpecial = false, Category = "Normal", Group = "Usual", Weight = 10 },
                new Product { IsSpecial = false, Category = "Normal", Group = "Usual", Weight = 2 }
            };

            // act 
            var requests = _calculator.PrepareApiRequests(products);

            // assert
            Assert.That(requests.Count, Is.EqualTo(1));
            Assert.That(requests.First().Category, Is.EqualTo("Normal"));
            Assert.That(requests.First().Group, Is.EqualTo("Usual"));
        }

        // Note: can we somehow ensure calls are run in parallel?
        [Test]
        public async Task CombinesApiCalls()
        {
            // arrange
            var requests = new[]
            {
                new ApiRequest { Category = "Birds", Group = "Ducks" },
                new ApiRequest { Category = "Birds", Group = "Parrots" },
                new ApiRequest { Category = "Mammals", Group = "Cows" }
            };

            // act 
            // Note: the setup is quite artificial
            var results = (await _calculator.CombineCalls(requests,
                request => Task.FromResult(new ApiResponse { MaxWeight = request.Group.Length * 10 })))
                .ToList();

            // assert
            Assert.That(results.Count, Is.EqualTo(3));
            Assert.That(results[0].Response.MaxWeight, Is.EqualTo(50m));
            Assert.That(results[0].Category, Is.EqualTo("Birds"));
            Assert.That(results[0].Group, Is.EqualTo("Ducks"));

            Assert.That(results[1].Response.MaxWeight, Is.EqualTo(70m));
            Assert.That(results[1].Category, Is.EqualTo("Birds"));
            Assert.That(results[1].Group, Is.EqualTo("Parrots"));

            Assert.That(results[2].Response.MaxWeight, Is.EqualTo(40m));
            Assert.That(results[2].Category, Is.EqualTo("Mammals"));
            Assert.That(results[2].Group, Is.EqualTo("Cows"));
        }

        [Test]
        public void CalculatesExcessiveWeight()
        {
            // arrange            
            var products = new[]
            {
                new Product { IsSpecial = false, Category = "Birds", Group = "Ducks", Weight = 15 },
                new Product { IsSpecial = false, Category = "Birds", Group = "Ducks", Weight = 5 },
                new Product { IsSpecial = false, Category = "Mammals", Group = "Cows", Weight = 1500 },
                // Note: this row is quite important, but does it belong to this test?
                new Product { IsSpecial = true, Category = "Birds", Group = "Ducks", Weight = 100 },
            };

            var apiResponses = new[]
            {
                new GroupResponse { Category = "Birds", Group = "Ducks", Response = new ApiResponse { MaxWeight = 8m } },
                new GroupResponse { Category = "Mammals", Group = "Cows", Response = new ApiResponse { MaxWeight = 1000m } }
            };

            // act
            var results = _calculator.Calculate(products, apiResponses);

            // assert
            Assert.That(results.Count, Is.EqualTo(2));

            Assert.That(results[products[0]].AllowedWeight, Is.EqualTo(8m));
            Assert.That(results[products[0]].Excess, Is.EqualTo(7m));

            Assert.That(results[products[2]].AllowedWeight, Is.EqualTo(1000m));
            Assert.That(results[products[2]].Excess, Is.EqualTo(500m));
        }

        // Note: the aggregated Calculate(IReadOnlyCollection<Product> products) method is never tested

        // Note: Never used by the tests but still required by the constructor
        private class ApiStub : IApi
        {
            public Task<ApiResponse> Get(ApiRequest request) => throw new NotImplementedException();
        }
    }
}
