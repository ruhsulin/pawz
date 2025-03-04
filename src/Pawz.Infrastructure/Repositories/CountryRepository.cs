using Pawz.Domain.Entities;
using Pawz.Domain.Interfaces;
using Pawz.Infrastructure.Data;

namespace Pawz.Infrastructure.Repositories;

public class CountryRepository : GenericRepository<Country, int>, ICountryRepository
{
    public CountryRepository(AppDbContext context) : base(context)
    {
    }
}
