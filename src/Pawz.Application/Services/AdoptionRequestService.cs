using AutoMapper;
using Microsoft.Extensions.Logging;
using Pawz.Application.Interfaces;
using Pawz.Application.Models;
using Pawz.Application.Models.NotificationModels;
using Pawz.Domain.Common;
using Pawz.Domain.Entities;
using Pawz.Domain.Enums;
using Pawz.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Pawz.Application.Services;

public class AdoptionRequestService : IAdoptionRequestService
{
    private readonly IAdoptionRequestRepository _adoptionRequestRepository;
    private readonly IAdoptionRepository _adoptionRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<AdoptionService> _logger;
    private readonly IMapper _mapper;
    private readonly ILocationService _locationService;
    private readonly IUserAccessor _userAccessor;
    private readonly INotificationService _notificationService;
    private readonly IPetRepository _petRepository;

    public AdoptionRequestService(
        IAdoptionRequestRepository adoptionRequestRepository,
        IUnitOfWork unitOfWork,
        ILogger<AdoptionService> logger,
        IMapper mapper,
        ILocationService locationService,
        IUserAccessor userAccessor,
        IAdoptionRepository adoptionRepository,
        INotificationService notificationService,
        IPetRepository petRepository)
    {
        _adoptionRequestRepository = adoptionRequestRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
        _mapper = mapper;
        _locationService = locationService;
        _userAccessor = userAccessor;
        _adoptionRepository = adoptionRepository;
        _notificationService = notificationService;
        _petRepository = petRepository;
    }

