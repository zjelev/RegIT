using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using Regit.Authorization;
using Regit.Data;
using Regit.Models;

namespace Regit.Controllers;

    public class ContractsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly string[] _tableHeader;

        public ContractsController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
            _tableHeader = TypeDescriptor.GetProperties(typeof(Contract))
            .Cast<PropertyDescriptor>()
            .Select(property =>
                {
                    DisplayAttribute? displayAttribute = property.Attributes.OfType<DisplayAttribute>().FirstOrDefault();
                    string? propertyName = displayAttribute?.Name ?? property.Name;
                    return (propertyName);
                })
            .ToArray();
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

        // [HttpPost]
    // public string Index(string searchString, bool notUsed) => "From [HttpPost]Index: filter on " + searchString;

    public IActionResult Download(string? searchSubject, string? department)
    {
        var isAuthorized = User.IsInRole(Constants.ManagersRole) ||
                           User.IsInRole(Constants.AdministratorsRole);

        var currentUserId = _userManager.GetUserId(User);
        if (_context.Contracts == null)
            return Problem("Entity set 'ContractContext.Contract'  is null.");

        var contracts = _context.Contracts.Select(c => c);

        if (isAuthorized)
        {
            if (!String.IsNullOrEmpty(searchSubject))
                contracts = contracts.Where(c => c.Subject!.Contains(searchSubject));

            if (!String.IsNullOrEmpty(department))
                contracts = contracts.Where(c => c.Responsible.Name == department);
        }

        using var stream = new MemoryStream();

        using ExcelPackage package = new ExcelPackage(stream);
        ExcelWorksheet ws = package.Workbook.Worksheets.Add("Sheet1");
        ws.Cells.LoadFromCollection(contracts, true);

        for (int i = 0; i < _tableHeader.Count(); i++)
            ws.Cells[1, i + 1].Value = _tableHeader[i];

        package.Save();

        string excelName = "Contracts.xlsx";

        return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", excelName);  //application/octet-stream
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
        [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (_context.Contracts == null)
                return Problem("Entity set 'ApplicationDbContext.Contract'  is null.");

            var contract = await _context.Contracts.FindAsync(id);
            if (contract != null)
                _context.Contracts.Remove(contract);

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool ContractExists(int id) =>
            (_context.Contracts?.Any(e => e.Id == id)).GetValueOrDefault();
    }