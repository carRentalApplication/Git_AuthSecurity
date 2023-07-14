using CarRentalEntities;

namespace CarRentalSystem.UtilityServices
{
    public interface IEmailService 
    {
        void SendEmail(EmailModel emailModel);
    }
}
