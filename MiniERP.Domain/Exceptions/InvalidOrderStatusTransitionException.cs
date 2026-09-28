using System;
using System.Collections.Generic;
using System.Text;

namespace MiniERP.Domain.Exceptions
{
    public class InvalidOrderStatusTransitionException : Exception
    {
        public InvalidOrderStatusTransitionException(string from, string to)
            : base($"Cannot change order status from '{from}' to '{to}'.")
        {
        }
    }
}
