namespace PostService.Messages
{
    public record AccountDeletedEvent
    {
        public int UserId { get; init; }
        public string Username { get; init; }
        public DateTime DeletedAt { get; init; }
    }
}
