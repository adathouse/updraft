using Updraft.Data.Entities;
using Updraft.Repositories;

namespace Updraft.Services;

public sealed record NewAttachmentCommand(string StorageKey, AttachmentRole AttachmentRole);

public sealed record CreateRequestCommand(
    Guid OfficeId,
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
    IReadOnlyList<string> TagIds,
    IReadOnlyList<NewAttachmentCommand> Attachments);

public sealed class RequestService(
    IRequestRepository requestRepository,
    IOfficeRepository officeRepository,
    ICommitteeRepository committeeRepository,
    ITagRepository tagRepository)
{
    public async Task<Request> CreateRequestAsync(CreateRequestCommand command, CancellationToken cancellationToken)
    {
        if (!await officeRepository.ExistsAsync(command.OfficeId, cancellationToken))
        {
            throw new InvalidOperationException("Office was not found.");
        }

        List<Committee> committees = await committeeRepository.GetByIdsAsync(command.CommitteeIds, cancellationToken);
        if (committees.Count != command.CommitteeIds.Distinct().Count())
        {
            throw new InvalidOperationException("One or more committees were not found.");
        }

        List<Updraft.Data.Entities.Tag> tags = await tagRepository.GetByIdsAsync(command.TagIds, cancellationToken);
        if (tags.Count != command.TagIds.Distinct().Count())
        {
            throw new InvalidOperationException("One or more tags were not found.");
        }

        var requestId = Guid.NewGuid();

        var request = new Request
        {
            RequestId = requestId,
            OfficeId = command.OfficeId,
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
            RequestCommittees = command.CommitteeIds
                .Distinct()
                .Select(committeeId => new RequestCommittee
                {
                    RequestId = requestId,
                    CommitteeId = committeeId
                })
                .ToList(),
            RequestTags = command.TagIds
                .Distinct()
                .Select(tagId => new RequestTag
                {
                    RequestId = requestId,
                    TagId = tagId
                })
                .ToList(),
            Attachments = command.Attachments
                .Select(attachment => new Attachment
                {
                    AttachmentId = Guid.NewGuid(),
                    RequestId = requestId,
                    StorageKey = attachment.StorageKey,
                    AttachmentRole = attachment.AttachmentRole
                })
                .ToList()
        };

        await requestRepository.AddAsync(request, cancellationToken);
        await requestRepository.SaveChangesAsync(cancellationToken);
        return request;
    }
}