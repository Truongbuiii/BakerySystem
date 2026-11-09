namespace BakerySystem.Application.Features.Orders;

public sealed class OrderNotificationService
{
    public event Action? OnOrderChanged;

    public void NotifyOrderChanged()
    {
        try
        {
            OnOrderChanged?.Invoke();
        }
        catch
        {
            // Ignore subscriber exceptions
        }
    }
}
