using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ConcurrencyProject.Data;
using ConcurrencyProject.Models;

namespace ConcurrencyProject.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BankAccountsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public BankAccountsController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/BankAccounts
        [HttpGet]
        public async Task<ActionResult<IEnumerable<BankAccount>>> GetBankAccounts()
        {
            return await _context.BankAccounts.ToListAsync();
        }

        // GET: api/BankAccounts/5
        [HttpGet("{id}")]
        public async Task<ActionResult<BankAccount>> GetBankAccount(int id)
        {
            var bankAccount = await _context.BankAccounts.FindAsync(id);

            if (bankAccount == null)
            {
                return NotFound();
            }

            return bankAccount;
        }

        // POST: api/BankAccounts
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<BankAccount>> PostBankAccount(BankAccount bankAccount)
        {
            _context.BankAccounts.Add(bankAccount);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetBankAccount", new { id = bankAccount.BankAccountId }, bankAccount);
        }

        [HttpPut("{triggerConcurrency}")]
        public async Task<IActionResult> PutBankAccountTriggerConcurrency(bool triggerConcurrency)
        {
            // The While loop runs until the function is successful
            // Or 3 exceptions has been thrown
            var success = false;
            var retries = 3;
            while (!success)
            {
                try
                {
                    var account = _context.BankAccounts.First();
                    
                    // Updating the same Entity twice to trigger exception
                    if (triggerConcurrency)
                    {
                        System.Diagnostics.Debug.WriteLine("Making concurrent update of accountId: " + account.BankAccountId);

                        await _context.Database.ExecuteSqlRawAsync(
                            "UPDATE dbo.BankAccounts SET Balance = @p0 " +
                            "WHERE BankAccountId = @p1",
                            account.Balance * 2, account.BankAccountId);
                    }

                    //account.Balance = account.Balance - 2;
                    account.Balance = 75;
                    await _context.SaveChangesAsync();

                    // If this is reached everything was successful
                    success = true;
                }
                catch (DbUpdateConcurrencyException e)
                {
                    // If the exception is a concurrency exception --> Try again
                    if (retries > 0)
                    {
                        // A Handle Concurrency method could be put here
                        // But in this case a simple retry will suffice
                        System.Diagnostics.Debug.WriteLine("Concurrency Exception, Retrying! Retries left: " + retries + " Current error: " + e.Message.Substring(0,30) + "...");
                        retries--;
                        Thread.Sleep(150);
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine(e);
                        throw;
                    }
                    success = false;
                }
            }

            return NoContent();
        }


        // DELETE: api/BankAccounts/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteBankAccount(int id)
        {
            var bankAccount = await _context.BankAccounts.FindAsync(id);
            if (bankAccount == null)
            {
                return NotFound();
            }

            _context.BankAccounts.Remove(bankAccount);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool BankAccountExists(int id)
        {
            return _context.BankAccounts.Any(e => e.BankAccountId == id);
        }
    }
}
