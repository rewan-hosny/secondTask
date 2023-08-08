using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using secondTask.errorHandling;
using secondTask.models;
using System.Security.Claims;
using System.Text;

namespace secondTask.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DataFileController : ControllerBase
    {

        private readonly IConfiguration _configuration;
        private readonly DataContext _context;



        public DataFileController(IConfiguration configuration, DataContext context)
        {
            _configuration = configuration;
            _context = context;
        }
  
        [HttpPost("upload")]
        public async Task<IActionResult> UploadCsv()
        {
            try
            {
                string userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                // Get the user ID claim from the token

                if (userIdClaim == null || string.IsNullOrEmpty(userIdClaim))
                {
                    // The token does not contain a valid user ID claim
                    return BadRequest("Invalid user ID claim in the token.");
                }


                if (!int.TryParse(userIdClaim, out int userId))
               
                {
                    return BadRequest(new { error = "Invalid user ID in the token." });
                }
                var person = await _context.Persons.FindAsync(userId);
                if(person == null) {
                    
                    return BadRequest(new { error = "that is user is not authorized." });
                }
                if (Request.Form.Files.Count == 0)
                {
                    return BadRequest(new { error = "File not found or empty." });
                }

                IFormFile file = Request.Form.Files[0];
                

                if (file == null || file.Length == 0)
                    return BadRequest(new { error = "File not found or empty." });





                if (!file.FileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
                    return BadRequest(new { error = "Invalid file format. Only .csv files are allowed." });



                List<Dictionary<string, string>> excelData = new List<Dictionary<string, string>>();

                using (var stream = new MemoryStream())
                {
                    //Copy the file content to a memory stream
                    file.CopyTo(stream);
                    stream.Position = 0;

                    using (var reader = new StreamReader(stream, Encoding.UTF8))
                    {
                        bool isFirstLine = true;
                        List<string> headers = new List<string>();

                        while (!reader.EndOfStream)
                        {

                            var line = reader.ReadLine();
                            var values = line.Split(',');

                            if (isFirstLine)
                            {

                                headers.AddRange(values);
                                isFirstLine = false;
                            }
                            else
                            {
                                //For each data line, create a dictionary with header-value header:value
                                var data = new Dictionary<string, string>();
                                for (int i = 0; i < headers.Count; i++)
                                {
                                    data[headers[i].ToLower()] = values[i];
                                }
                                // Add the dictionary to the list of people
                                excelData.Add(data);
                            }
                        }
                    }
                }


                return Ok(excelData);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }

    }
}
