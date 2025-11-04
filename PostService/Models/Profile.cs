using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PostService.Models
{
    public class Profile
    {
        private const string PROFILE_PICTURE_LINK_DEFAULT = "default_pr_pic";
        [Key]                         
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public string? Username { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }

        public string? ProfilePictureLink { get; set; }
        
        public Profile(string username, string name)
        {
            Username = username;
            Name = name;
            ProfilePictureLink = PROFILE_PICTURE_LINK_DEFAULT;
        }

        public Profile(string username, string name, string description)
        {
            Username = username;
            Name = name;
            Description = description;
            ProfilePictureLink = PROFILE_PICTURE_LINK_DEFAULT;
        }
         
        public Profile() { }
    }
}
