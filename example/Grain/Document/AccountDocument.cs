using Nest;

namespace Grain
{
    [ElasticsearchType(Name = "user")]
    public class AccountDocument
    {
        public const string IndexName = "orleans-account";
        public int Id { get; set; }
        public string Username { get; set; }

        public string Password { get; set; }
    }
}
