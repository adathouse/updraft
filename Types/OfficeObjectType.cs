using HotChocolate.Types;
using Updraft.Data.Entities;
using Updraft.Repositories;

namespace Updraft.Types;

[ObjectType<Office>]
public static partial class OfficeObjectType
{
    static partial void Configure(IObjectTypeDescriptor<Office> descriptor) =>
        descriptor.Ignore(x => x.RequestCommittees);

    [NodeResolver]
    public static Task<Office?> GetOfficeByIdAsync(Guid id, IOfficeRepository officeRepository, CancellationToken cancellationToken) =>
        officeRepository.GetByIdAsync(id, cancellationToken);

    [UsePaging]
    [UseFiltering]
    [UseSorting]
    public static IQueryable<Request> GetRequests([Parent] Office office, IRequestRepository requestRepository) =>
        requestRepository.Query().Where(x => x.OfficeId == office.OfficeId);
}
