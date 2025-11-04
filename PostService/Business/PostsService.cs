using PostService.Data;
using PostService.Dtos;
using PostService.Models;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace PostService.Business
{
    public class PostsService
    {
        private PostRepository _dbContext;

        public PostsService(PostRepository dbContext)
        {
            _dbContext = dbContext;
        }
        //public async Task<int> CreatePost(CreatePostDto newPost)
        //{
        //    //TODO: CHeck formatting and null
        //    var prof = new Profile(newProfile.Username, newProfile.Name, newProfile.Description);
        //    _dbContext.Profile.Add(prof);
        //    await _dbContext.SaveChangesAsync();
        //    return prof.Id;
        //}
        //public async Task<bool> UpdateProfileName(UpdateProfileNameDto dto)
        //{
        //    var profile = await _dbContext.Profile.FindAsync(dto.Id);

        //    if (profile == null)
        //    {
        //        throw new Exception("Profile not found.");
        //    }

        //    profile.Name = dto.Name;

        //    _dbContext.Profile.Update(profile);
        //    await _dbContext.SaveChangesAsync();
        //    return true;
        //}
        //public async Task<bool> UpdateProfileDescription(UpdateProfileDescDto dto)
        //{
        //    var profile = await _dbContext.Profile.FindAsync(dto.Id);

        //    if (profile == null)
        //    {
        //        throw new Exception("Profile not found.");
        //    }

        //    profile.Description = dto.Description;

        //    _dbContext.Profile.Update(profile);
        //    await _dbContext.SaveChangesAsync();
        //    return true;
        //}
        //public async Task UpdateProfilePicture(int id,string url)
        //{
        //    var profile = await _dbContext.Profile.FindAsync(id);

        //    profile.ProfilePictureLink = url;
        //    await _dbContext.SaveChangesAsync();
        //}
        //public IEnumerable<Profile> SearchForAProfile(string username)
        //{
        //    if (string.IsNullOrEmpty(username))
        //    {
        //        throw new ArgumentNullException("username");
        //    }
        //    var profileResults = _dbContext.Profile.Where(p => p.Username.Contains(username)).ToList();
        //    return profileResults;
        //}

        //public async Task<int> DeleteProfile(int id)
        //{
        //    var profile = await _dbContext.Profile.FindAsync(id);

        //    if (profile == null)
        //    {
        //        throw new Exception("Profile not found.");
        //    }

        //    _dbContext.Profile.Remove(profile);
        //    await _dbContext.SaveChangesAsync();
        //    return id;
        //}

        //internal object GetPost(string username)
        //{
        //    throw new NotImplementedException();
        //}

        //internal async Task<bool> UpdatePost(UpdatePostDto dto)
        //{
        //    throw new NotImplementedException();
        //}

        //internal async Task DeletePost(int id)
        //{
        //    throw new NotImplementedException();
        //}
    }
}
