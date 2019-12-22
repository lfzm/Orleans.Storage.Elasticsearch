using System;
using System.Threading.Tasks;
using Orleans;

namespace Grain
{
    public interface IUserGrain:IGrainWithIntegerKey
    {
        Task AddUser(UserModel model);

    }
}
