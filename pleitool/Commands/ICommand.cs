using System.Threading.Tasks;

namespace Pleiades.Tool.Commands
{
    public interface ICommand
    {
        Task Run();
    }
}
