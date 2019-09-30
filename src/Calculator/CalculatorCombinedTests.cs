using System.Threading.Tasks;
using Calculator.Combined;
using NSubstitute;
using NUnit.Framework;

namespace Calculator.Tests
{
    [TestFixture]
    public class CalculatorCombinedTests
    {
        private IApi _apiFake;

        private Combined.Calculator _calculator;

        [SetUp]
        public void SetUp()
        {
            _apiFake = Substitute.For<IApi>();
            _calculator = new Combined.Calculator(_apiFake);
        }

        // NOTE: every test is async
        [Test]
        public async Task DoesNotCallApiForSpecialProducts()
        {
            // arrange
            var products = new[]
            {
                new Product { IsSpecial = true, Category = "Special", Group = "Very special" },
                new Product { IsSpecial = false, Category = "Normal", Group = "Usual" }
            };

            // act 
            await _calculator.Calculate(products);

            // assert
            _apiFake.DidNotReceive().Get(Arg.Is<ApiRequest>(it => it.Category == "Special"));
        }

        [Test]
        public async Task CallsApiForNormalProducts()
        {
            // arrange
            var products = new[]
            {
                new Product { IsSpecial = false, Category = "Normal", Group = "Usual" }
            };

            // act 
            await _calculator.Calculate(products);

            // assert
            _apiFake.Received().Get(Arg.Is<ApiRequest>(r => r.Category == "Normal" && r.Group == "Usual"));
        }

        [Test]
        public async Task GroupsRequestsByCategoryAndGroup()
        {
            // arrange
            var products = new[]
            {
                new Product { IsSpecial = false, Category = "Normal", Group = "Usual", Weight = 10 },
                new Product { IsSpecial = false, Category = "Normal", Group = "Usual", Weight = 2 }
            };

            // act 
            await _calculator.Calculate(products);

            // assert
            _apiFake.Received(1).Get(Arg.Is<ApiRequest>(r => r.Category == "Normal" && r.Group == "Usual"));
        }

        // Note: We want to test this because we know the implementation might be tricky. However, the relation to visible outcomes is not obvious.                
        // Note: can we somehow ensure calls are run in parallel?
        [Test]
        public async Task CombinesApiCallsCorrectly()
        {
            // arrange
            // Note: complex arrangement which depends both on how we call the API and how we calculate
            var products = new[]
            {
                new Product { Category = "Birds", Group = "Ducks", Weight = 20m },
                new Product { Category = "Birds", Group = "Parrots", Weight = 15m  },
                new Product { Category = "Mammals", Group = "Cows", Weight = 1500m  }
            };

            _apiFake.Get(Arg.Is<ApiRequest>(r => r.Category == "Birds" && r.Group == "Ducks"))
                .Returns(new ApiResponse { MaxWeight = 10m });
            _apiFake.Get(Arg.Is<ApiRequest>(r => r.Category == "Birds" && r.Group == "Parrots"))
                .Returns(new ApiResponse { MaxWeight = 5m });
            _apiFake.Get(Arg.Is<ApiRequest>(r => r.Category == "Mammals" && r.Group == "Cows"))
                .Returns(new ApiResponse { MaxWeight = 1000m });

            // act 
            var results = await _calculator.Calculate(products);

            // assert
            Assert.That(results.Count, Is.EqualTo(3));
            Assert.That(results[products[0]].AllowedWeight, Is.EqualTo(10m));
            Assert.That(results[products[1]].AllowedWeight, Is.EqualTo(5m));
            Assert.That(results[products[2]].AllowedWeight, Is.EqualTo(1000m));
        }
        
        [Test]
        public async Task CalculatesExcessiveWeight()
        {
            // arrange
            var products = new[]
            {
                new Product { IsSpecial = false, Category = "Birds", Group = "Ducks", Weight = 15 },
                new Product { IsSpecial = false, Category = "Birds", Group = "Ducks", Weight = 5 },
                new Product { IsSpecial = false, Category = "Mammals", Group = "Cows", Weight = 1500 },
                new Product { IsSpecial = true, Category = "Birds", Group = "Ducks", Weight = 100 },
            };

            _apiFake.Get(Arg.Is<ApiRequest>(r => r.Category == "Birds" && r.Group == "Ducks"))
                .Returns(new ApiResponse { MaxWeight = 8m });
            _apiFake.Get(Arg.Is<ApiRequest>(r => r.Category == "Mammals" && r.Group == "Cows"))
                .Returns(new ApiResponse { MaxWeight = 1000m });

            // act
            var results = await _calculator.Calculate(products);

            // assert
            Assert.That(results.Count, Is.EqualTo(2));

            Assert.That(results[products[0]].AllowedWeight, Is.EqualTo(8m));
            Assert.That(results[products[0]].Excess, Is.EqualTo(7m));

            Assert.That(results[products[2]].AllowedWeight, Is.EqualTo(1000m));
            Assert.That(results[products[2]].Excess, Is.EqualTo(500m));
        }
    }
}
