using Pawz.Domain.Entities;
using Pawz.Domain.Interfaces;
using Pawz.Infrastructure.Data;

namespace Pawz.Infrastructure.Repositories;

public class CityRepository : GenericRepository<City, int>, ICityRepository
{
    public CityRepository(AppDbContext context) : base(context)
    {
    }
}
