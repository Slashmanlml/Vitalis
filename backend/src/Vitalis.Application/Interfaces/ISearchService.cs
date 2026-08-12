using Vitalis.Application.DTOs;

namespace Vitalis.Application.Interfaces;

public interface ISearchService
{
    Task<SearchResultDto> BuscarAsync(string query);
}
