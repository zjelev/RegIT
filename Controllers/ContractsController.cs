using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Contracts.Data;
using Contracts.Models;
using Contracts.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;

namespace Contracts.Controllers
{
    public class ContractsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IAuthorizationService _authorizationService;

        public ContractsController(ApplicationDbContext context, UserManager<IdentityUser> userManager, IAuthorizationService authorizationService)
        {
            _context = context;
            _userManager = userManager;
            _authorizationService = authorizationService;
        }

        // GET: Contracts
        public async Task<IActionResult> Index(string searchString, string responsible)
        {
            var isAuthorized = User.IsInRole(Constants.ManagersRole) ||
                               User.IsInRole(Constants.AdministratorsRole);

            var currentUserId = _userManager.GetUserId(User);

            if (_context.Contract == null)
                return Problem("Entity set 'ContractContext.Contract'  is null.");

            var departmentQuery = _context.Contract.OrderBy(c => c.Responsible).Select(c => c.Responsible);
            var contracts = _context.Contract.Select(c => c);

            if (isAuthorized)
            {
                if (!String.IsNullOrEmpty(searchString))
                    contracts = contracts.Where(c => c.Subject!.Contains(searchString));

                if (!String.IsNullOrEmpty(responsible))
                    contracts = contracts.Where(c => c.Responsible == responsible);

                var departmentVM = new ResponsibleViewModel
                {
                    Responsibles = new SelectList(await departmentQuery.Distinct().ToListAsync()),
                    Contracts = await contracts.ToListAsync()
                };
                return View(departmentVM);
            }
            else
                return Forbid();
        }

        // [HttpPost]
        // public string Index(string searchString, bool notUsed) => "From [HttpPost]Index: filter on " + searchString;


        // GET: Contracts/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null || _context.Contract == null)
                return NotFound();

            var contract = await _context.Contract.FirstOrDefaultAsync(m => m.Id == id);
            if (contract == null)
                return NotFound();

            var isAuthorized = User.IsInRole(Constants.ManagersRole) || User.IsInRole(Constants.AdministratorsRole);

            var currentUserId = _userManager.GetUserId(User);

            if (!isAuthorized && currentUserId != contract.OwnerID && contract.Status != ContractStatus.Approved)
                return Forbid();

            return View(contract);
        }

        // GET: Contracts/Create
        public IActionResult Create() => View();

        // POST: Contracts/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,SignedOn,Title,ValidFrom,RegNum,Subject,Value,Term,ControlledBy,Responsible,Guarantee,WaysOfCollection,InformationList")] Contract contract)
        {
            if (ModelState.IsValid)
            {
                contract.OwnerID = _userManager.GetUserId(User);
                var isAuthorized = await _authorizationService.AuthorizeAsync(User, contract, Operations.Create);

                if (!isAuthorized.Succeeded)
                    return Forbid();

                _context.Add(contract);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(contract);
        }

        // GET: Contracts/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null || _context.Contract == null)
                return NotFound();

            var contract = await _context.Contract.FindAsync(id);
            if (contract == null)
                return NotFound();

            return View(contract);
        }

        // POST: Contracts/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,SignedOn,Title,ValidFrom,RegNum,Subject,Value,Term,ControlledBy,Responsible,Guarantee,WaysOfCollection,InformationList")] Contract contract)
        {
            if (id != contract.Id)
                return NotFound();

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
                        return NotFound();
                    else
                        throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(contract);
        }

        // GET: Contracts/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null || _context.Contract == null)
                return NotFound();

            var contract = await _context.Contract
                .FirstOrDefaultAsync(m => m.Id == id);
            if (contract == null)
                return NotFound();

            return View(contract);
        }

        // POST: Contracts/Delete/5
        [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (_context.Contract == null)
                return Problem("Entity set 'ApplicationDbContext.Contract'  is null.");

            var contract = await _context.Contract.FindAsync(id);
            if (contract != null)
                _context.Contract.Remove(contract);

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool ContractExists(int id) =>
            (_context.Contract?.Any(e => e.Id == id)).GetValueOrDefault();
    }
}
