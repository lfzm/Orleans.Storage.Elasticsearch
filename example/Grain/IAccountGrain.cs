using Orleans;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Grain
{
    public interface IAccountGrain : IGrainWithIntegerKey
    {
        Task Add(AccountModel account);
    }
}
