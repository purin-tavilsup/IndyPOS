using System;

namespace IndyPOS.Facade.Exceptions
{
    public class ProductNotFoundException : Exception
    {
        public ProductNotFoundException(string message) : base(message)
        {
		}
    }
}
