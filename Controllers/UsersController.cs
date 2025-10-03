using Microsoft.AspNetCore.Mvc;
using WebApi.Models;
using WebApi.Services;
using Microsoft.AspNetCore.Authorization;

namespace WebApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly UserService _userService;
        private readonly IWebHostEnvironment _environment;
        private readonly JwtToken _jwtToken;


        //Inject the UserService
        public UsersController(UserService userService, IWebHostEnvironment environment, JwtToken jwtToken)
        {
            _userService = userService;
            _environment = environment;
            _jwtToken = jwtToken;
        }

        [AllowAnonymous]
        [HttpPost("signup")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> SignUp([FromForm] SignUpRequest request)
        {
            var existingUser = await _userService.GetByUsernameAsync(request.Username);
            if (existingUser != null)
            {
                return Conflict("Username already exists."); // HTTP 409
            }

            // Create new user
            var newUser = new User
            {
                Username = request.Username,
                Email = request.Email,
                PasswordHash = request.Password // Will be hashed in service
            };

            //  profile image
            if (request.ProfileImage != null && request.ProfileImage.Length > 0)
            {
                // Validate file type
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
                var extension = Path.GetExtension(request.ProfileImage.FileName).ToLower();
                if (!allowedExtensions.Contains(extension))
                {
                    return BadRequest("Invalid file type. Only image files are allowed.");
                }

                // Read file into byte array
                using (var memoryStream = new MemoryStream())
                {
                    await request.ProfileImage.CopyToAsync(memoryStream);
                    newUser.ProfileImage = memoryStream.ToArray();
                }
            }

            await _userService.CreateAsync(newUser);

            return CreatedAtAction(nameof(GetUser), new { id = newUser.Id }, newUser);
        }

        [AllowAnonymous]
        [HttpPost("signin")]
        public async Task<IActionResult> SignIn([FromBody] LoginRequest loginRequest)
        {
            var user = await _userService.AuthenticateAsync(loginRequest.Username, loginRequest.Password);

            if (user == null)
            {
                return Unauthorized("Invalid credentials."); // HTTP 401
            }

            // Generate JWT after successful authentication
            var tokenString = _jwtToken.GenerateJSONWebToken(user);

            return Ok(new { Message = "Sign in successful!", UserToken = tokenString });
        }

        [Authorize]
        [HttpGet("{id:length(24)}")]
        public async Task<ActionResult<User>> GetUser(string id)
        {
            var user = await _userService.GetByIdAsync(id);
            if (user is null)
            {
                return NotFound(); // HTTP 404
            }
            return user;
        }

        [Authorize]
        [HttpPut("{id:length(24)}")]
        public async Task<IActionResult> UpdateUser(string id, User updatedUser)
        {
            var user = await _userService.GetByIdAsync(id);
            if (user is null)
            {
                return NotFound();
            }

            updatedUser.Id = user.Id;

            await _userService.UpdateAsync(id, updatedUser);
            return NoContent(); // HTTP 204
        }

        [Authorize]
        [HttpDelete("{id:length(24)}")]
        public async Task<IActionResult> DeleteUser(string id)
        {
            var user = await _userService.GetByIdAsync(id);
            if (user is null)
            {
                return NotFound();
            }

            await _userService.DeleteAsync(id);
            return NoContent();
        }

        [Authorize]
        [HttpPost("{id:length(24)}/upload-profile-image")]
        public async Task<IActionResult> UploadProfileImage(string id, IFormFile file)
        {
            var user = await _userService.GetByIdAsync(id);
            if (user is null)
            {
                return NotFound();
            }

            if (file == null || file.Length == 0)
            {
                return BadRequest("No file uploaded.");
            }

            // Validate file type (e.g., only images)
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
            var extension = Path.GetExtension(file.FileName).ToLower();
            if (!allowedExtensions.Contains(extension))
            {
                return BadRequest("Invalid file type. Only image files are allowed.");
            }

            // Read file into byte array
            using (var memoryStream = new MemoryStream())
            {
                await file.CopyToAsync(memoryStream);
                var imageBytes = memoryStream.ToArray();

                // Update user's profile image in database
                await _userService.UpdateProfileImageAsync(id, imageBytes);
            }

            return Ok(new { Message = "Profile image uploaded successfully." });
        }

        [Authorize]
        [HttpGet("{id:length(24)}/profile-image")]
        public async Task<IActionResult> GetProfileImage(string id)
        {
            var user = await _userService.GetByIdAsync(id);
            if (user is null || user.ProfileImage == null)
            {
                return NotFound();
            }

            // Determine content type based on common image types
            var contentType = "image/jpeg"; 
            return File(user.ProfileImage, contentType);
        }
    }
}


public class SignUpRequest
{
    public string Username { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string Password { get; set; } = null!;
    public IFormFile? ProfileImage { get; set; }
}

public class LoginRequest
{
    public string Username { get; set; } = null!;
    public string Password { get; set; } = null!;
}