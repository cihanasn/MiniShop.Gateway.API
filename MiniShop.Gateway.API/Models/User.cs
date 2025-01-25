namespace MiniShop.Gateway.API.Models
{
    public class User
    {
        public User()
        {
            Id = Guid.NewGuid();
        }

        public Guid Id { get; set; }
        public string Username { get; set; } = default!;
        public string Password { get; set; } = default!;
    }

}
