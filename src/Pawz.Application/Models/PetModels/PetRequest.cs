using Pawz.Application.Models.PetImagesModels;
using Pawz.Domain.Entities;
using Pawz.Domain.Enums;
using System;
using System.Collections.Generic;

namespace Pawz.Application.Models.PetModels;

public class PetRequest
{
    public int Id { get; set; }
    public string Name { get; set; }
    public int SpeciesId { get; set; }
    public Species Species { get; set; }
    public int BreedId { get; set; }
    public Breed Breed { get; set; }
    public int AgeYears { get; set; }
    public int AgeMonths { get; set; }
    public string About { get; set; }
    public decimal Price { get; set; }
    public PetStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public int LocationId { get; set; }
    public Location Location { get; set; }
    public ICollection<PetImageRequest> PetImages { get; set; }  = new List<PetImageRequest>();
    public ICollection<AdoptionRequest> AdoptionRequests { get; set; } = new List<AdoptionRequest>();
    public string PostedByUserId { get; set; }
    public ApplicationUser User { get; set; }
    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
}