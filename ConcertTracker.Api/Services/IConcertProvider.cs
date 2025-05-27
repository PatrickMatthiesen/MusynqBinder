using MusynqBinder.Shared.Models;

namespace ConcertTracker.Api.Services;

public interface IConcertProvider {
    Task<IEnumerable<Concert>> FetchConcertsAsync(string artistName);
}
