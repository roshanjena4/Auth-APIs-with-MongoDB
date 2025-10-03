
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using WebApi.Models;

namespace WebApi.Services
{
    public class UserService
    {
        private readonly IMongoCollection<User> _usersCollection;

    public UserService(IMongoClient mongoClient, IOptions<MongoDbSettings> dbSettings)
    {
        var mongoDatabase = mongoClient.GetDatabase(dbSettings.Value.DatabaseName);
        _usersCollection = mongoDatabase.GetCollection<User>(dbSettings.Value.UsersCollectionName);
    }

    public async Task CreateAsync(User newUser)
    {
        //Hash the password before saving it!
        newUser.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newUser.PasswordHash);
        await _usersCollection.InsertOneAsync(newUser);
    }
 
    public async Task<User?> AuthenticateAsync(string username, string password)
    {
        var user = await _usersCollection.Find(u => u.Username == username).FirstOrDefaultAsync();

        //If user is found and password is correct, return user. Otherwise, return null.
        if (user != null && BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
        {
            return user;
        }

        return null;
    }

    // GET by Username 
    public async Task<User?> GetByUsernameAsync(string username) =>
        await _usersCollection.Find(u => u.Username == username).FirstOrDefaultAsync();
        
    // GET by ID
    public async Task<User?> GetByIdAsync(string id) =>
        await _usersCollection.Find(u => u.Id == id).FirstOrDefaultAsync();

    // UPDATE
    public async Task UpdateAsync(string id, User updatedUser) =>
        await _usersCollection.ReplaceOneAsync(u => u.Id == id, updatedUser);

    // UPDATE Profile Image
    public async Task UpdateProfileImageAsync(string id, byte[] profileImage)
    {
        var update = Builders<User>.Update.Set(u => u.ProfileImage, profileImage);
        await _usersCollection.UpdateOneAsync(u => u.Id == id, update);
    }

    // DELETE
    public async Task DeleteAsync(string id) =>
        await _usersCollection.DeleteOneAsync(u => u.Id == id);
    }
}