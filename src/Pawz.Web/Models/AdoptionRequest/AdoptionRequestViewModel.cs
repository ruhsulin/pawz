using Pawz.Domain.Entities;
using Pawz.Web.Models.Location;
using Pawz.Web.Models.Pet;
using System;
using System.Collections.Generic;

namespace Pawz.Web.Models.AdoptionRequest;

public class AdoptionRequestViewModel
{
    /// <summary>
    /// The Id of the adoption request
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// The status of the adoption request
    /// </summary>
    public AdoptionRequestStatusViewModel Status { get; set; }

    /// <summary>
    /// The date when the adoption request was made
    /// </summary>
    public DateTime RequestDate { get; set; }

    /// <summary>
    /// The date when the adoption request was responded to
    /// </summary>
    public DateTime ResponseDate { get; set; }

    /// <summary>
    /// The Id of the pet associated with this adoption request
    /// </summary>
    public int? PetId { get; set; }

    /// <summary>
    /// The pet associated with this adoption request
    /// </summary>
    public PetViewModel? Pet { get; set; }

    /// <summary>
    /// The adoption related to this request, if applicable
    /// </summary>
    public Adoption Adoption { get; set; }

    /// <summary>
    /// The ID of the user who made the request.
    /// </summary>
    public string RequesterUserId { get; set; }

    /// <summary>
    /// The user who made the request.
    /// </summary>
    public ApplicationUser User { get; set; }

    /// <summary>
    /// Location of Pet
    /// </summary>
    public int LocationId { get; set; }

    /// <summary>
    /// Location of Pet
    /// </summary>
    public LocationViewModel Location { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the entity is soft-deleted.
    /// This property is implemented from the <see cref="ISoftDelete"/> interface.
    /// </summary>
    public bool IsDeleted { get; set; }

    /// <summary>
    /// Gets or sets the timestamp of when the entity was soft-deleted.
    /// This property is implemented from the <see cref="ISoftDelete"/> interface.
    /// </summary>
    public DateTimeOffset? DeletedAt { get; set; }

    /// <summary>
    /// Gets or sets the collection of adoption requests associated with the entity.
    /// This property holds a list of <see cref="AdoptionRequestViewModel"/> objects.
    /// </summary>
    public IEnumerable<AdoptionRequestViewModel> AdoptionRequests { get; set; }
}
