using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace Pawz.Infrastructure.Data.Seed
{
    public class SeedSpecies
    {
        public static async Task SeedSpeciesData(AppDbContext context)
        {
            if (!await context.Species.AnyAsync())
            {
                await context.Database.ExecuteSqlRawAsync(@"
                    INSERT INTO Species (Id, Name, Description, CreatedAt) VALUES 
                    (1, 'Dog', 'Domesticated carnivorous mammal', datetime('now')), 
                    (2, 'Cat', 'Small domesticated carnivorous mammal', datetime('now'))"
                );

                await context.SaveChangesAsync();
            }
        }
    }
}