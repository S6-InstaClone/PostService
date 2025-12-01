namespace AccountService.Messages
{
    /// <summary>
    /// Event received when a user account is deleted (GDPR compliance)
    /// PostService should delete all posts by this user
    /// </summary>
    public record AccountDeletedEvent
    {
        /// <summary>
        /// Keycloak user ID (UUID format string)
        /// </summary>
        public string UserId { get; init; } = string.Empty;

        public string? Username { get; init; }

        public string? Email { get; init; }

        public DateTime DeletedAt { get; init; }

        /// <summary>
        /// Reason for deletion (e.g., "GDPR_USER_REQUEST", "ADMIN_ACTION")
        /// </summary>
        public string Reason { get; init; } = string.Empty;
    }
}