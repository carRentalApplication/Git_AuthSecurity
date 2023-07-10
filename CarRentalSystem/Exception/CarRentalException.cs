
using System;
namespace CarRentalSystem
{
    public class CarRentalException : Exception
    {
        public CarRentalException()
        {
        }

        public CarRentalException(string message)
            : base(message)
        {
        }
        public CarRentalException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
    }
}
