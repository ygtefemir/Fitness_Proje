using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Web_Proje.Models; // Namespace'ini kontrol et

public class AppointmentStatusWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;

    public AppointmentStatusWorker(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using (var scope = _scopeFactory.CreateScope())
                {
                    var context = scope.ServiceProvider.GetRequiredService<GymContext>();

                    // Tarihi geçmiş olanları bul ve durumunu güncelle
                    var pastAppointments = await context.Appointments
                        .Where(a => a.AppointmentDate < DateTime.Now &&
                               a.Status != Status.Completed && 
                               a.Status != Status.NotCompleted
                                )
                        .ToListAsync(stoppingToken);

                    if (pastAppointments.Any())
                    {
                        foreach (var item in pastAppointments)
                        {
                            if (item.Status == Status.Confirmed)
                                item.Status = Status.Completed;
                            else item.Status = Status.NotCompleted;
                        }
                        await context.SaveChangesAsync(stoppingToken);
                        Console.WriteLine("Randevular güncellendi.");
                    }
                }
            }
            catch (Exception ex)
            {
                // Hata olursa uygulamayı çökertme, logla
                Console.WriteLine($"Worker Hatası: {ex.Message}");
            }

            // 1 Saat bekle sonra tekrar çalış
            await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
        }
    }
}