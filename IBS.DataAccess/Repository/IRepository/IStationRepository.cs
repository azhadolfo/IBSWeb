using IBS.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IBS.DataAccess.Repository.IRepository
{
    public interface IStationRepository : IRepository<Station>
    {
        Task<bool> IsStationCodeExistAsync(string stationCode, CancellationToken cancellationToken = default);

        Task<bool> IsStationNameExistAsync(string stationName, CancellationToken cancellationToken = default);

        Task<bool> IsPosCodeExistAsync(string posCode, CancellationToken cancellationToken = default);

        Task UpdateAsync(Station model, CancellationToken cancellationToken = default);

        string GenerateFolderPath(string stationName);

    }
}
