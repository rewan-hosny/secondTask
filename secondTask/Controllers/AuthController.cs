using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using secondTask.Dto;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.Json;
using System.Text;
using System;
using secondTask.models;
using Microsoft.EntityFrameworkCore;

namespace secondTask.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly DataContext _context;



        public AuthController(IConfiguration configuration, DataContext context)
        {
            _configuration = configuration;
            _context = context;
        }
    
        [HttpPost("register")]
        public async Task<ActionResult<string>> Register(RegisterDto request)
        {
            if (!ModelState.IsValid)
            {
                // If the model state is invalid, return the validation errors
                var errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage));
                return BadRequest(errors);
            }
            var existingUser = await _context.Persons.FirstOrDefaultAsync(u => u.Email == request.Email);
            if (existingUser != null)
            {
                return BadRequest(new { error = "User already exists." });
           
            }

            var person = new Person
            {
                Email = request.Email,
                Password = BCrypt.Net.BCrypt.HashPassword(request.Password),
                FirstName = request.FirstName,
                LastName = request.LastName
            };

            _context.Persons.Add(person);
            await _context.SaveChangesAsync();

            var token = CreateToken(person);

            return Ok(token);
        }





        [HttpGet("User")]
        public async Task<ActionResult<PersonDto>> GetPerson()
        {

            string userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        

            if (!int.TryParse(userIdClaim, out int userId))
           
            {
                return BadRequest(new { error = "Invalid user ID in the token." });
            }

            // Retrieve the person from the database
            var person = await _context.Persons.FirstOrDefaultAsync(u => u.Id == userId);
            if (person == null)
            {
                return NotFound((new { error = "Person not found." }));
            }

            var personDTO = new PersonDto
            {

                FirstName = person.FirstName,
                LastName = person.LastName,
                Email = person.Email,

            };

            return Ok(personDTO);
        }


        [HttpPost("login")]
        public async Task<ActionResult<string>> Login(LoginDto request)
        {

            var user = await _context.Persons.FirstOrDefaultAsync(u => u.Email == request.Email);
            if (user == null)
            {
                return BadRequest(new { error = "Invalid username or password." });
            }

            // Check if the password is correct
            if (!BCrypt.Net.BCrypt.Verify(request.Password, user.Password))
            {
                return BadRequest(new { error = "Invalid username or password." });
            }

            var token = CreateToken(user);

            return Ok(token);
        }

        private string CreateToken(Person person)
        {
            var claims = new List<Claim>
    {
        new Claim(ClaimTypes.Name, person.Email),
        new Claim(ClaimTypes.NameIdentifier, person.Id.ToString())
    };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(
                _configuration.GetSection("AppSettings:Token").Value));

            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256); // Use HmacSha256

            var token = new JwtSecurityToken(
                claims: claims,
                expires: DateTime.Now.AddDays(1),
                signingCredentials: creds
            );

            var jwt = new JwtSecurityTokenHandler().WriteToken(token);


            var response = new
            {
                token = jwt
            };


            return JsonSerializer.Serialize(response);
        }



        






    }
}
