using Azure;
using Azure.Communication.Email;

namespace MediDropProject.Services;

public class AzureEmailService
{
    private readonly EmailClient _client;
    private readonly string _senderAddress;

    public AzureEmailService(IConfiguration configuration)
    {
        var connectionString = configuration["Email:ConnectionString"]
                               ?? throw new InvalidOperationException(
                                   "Email connection string is missing.");

        _senderAddress = configuration["Email:SenderAddress"]
                         ?? throw new InvalidOperationException(
                             "Email sender address is missing.");

        _client = new EmailClient(connectionString);
    }

    public async Task SendAsync(string recipient, string subject, string htmlBody)
    {
        await _client.SendAsync(WaitUntil.Completed, _senderAddress, recipient, subject, htmlBody);
    }
}