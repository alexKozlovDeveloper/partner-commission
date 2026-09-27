namespace PartnerCommission.Commissions.Domain;

public class FibonacciSchema : ICommissionSchema
{
    public SchemaType Type => SchemaType.Fibonacci;

    private readonly Dictionary<int, int> _fibonacciSequence = new()
    {
        [1] = 0,
        [2] = 1
    };

    public decimal RateFor(int level)
    {
        if(level <= 0)
            return 0;

        if(_fibonacciSequence.TryGetValue(level, out int result))
            return result;

        while(_fibonacciSequence.Count < level) 
        {
            var a = _fibonacciSequence[_fibonacciSequence.Count - 1];
            var b = _fibonacciSequence[_fibonacciSequence.Count];

            _fibonacciSequence.Add(_fibonacciSequence.Count + 1, a + b);
        }

        return _fibonacciSequence[level];
    }

}
