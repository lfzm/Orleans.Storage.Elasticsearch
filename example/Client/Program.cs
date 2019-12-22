using Grain;
using Microsoft.Extensions.DependencyInjection;
using Orleans;
using System;
using System.Threading.Tasks;

namespace Client
{
    class Program
    {
        static void Main(string[] args)
        {
            IServiceCollection services = new ServiceCollection();
            services.AddLogging();
            services.AddOrleansMultiClient(build =>
            {
                build.AddClient(opt =>
                {
                    opt.ServiceId = "A";
                    opt.ClusterId = "AApp";
                    opt.SetServiceAssembly(typeof(IUserGrain).Assembly);
                    opt.Configure = (b =>
                    {
                        b.UseLocalhostClustering();
                    });
                });
            });

            var sp = services.BuildServiceProvider();

            Console.WriteLine("Enter btn start");
            Console.ReadLine();

            // simple storage
            //var userGrain = sp.GetRequiredService<IOrleansClient>().GetGrain<IUserGrain>(1);
            //userGrain.AddUser(new UserModel()
            //{
            //    Id = 1,
            //    Name = "test user",
            //    Sex = "girl"
            //}).GetAwaiter().GetResult();
            //Console.WriteLine("add user success");

            // model is not the same as document

            var accountGrain = sp.GetRequiredService<IOrleansClient>().GetGrain<IAccountGrain>(1);
            accountGrain.Add(new AccountModel()
            {
                Id = 1,
                Password = "password",
                Username = "username"
            }).GetAwaiter().GetResult();
            Console.WriteLine("add account success");
        }
    }
}