    public async Task<Result<bool>> CreateAdoptionRequestAsync(AdoptionRequestCreateRequest adoptionRequestCreateRequest, CancellationToken cancellationToken)
    {
        try
        {
            var location = new Location
            {
                CityId = adoptionRequestCreateRequest.CityId,
                Address = adoptionRequestCreateRequest.Address,
                PostalCode = adoptionRequestCreateRequest.PostalCode
            };

            var locationInsertResult = await _locationService.CreateLocationAsync(location, cancellationToken);

            if (!locationInsertResult.IsSuccess)
            {
                _logger.LogError("Failed to create a location for the pet with Id: {AdoptionRequestId}", adoptionRequestCreateRequest.Id);
                return Result<bool>.Failure(LocationErrors.CreationFailed);
            }

            var pet = await _petRepository.GetPetByIdWithRelatedEntitiesAsync(adoptionRequestCreateRequest.PetId, cancellationToken);

            if (pet == null)
            {
                _logger.LogError("Pet with Id: {PetId} not found.", adoptionRequestCreateRequest.PetId);
                return Result<bool>.Failure(PetErrors.NoPetsFound());
            }

            var adoptionRequest = _mapper.Map<AdoptionRequest>(adoptionRequestCreateRequest);
            adoptionRequest.RequestDate = DateTime.Now;
            adoptionRequest.Location = locationInsertResult.Value;
            adoptionRequest.RequesterUserId = _userAccessor.GetUserId();
            adoptionRequest.Status = AdoptionRequestStatus.Pending;
            adoptionRequest.Email = _userAccessor.GetEmail();
            adoptionRequest.Pet = pet;

            await _adoptionRequestRepository.InsertAsync(adoptionRequest, cancellationToken);

            var isAdoptionRequestCreated = await _unitOfWork.SaveChangesAsync(cancellationToken) > 0;

            if (isAdoptionRequestCreated)
            {
                var message = $"{_userAccessor.GetUserFirstName()} has made an adoption request for your pet: {adoptionRequest.Pet.Name}.";
                var notificationResult = await SendNotificationAsync(adoptionRequest.RequesterUserId, pet.User.Id, pet.Id, message, NotificationType.AdoptionRequest, cancellationToken);

                if (!notificationResult.IsSuccess)
                {
                    return Result<bool>.Failure(NotificationErrors.CreationFailed);
                }

                return Result<bool>.Success();
            }

            _logger.LogError("Failed to create an Adoption Request with Id: {AdoptionRequestId} from UserId: {UserId}", adoptionRequest.Id,
                adoptionRequest.RequesterUserId);

            return Result<bool>.Failure(AdoptionRequestErrors.CreationFailed);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred in the {ServiceName} while attempting to create a Adoption Request for the UserId: {UserId}",
                nameof(AdoptionService), adoptionRequestCreateRequest.RequesterUserId);
            return Result<bool>.Failure(AdoptionRequestErrors.CreationUnexpectedError);
        }
    }

    public async Task<Result<IEnumerable<AdoptionRequest>>> GetAllAdoptionRequestsAsync(CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Started retrieving all adoption requests.");

            var adoptionsRequests = await _adoptionRequestRepository.GetAllAsync(cancellationToken);

            _logger.LogInformation("Successfully retrieved all adoption requests.");
            return Result<IEnumerable<AdoptionRequest>>.Success(adoptionsRequests);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred in the {ServiceName} while attempting to retrieve all adoption requests.",
                nameof(AdoptionRequestService));
            return Result<IEnumerable<AdoptionRequest>>.Failure(AdoptionRequestErrors.RetrievalError);
        }
    }

    public async Task<Result<AdoptionRequest>> GetAdoptionRequestByIdAsync(int adoptionRequestId, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Started retrieving Adoption Requests with Id: {AdoptionRequestId}", adoptionRequestId);

            var adoptionRequest = await _adoptionRequestRepository.GetByIdAsync(adoptionRequestId, cancellationToken);

            if (adoptionRequest is null)
            {
                _logger.LogWarning("Adoption Request with Id: {AdoptionRequestId} was not found.", adoptionRequestId);
                return Result<AdoptionRequest>.Failure(AdoptionRequestErrors.NotFound(adoptionRequestId));
            }

            _logger.LogInformation("Successfully retrieved Adoption Request with Id: {AdoptionRequestId}", adoptionRequestId);
            return Result<AdoptionRequest>.Success(adoptionRequest);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred in the {ServiceName} while attempting to retrieve Adoption Request with Id: {AdoptionRequestId}",
                nameof(AdoptionRequestService), adoptionRequestId);
            return Result<AdoptionRequest>.Failure(AdoptionRequestErrors.RetrievalError);
        }
    }

    public async Task<Result<bool>> UpdateAdoptionRequestAsync(AdoptionRequest adoptionRequest, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Started updating Adoption Request with Id: {AdoptionRequestId} from UserId: {UserId}", adoptionRequest.Id,
                adoptionRequest.RequesterUserId);

            await _adoptionRequestRepository.UpdateAsync(adoptionRequest, cancellationToken);
            var adoptionRequestUpdated = await _unitOfWork.SaveChangesAsync(cancellationToken) > 0;

            if (adoptionRequestUpdated)
            {
                _logger.LogInformation("Successfully updated Adoption Request with Id: {AdoptionRequestId} from UserId: {UserId}", adoptionRequest.Id,
                    adoptionRequest.RequesterUserId);
                return Result<bool>.Success();
            }

            _logger.LogWarning("Failed to update Adoption Request with Id: {AdoptionRequestId} from UserId: {UserId}. No changes were detected.",
                adoptionRequest.Id, adoptionRequest.RequesterUserId);
            return Result<bool>.Failure(AdoptionErrors.NoChangesDetected);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "An error occurred in the {ServiceName} while attempting to update Adoption Request with Id: {AdoptionRequestId} for the UserId: {UserId}",
                nameof(AdoptionRequestService), adoptionRequest.Id, adoptionRequest.RequesterUserId);
            return Result<bool>.Failure(AdoptionRequestErrors.UpdateUnexpectedError);
        }
    }

    public async Task<Result<bool>> DeleteAdoptionRequestAsync(int adoptionRequestId, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Started deleting Adoption Request with Id: {AdoptionRequestId}", adoptionRequestId);

            var adoption = await _adoptionRequestRepository.GetByIdAsync(adoptionRequestId, cancellationToken);
            if (adoption is null)
            {
                _logger.LogWarning("Adoption Request with Id: {AdoptionRequestId} was not found.", adoptionRequestId);
                return Result<bool>.Failure(AdoptionRequestErrors.NotFound(adoptionRequestId));
            }

            await _adoptionRequestRepository.DeleteAsync(adoption, cancellationToken);
            var adoptionDeleted = await _unitOfWork.SaveChangesAsync(cancellationToken) > 0;

            if (adoptionDeleted)
            {
                _logger.LogInformation("Successfully deleted Adoption Request with Id: {AdoptionRequestId}", adoptionRequestId);
                return Result<bool>.Success();
            }

            _logger.LogWarning("Failed to delete Adoption Request with Id: {AdoptionRequestId}. No changes were detected.", adoptionRequestId);
            return Result<bool>.Failure(AdoptionRequestErrors.NoChangesDetected);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred in the {ServiceName} while attempting to delete Adoption Request with Id: {AdoptionRequestId}",
                nameof(AdoptionRequestService), adoptionRequestId);
            return Result<bool>.Failure(AdoptionRequestErrors.DeletionUnexpectedError);
        }
    }

    public async Task<Result<List<AdoptionRequestResponse>>> GetAdoptionRequestsByPetIdAsync(int petId, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Started retrieving Adoption Requests for PetId: {PetId}", petId);

            var adoptionRequests = await _adoptionRequestRepository.GetByPetIdAsync(petId, cancellationToken);

            if (adoptionRequests == null || !adoptionRequests.Any())
            {
                _logger.LogInformation("No Adoption Requests found for PetId: {PetId}", petId);
                return Result<List<AdoptionRequestResponse>>.Success(new List<AdoptionRequestResponse>());
            }

            var adoptionRequestResponses = _mapper.Map<List<AdoptionRequestResponse>>(adoptionRequests);

            _logger.LogInformation("Successfully retrieved {Count} Adoption Requests for PetId: {PetId}", adoptionRequests.Count(), petId);
            return Result<List<AdoptionRequestResponse>>.Success(adoptionRequestResponses);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred in the {ServiceName} while attempting to retrieve Adoption Requests for PetId: {PetId}",
                nameof(AdoptionRequestService), petId);
            return Result<List<AdoptionRequestResponse>>.Failure(AdoptionRequestErrors.RetrievalError);
        }
    }

    public async Task<Result<bool>> AcceptAdoptionRequestAsync(int adoptionRequestId, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Started accepting Adoption Request with Id: {AdoptionRequestId}", adoptionRequestId);

            var adoptionRequest = await _adoptionRequestRepository.GetByAdoptionIdAsync(adoptionRequestId, cancellationToken);

            if (adoptionRequest is null)
            {
                _logger.LogWarning("Adoption Request with Id: {AdoptionRequestId} was not found.", adoptionRequestId);
                return Result<bool>.Failure(AdoptionRequestErrors.NotFound(adoptionRequestId));
            }

            adoptionRequest.Status = AdoptionRequestStatus.Approved;
            adoptionRequest.ResponseDate = DateTime.UtcNow;

            var pet = adoptionRequest.Pet;
            if (pet is null)
            {
                _logger.LogError("Pet with Id: {PetId} not found for Adoption Request with Id: {AdoptionRequestId}", adoptionRequest.PetId, adoptionRequestId);
                return Result<bool>.Failure(PetErrors.NotFound(adoptionRequest.PetId!.Value));
            }
            pet.Status = PetStatus.Approved;

            var adoption = new Adoption
            {
                AdoptionRequestId = adoptionRequestId,
                AdoptionRequest = adoptionRequest,
                AdoptionDate = DateTime.UtcNow,
                AdoptionFee = 0,
                IsDeleted = false
            };

            await _adoptionRepository.InsertAsync(adoption, cancellationToken);

            var changesSaved = await _unitOfWork.SaveChangesAsync(cancellationToken) > 0;

            await RejectOtherAdoptionRequestsAsync(adoptionRequest.PetId!.Value, adoptionRequestId, cancellationToken);

            if (changesSaved)
            {
                var message = $"Your adoption request for the pet: {adoptionRequest.Pet!.Name} has been accepted.";
                var notificationResult = await SendNotificationAsync(_userAccessor.GetUserId(), adoptionRequest.RequesterUserId, adoptionRequest.PetId!.Value, message, NotificationType.RequestAccepted, cancellationToken);

                if (!notificationResult.IsSuccess)
                {
                    return Result<bool>.Failure(NotificationErrors.CreationFailed);
                }

                return Result<bool>.Success();
            }

            _logger.LogWarning("Failed to accept Adoption Request with Id: {AdoptionRequestId}", adoptionRequestId);
            return Result<bool>.Failure(AdoptionRequestErrors.CreationFailed);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while accepting Adoption Request with Id: {AdoptionRequestId}", adoptionRequestId);
            return Result<bool>.Failure(AdoptionRequestErrors.UpdateUnexpectedError);
        }
    }

    private async Task<Result<bool>> RejectOtherAdoptionRequestsAsync(int petId, int approvedRequestId, CancellationToken cancellationToken)
    {
        try
        {
            var pendingAdoptionRequests = await _adoptionRequestRepository.GetByPetIdAsync(petId, cancellationToken);

            if (pendingAdoptionRequests is null || !pendingAdoptionRequests.Any())
            {
                _logger.LogWarning("No pending adoption requests found for pet with ID {PetId}", petId);
                return Result<bool>.Failure(AdoptionRequestErrors.RetrievalError);
            }

            var rejectionCandidates = pendingAdoptionRequests.Where(request => request.Id != approvedRequestId).ToList();

            if (rejectionCandidates is null || rejectionCandidates.Count == 0)
            {
                _logger.LogInformation("There are no other pending adoption requests for pet with ID {PetId}", petId);
                return Result<bool>.Success();
            }

            foreach (var request in rejectionCandidates)
            {
                request.Status = AdoptionRequestStatus.Rejected;
                request.ResponseDate = DateTime.UtcNow;

                var message = $"Your adoption request for the pet: {request.Pet!.Name} has been rejected.";
                var notificationResult = await SendNotificationAsync(_userAccessor.GetUserId(), request.RequesterUserId, request.PetId.Value, message, NotificationType.RequestRejected, cancellationToken);

                if (!notificationResult.IsSuccess)
                {
                    _logger.LogError("Failed to create notification for rejected Adoption Request with Id: {AdoptionRequestId}", request.Id);
                    return Result<bool>.Failure(NotificationErrors.CreationFailed);
                }
            }

            await _adoptionRequestRepository.UpdateListAsync(rejectionCandidates, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result<bool>.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexpected error occurred while rejecting other adoption requests for pet with ID {PetId}", petId);
            return Result<bool>.Failure(AdoptionRequestErrors.UpdateUnexpectedError);
        }
    }

    public async Task<Result<bool>> RejectAdoptionRequestAsync(int adoptionRequestId, CancellationToken cancellationToken)
    {
        try
        {
            var adoptionRequest = await _adoptionRequestRepository.GetByIdAsync(adoptionRequestId, cancellationToken);
            if (adoptionRequest is null)
            {
                _logger.LogWarning("Adoption Request with Id: {AdoptionRequestId} was not found.", adoptionRequestId);
                return Result<bool>.Failure(AdoptionRequestErrors.NotFound(adoptionRequestId));
            }

            adoptionRequest.Status = AdoptionRequestStatus.Rejected;
            adoptionRequest.ResponseDate = DateTime.Now;

            await _adoptionRequestRepository.UpdateAsync(adoptionRequest, cancellationToken);

            var changesSaved = await _unitOfWork.SaveChangesAsync(cancellationToken) > 0;

            if (changesSaved)
            {
                var message = $"Your adoption request for the pet: {adoptionRequest.Pet.Name} has been rejected.";
                var notificationResult = await SendNotificationAsync(_userAccessor.GetUserId(), adoptionRequest.RequesterUserId, adoptionRequest.PetId.Value, message, NotificationType.RequestRejected, cancellationToken);

                if (!notificationResult.IsSuccess)
                {
                    return Result<bool>.Failure(NotificationErrors.CreationFailed);
                }

                return Result<bool>.Success();
            }

            _logger.LogError("Failed to reject Adoption Request with Id: {AdoptionRequestId}", adoptionRequestId);
            return Result<bool>.Failure(AdoptionRequestErrors.UpdateUnexpectedError);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while rejecting Adoption Request with Id: {AdoptionRequestId}", adoptionRequestId);
            return Result<bool>.Failure(AdoptionRequestErrors.UpdateUnexpectedError);
        }
    }

    public async Task<Result<bool>> HasUserMadeRequestForPetAsync(int petId, CancellationToken cancellationToken)
    {
        string userId = _userAccessor.GetUserId();
        try
        {
            if (userId is null)
            {
                _logger.LogError("User with ID: {userId} doesn't exists", userId);
                return Result<bool>.Failure(UsersErrors.NotFound(userId));
            }

            var exists = await _adoptionRequestRepository.ExistsByUserIdAndPetIdAsync(userId, petId, cancellationToken);
            return Result<bool>.Success(exists);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while checking if user {UserId} has made a request for pet {PetId}", userId, petId);
            return Result<bool>.Failure(AdoptionRequestErrors.RetrievalError);
        }
    }

    public async Task<Result<(bool HasExistingRequest, int? AdoptionRequestId)>> CheckUserAdoptionRequestForPetAsync(int petId, CancellationToken cancellationToken)
    {
        try
        {
            var currentUserId = _userAccessor.GetUserId();
            var requestResult = await GetAdoptionRequestsByPetIdAsync(petId, cancellationToken);

            if (!requestResult.IsSuccess)
            {
                _logger.LogError("Failed to retrieve adoption requests for pet with ID {PetId}", petId);
                return Result<(bool, int?)>.Failure(AdoptionRequestErrors.FailedToRetrieveAdoptionRequests);
            }

            var userAdoptionRequest = requestResult.Value.FirstOrDefault(req => req.RequesterUserId == currentUserId);

            if (userAdoptionRequest != null)
            {
                return Result<(bool, int?)>.Success((true, userAdoptionRequest.Id));
            }

            return Result<(bool, int?)>.Success((false, null));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while checking user adoption request for pet with ID {PetId}", petId);
            return Result<(bool, int?)>.Failure(AdoptionRequestErrors.RetrievalError);
        }
    }

    public async Task<Result<bool>> CancelAdoptionRequestAsync(int adoptionRequestId, CancellationToken cancellationToken)
    {
        try
        {
            var adoptionRequest = await _adoptionRequestRepository.GetByIdAsync(adoptionRequestId, cancellationToken);
            if (adoptionRequest is null)
            {
                _logger.LogWarning("Adoption Request with Id: {AdoptionRequestId} was not found.", adoptionRequestId);
                return Result<bool>.Failure(AdoptionRequestErrors.NotFound(adoptionRequestId));
            }

            adoptionRequest.Status = AdoptionRequestStatus.Cancelled;
            adoptionRequest.IsDeleted = true;
            adoptionRequest.ResponseDate = DateTime.UtcNow;

            await _adoptionRequestRepository.UpdateAsync(adoptionRequest, cancellationToken);
            var changesSaved = await _unitOfWork.SaveChangesAsync(cancellationToken) > 0;

            if (changesSaved)
            {
                _logger.LogInformation("Successfully cancelled Adoption Request with Id: {AdoptionRequestId}", adoptionRequestId);
                return Result<bool>.Success();
            }

            _logger.LogWarning("Failed to cancel Adoption Request with Id: {AdoptionRequestId}", adoptionRequestId);
            return Result<bool>.Failure(AdoptionRequestErrors.UpdateUnexpectedError);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while cancelling Adoption Request with Id: {AdoptionRequestId}", adoptionRequestId);
            return Result<bool>.Failure(AdoptionRequestErrors.UpdateUnexpectedError);
        }
    }

    private async Task<Result<bool>> SendNotificationAsync(string senderId, string recipientId, int petId, string message, NotificationType notificationType, CancellationToken cancellationToken)
    {
        var notificationRequest = new NotificationRequest
        {
            SenderId = senderId,
            RecipientId = recipientId,
            PetId = petId,
            Message = message,
            Type = notificationType
        };

        var notificationResult = await _notificationService.CreateNotificationAsync(notificationRequest, cancellationToken);

        if (!notificationResult.IsSuccess)
        {
            _logger.LogError("Failed to create notification for the PetId: {PetId}", petId);
            return Result<bool>.Failure(NotificationErrors.CreationFailed);
        }

        return Result<bool>.Success();
    }
}
