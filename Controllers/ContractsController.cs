using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Regit.Authorization;
using Regit.Data;
using Regit.Models;

namespace Regit.Controllers
{
    public class ContractsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public ContractsController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: Contracts
        public async Task<IActionResult> Index(string searchSubject, string selectDepartment)
        {
            // var applicationDbContext = _context.Contracts.Include(c => c.ControlledBy).Include(c => c.Responsible);
            // return View(await applicationDbContext.ToListAsync());
            Console.WriteLine("### Remote: " + HttpContext.Connection.RemoteIpAddress.ToString());
            Console.WriteLine("### User: " + HttpContext.User.Identity.Name);

            ViewData["Department"] = selectDepartment;
            ViewData["SearchSubject"] = searchSubject;

            var isAuthorized = User.IsInRole(Constants.ManagersRole) ||
                               User.IsInRole(Constants.AdministratorsRole);

            var currentUserId = _userManager.GetUserId(User);

            if (_context.Contracts == null)
                return Problem("Entity set 'ContractContext.Contract'  is null.");

            var departmentQuery = _context.Contracts.OrderBy(c => c.Responsible).Select(c => c.Responsible);
            var contracts = _context.Contracts.Select(c => c);

            if (isAuthorized)
            {
                if (!String.IsNullOrEmpty(searchSubject))
                    contracts = contracts.Where(c => c.Subject!.Contains(searchSubject));

                if (!String.IsNullOrEmpty(selectDepartment))
                    contracts = contracts.Where(c => c.Responsible.Name == selectDepartment);

                var model = new ContractsViewModel
                {
                    Departments = await departmentQuery.Distinct().ToListAsync(),
                    Contracts = await contracts.ToListAsync()
                };
                return View(model);
            }
            else
                return Forbid();
        }

        // GET: Contracts/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null || _context.Contracts == null)
            {
                return NotFound();
            }

            var contract = await _context.Contracts
                .Include(c => c.ControlledBy)
                .Include(c => c.Responsible)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (contract == null)
            {
                return NotFound();
            }

            return View(contract);
        }

        // GET: Contracts/Create
        public IActionResult Create()
        {
            ViewData["Departments"] = new SelectList(_context.Departments, "Id", "Name");
            return View();
        }

        // POST: Contracts/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,SignedOn,ValidFrom,RegNum,Subject,Value,Term,ControlledById,ResponsibleId,Guarantee,WaysOfCollection,InformationList,OwnerID,Status,FilePath")] Contract contract)
        {
            var errors = ModelState
                .Where(x => x.Value.Errors.Count > 0)
                .Select(x => new { x.Key, x.Value.Errors })
                .ToArray();

            if (ModelState.IsValid)
            {
                _context.Add(contract);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(contract);
        }

        // GET: Contracts/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null || _context.Contracts == null)
            {
                return NotFound();
            }

            var contract = await _context.Contracts.FindAsync(id);
            if (contract == null)
            {
                return NotFound();
            }
            ViewData["Departments"] = new SelectList(_context.Departments, "Id", "Name");
            return View(contract);
        }

        // POST: Contracts/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,SignedOn,ValidFrom,RegNum,Subject,Value,Term,ControlledById,ResponsibleId,Guarantee,WaysOfCollection,InformationList,OwnerID,Status,FilePath")] Contract contract)
        {
            if (id != contract.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(contract);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ContractExists(contract.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["Departments"] = new SelectList(_context.Departments, "Id", "Name");
            return View(contract);
        }

        // GET: Contracts/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null || _context.Contracts == null)
            {
                return NotFound();
            }

            var contract = await _context.Contracts
                .Include(c => c.ControlledBy)
                .Include(c => c.Responsible)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (contract == null)
            {
                return NotFound();
            }

            return View(contract);
        }

        // POST: Contracts/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (_context.Contracts == null)
            {
                return Problem("Entity set 'ApplicationDbContext.Contracts'  is null.");
            }
            var contract = await _context.Contracts.FindAsync(id);
            if (contract != null)
            {
                _context.Contracts.Remove(contract);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool ContractExists(int id)
        {
            return (_context.Contracts?.Any(e => e.Id == id)).GetValueOrDefault();
        }
    }
}
