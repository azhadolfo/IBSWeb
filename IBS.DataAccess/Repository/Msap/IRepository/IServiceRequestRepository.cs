using IBS.DataAccess.Repository.IRepository;
using IBS.Models.Msap;
using IBS.Models.Msap.ViewModels;

namespace IBS.DataAccess.Repository.Msap.IRepository
{
    public interface IServiceRequestRepository : IRepository<DispatchTicket>
    {
        Task SaveAsync(CancellationToken cancellationToken);

        Task<ServiceRequestViewModel> GetDispatchTicketSelectLists(ServiceRequestViewModel model, CancellationToken cancellationToken = default);

    }
}


