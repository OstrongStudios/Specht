using Specht.Core.Models;

namespace Specht.Core.Services;

public interface IExportService
{
    string ToCsv(IEnumerable<Device> devices);
    string ToJson(IEnumerable<Device> devices);
}
