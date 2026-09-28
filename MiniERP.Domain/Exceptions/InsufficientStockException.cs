using System;
using System.Collections.Generic;
using System.Text;

namespace MiniERP.Domain.Exceptions
{
    public class InsufficientStockException : Exception
    {
        public InsufficientStockException(string productName, int requested, int available)
            : base($"Insufficient stock for '{productName}'. Requested: {requested}, Available: {available}")
        {
        }
    }
}
