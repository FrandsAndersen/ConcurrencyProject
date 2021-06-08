using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ConcurrencyProject.Data;
using ConcurrencyProject.Models;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace ConcurrencyProject.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly AppDbContext _context;

        public UsersController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/Users
        [HttpGet]
        public async Task<ActionResult<IEnumerable<User>>> GetUsers()
        {
            return await _context.Users.ToListAsync();
        }

        // GET: api/Users/5
        [HttpGet("{id}")]
        public async Task<ActionResult<User>> GetUser(int id)
        {
            var user = await _context.Users.FindAsync(id);

            if (user == null)
            {
                return NotFound();
            }

            return user;
        }

        // PUT: api/Users/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{handleConcurrency}")]
        public async Task<IActionResult> PutUser(bool handleConcurrency)
        {
            try
            {
                var user = _context.Users.First();

                System.Diagnostics.Debug.WriteLine("Making concurrent update of userId: " + user.UserId);

                await _context.Database.ExecuteSqlRawAsync(
                    "UPDATE dbo.Users SET Age = @p0 " +
                    "WHERE UserId = @p1",
                    user.Age + 1, user.UserId);

                user.Height++;
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException e)
            {
                if (handleConcurrency)
                {
                    var errorEntry = e.Entries.Single();
                    var error = HandleConcurrency(errorEntry);
                    if (error == null)
                    {
                        System.Diagnostics.Debug.WriteLine("Concurrency handled successfully saving");
                        await _context.SaveChangesAsync();
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("Error in handling concurrency: " + error);
                        throw;
                    }
                }
            }

            return NoContent();
        }

        private string HandleConcurrency(EntityEntry errorEntry)
        {
            var desiredUserData = errorEntry.Entity as User;

            var currentUserDataInDb = _context.Set<User>().AsNoTracking()
                .SingleOrDefault(u => u.UserId == desiredUserData.UserId);

            if (currentUserDataInDb == null)
            {
                System.Diagnostics.Debug.WriteLine("The user was deleted!");
                return "The user was deleted";
            }

            var currentUserDataInDbAsEntry = _context.Entry(currentUserDataInDb);

            foreach (var property in errorEntry.Metadata.GetProperties())
            {
                var originalValue = errorEntry.Property(property.Name).OriginalValue;
                var currentValue = currentUserDataInDbAsEntry.Property(property.Name).CurrentValue;
                var desiredValue = errorEntry.Property(property.Name).CurrentValue;

                if (property.Name == nameof(Models.User.Name))
                {
                    // Select the name different from original value
                    if (originalValue == currentValue)
                    {
                        errorEntry.Property(property.Name).OriginalValue = desiredValue;
                    }
                    else
                    {
                        errorEntry.Property(property.Name).OriginalValue = currentValue;
                    }
                } 
                else if (property.Name == nameof(Models.User.Age))
                {
                    // Select highest number (You only get older)

                    // DEMO ONLY
                    errorEntry.Property(property.Name).OriginalValue = currentValue;

                }
                else if (property.Name == nameof(Models.User.Height))
                {
                    // Select highest value (You "usually" only get higher)

                    // DEMO ONLY
                    errorEntry.Property(property.Name).OriginalValue = desiredValue;
                }
                else if (property.Name == nameof(Models.User.ChangeCheck))
                {
                    errorEntry.Property(property.Name).OriginalValue = currentValue;
                }
            }
            return null;
        }

        // POST: api/Users
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<User>> PostUser(User user)
        {
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetUser", new { id = user.UserId }, user);
        }

        // DELETE: api/Users/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool UserExists(int id)
        {
            return _context.Users.Any(e => e.UserId == id);
        }
    }
}
