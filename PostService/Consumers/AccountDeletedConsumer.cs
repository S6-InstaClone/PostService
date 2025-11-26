using MassTransit;
using PostService.Data;

namespace PostService.Consumers
{
    public class AccountDeletedConsumer : IConsumer<AccountDeletedEvent>
    {
        private readonly PostRepository _db;

        public AccountDeletedConsumer(PostRepository db)
        {
            _db = db;
        }

        public async Task Consume(ConsumeContext<AccountDeletedEvent> context)
        {
            var userId = context.Message.UserId;

            // Option 1: Delete all posts (full GDPR compliance)
            var posts = _db.Posts.Where(p => p.UserId == userId);
            _db.Posts.RemoveRange(posts);

            // Option 2: Anonymize posts (preserve content)
            // foreach(var post in posts)
            // {
            //     post.UserId = 0; // or special "deleted user" ID
            //     post.Caption = "[User deleted]";
            // }

            await _db.SaveChangesAsync();
        }
    }

    public record AccountDeletedEvent(string UserId, string Username, DateTime DeletedAt);
}
