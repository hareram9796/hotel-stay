using HotelStay.Api.Models;

namespace HotelStay.Api.Providers;

public interface IHotelProvider
{
    string ProviderId { get; }
    Task<IEnumerable<AvailabilityResult>> SearchAsync(SearchParams parameters);
}
