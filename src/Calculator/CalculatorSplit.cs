using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Calculator.Split
{
    public class Calculator
    {
        private readonly IApi _api;

        public Calculator(IApi api)
        {
            _api = api ?? throw new ArgumentNullException(nameof(api));
        }

        public async Task<IDictionary<Product, CalculationResult>> Calculate(IReadOnlyCollection<Product> products)
        {
            var requests = PrepareApiRequests(products);

            var responses = await CombineCalls(requests, _api.Get);

            return Calculate(products, responses);
        }

        public IReadOnlyCollection<ApiRequest> PrepareApiRequests(IReadOnlyCollection<Product> products)
        {
            var normalProducts = products.Where(p => !p.IsSpecial).ToList();

            var requests = normalProducts.GroupBy(p => new { p.Group, p.Category })
                .Select(g => new ApiRequest
                {
                    Group = g.Key.Group,
                    Category = g.Key.Category
                }).ToList();

            return requests;
        }

        public async Task<IReadOnlyCollection<GroupResponse>> CombineCalls(IEnumerable<ApiRequest> requests, Func<ApiRequest, Task<ApiResponse>> call)
        {
            var tasks = requests.Select(async request =>
            {
                var response = await call(request);
                return new GroupResponse
                {
                    Group = request.Group,
                    Category = request.Category,
                    Response = response
                };
            });

            return await Task.WhenAll(tasks);
        }

        public IDictionary<Product, CalculationResult> Calculate(IReadOnlyCollection<Product> products, IReadOnlyCollection<GroupResponse> responses)
        {
            // Note: duplicate of code in PrepareApiRequests
            var normalProducts = products.Where(p => !p.IsSpecial).ToList();

            var results = new Dictionary<Product, CalculationResult>();

            foreach (var product in normalProducts)
            {
                var matchingResponse = responses
                    .Where(r => r.Category == product.Category && r.Group == product.Group)
                    .Select(r => r.Response)
                    .FirstOrDefault();

                if (matchingResponse != null && product.Weight > matchingResponse.MaxWeight + 5m)
                {
                    results[product] = new CalculationResult
                    {
                        AllowedWeight = matchingResponse.MaxWeight,
                        Excess = product.Weight - matchingResponse.MaxWeight
                    };
                }
            }

            return results;
        }     
    }

    public class GroupResponse
    {
        public string Group { get; set; }

        public string Category { get; set; }

        public ApiResponse Response { get; set; }
    }

    public class Product
    {
        public string Group { get; set; }

        public string Category { get; set; }

        public decimal Weight { get; set; }

        public bool IsSpecial { get; set; }
    }

    public class CalculationResult
    {
        public decimal AllowedWeight { get; set; }

        public decimal Excess { get; set; }
    }

    public interface IApi
    {
        Task<ApiResponse> Get(ApiRequest request);
    }

    public class ApiRequest
    {
        public string Group { get; set; }

        public string Category { get; set; }        
    }

    public class ApiResponse
    {
        public decimal MaxWeight;
    }
}
