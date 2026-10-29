namespace Microservice.identity.Models
{
    public class AuditLog
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string Action { get; set; }
        public string Details { get; set; }
        public DateTime CreatedAt { get; set; }

        // Foreign key relationship
        public AppUser User { get; set; }
    }
}
