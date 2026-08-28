using Updraft.Data.Entities;
using Updraft.Repositories;

namespace Updraft.Services;

public sealed record CreateRequestCommand(
    Guid OfficeId,
    Guid RequesterId,
    string? Proposal,
    string? AmendingBill,
    string? ReintroducingBill,
    string? RelatedAgencies,
    string? RelatedLaw,
    string ScopeResponse,
    string AdministrationResponse,
    string EnforcementResponse,
    string TimingResponse,
    string ExistingLawResponse,
    IReadOnlyList<Guid> CommitteeIds,
    IReadOnlyList<string> TagIds);

public sealed record UpdateRequestCommand(
    Guid RequestId,
    string? Proposal,
    string? AmendingBill,
    string? ReintroducingBill,
    string? RelatedAgencies,
    string? RelatedLaw,
    string ScopeResponse,
    string AdministrationResponse,
    string EnforcementResponse,
    string TimingResponse,
    string ExistingLawResponse,
    RequestStatus Status);

public sealed class RequestService(
    IRequestRepository requestRepository,
    IOfficeRepository officeRepository,
    ITagRepository tagRepository,
    IUserRepository userRepository)
{
    public async Task<Request> CreateRequestAsync(CreateRequestCommand command, CancellationToken cancellationToken)
    {
        if (!await officeRepository.ExistsAsync(command.OfficeId, cancellationToken))
        {
            throw new OfficeNotFoundException(command.OfficeId);
        }

        if (!await userRepository.ExistsAsync(command.RequesterId, cancellationToken))
        {
            throw new UserNotFoundException(command.RequesterId);
        }

        List<Updraft.Data.Entities.Tag> tags = await tagRepository.GetByIdsAsync(command.TagIds, cancellationToken);
        if (tags.Count != command.TagIds.Distinct().Count())
        {
            throw new TagsNotFoundException();
        }


        List<Office> committees = await officeRepository.GetByIdsAsync(command.CommitteeIds, cancellationToken);
        if(committees.Count != command.CommitteeIds.Distinct().Count())
        {
             throw new CommitteesNotFoundException();
        }

        var requestId = Guid.NewGuid();

        var request = new Request
        {
            RequestId = requestId,
            OfficeId = command.OfficeId,
            RequesterId = command.RequesterId,
            Proposal = command.Proposal,
            AmendingBill = command.AmendingBill,
            ReintroducingBill = command.ReintroducingBill,
            RelatedAgencies = command.RelatedAgencies,
            RelatedLaw = command.RelatedLaw,
            ScopeResponse = command.ScopeResponse,
            AdministrationResponse = command.AdministrationResponse,
            EnforcementResponse = command.EnforcementResponse,
            TimingResponse = command.TimingResponse,
            ExistingLawResponse = command.ExistingLawResponse,
            Status = RequestStatus.Unassigned,
            RequestCommittees = [.. committees
                .Select(c => new RequestCommittee
                {
                    RequestId = requestId,
                    OfficeId = c.OfficeId
                })],
            RequestTags = [.. command.TagIds
                .Distinct()
                .Select(tagId => new RequestTag
                {
                    RequestId = requestId,
                    TagId = tagId
                })]
        };

        await requestRepository.AddAsync(request, cancellationToken);
        await requestRepository.SaveChangesAsync(cancellationToken);
        return request;
    }

    public async Task<Request> UpdateRequestAsync(UpdateRequestCommand command, CancellationToken cancellationToken)
    {
        Request? request = await requestRepository.GetByIdAsync(command.RequestId, cancellationToken);
        if (request is null)
        {
            throw new RequestNotFoundException(command.RequestId);
        }

        request.Proposal = command.Proposal;
        request.AmendingBill = command.AmendingBill;
        request.ReintroducingBill = command.ReintroducingBill;
        request.RelatedAgencies = command.RelatedAgencies;
        request.RelatedLaw = command.RelatedLaw;
        request.ScopeResponse = command.ScopeResponse;
        request.AdministrationResponse = command.AdministrationResponse;
        request.EnforcementResponse = command.EnforcementResponse;
        request.TimingResponse = command.TimingResponse;
        request.ExistingLawResponse = command.ExistingLawResponse;
        request.Status = command.Status;

        await requestRepository.SaveChangesAsync(cancellationToken);
        return request;
    }
}
