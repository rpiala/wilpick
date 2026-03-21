using System.ComponentModel.DataAnnotations;
using System.Numerics;

namespace WilPick.ViewModels
{
    public class ClientListViewModel
    {
        public List<WpAppUserViewModel> Clients { get; set; } = new List<WpAppUserViewModel>();
        public BigInteger TotalClients { get; set; }
    }
}