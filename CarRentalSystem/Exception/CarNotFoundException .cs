namespace CarRentalSystem
{
    public class CarNotFoundException : CarRentalException
    {
        public CarNotFoundException(string carId)
            : base($"Car with ID {carId} not found.")
        {
        }
    }
}
