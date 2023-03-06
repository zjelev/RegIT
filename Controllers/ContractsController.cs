using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Regit.Data;
using Regit.Models;
using Regit.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using OfficeOpenXml;

namespace Regit.Controllers;

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
        ViewBag.Responsible = responsible;
        ViewBag.SearchString = searchString;
        
        var isAuthorized = User.IsInRole(Constants.ManagersRole) ||
                           User.IsInRole(Constants.AdministratorsRole);

        var currentUserId = _userManager.GetUserId(User);

        if (_context.Contracts == null)
            return Problem("Entity set 'ContractContext.Contract'  is null.");

        var departmentQuery = _context.Contracts.OrderBy(c => c.Responsible).Select(c => c.Responsible);
        var contracts = _context.Contracts.Select(c => c);

        if (isAuthorized)
        {
            if (!String.IsNullOrEmpty(searchString))
                contracts = contracts.Where(c => c.Subject!.Contains(searchString));

            if (!String.IsNullOrEmpty(responsible))
                contracts = contracts.Where(c => c.Responsible.Name == responsible);

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

    public IActionResult Download(string? searchString, string? responsible)
    {
        string[] tableHeader = "Рег.№;Подписан на;Валиден от;Предмет;Стойност лв. без ДДС;Срок;Контролиращ отдел;Отговорен отдел;Гаранция;Начин на събиране;Инф. лист;Действие;Качил;Статус".Split(";", StringSplitOptions.RemoveEmptyEntries);
        
        // query data from database
        var isAuthorized = User.IsInRole(Constants.ManagersRole) ||
                           User.IsInRole(Constants.AdministratorsRole);

        var currentUserId = _userManager.GetUserId(User);  
        if (_context.Contracts == null)
            return Problem("Entity set 'ContractContext.Contract'  is null.");

        var contracts = _context.Contracts.Select(c => c);

        if (isAuthorized)
        {
            if (!String.IsNullOrEmpty(searchString))
                contracts = contracts.Where(c => c.Subject!.Contains(searchString));

            if (!String.IsNullOrEmpty(responsible))
                contracts = contracts.Where(c => c.Responsible.Name == responsible);
        }

        using var stream = new MemoryStream();

        using ExcelPackage package = new ExcelPackage(stream);
        ExcelWorksheet ws = package.Workbook.Worksheets.Add("Sheet1");
        ws.Cells.LoadFromCollection(contracts, true);

        for (int i = 0; i < tableHeader.Length; i++)
            ws.Cells[1, i + 1].Value = tableHeader[i];

        package.Save();

        string excelName = "Contracts.xlsx";

        return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", excelName);  //application/octet-stream
    }


    // GET: Contracts/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null || _context.Contracts == null)
            return NotFound();

        var contract = await _context.Contracts.FirstOrDefaultAsync(m => m.Id == id);
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
    [Authorize(Roles = "Administrators")]
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null || _context.Contracts == null)
            return NotFound();

        var contract = await _context.Contracts.FindAsync(id);
        if (contract == null)
            return NotFound();

        return View(contract);
    }

    // POST: Contracts/Edit/5
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken, Authorize(Roles = "Administrators")]
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
    [Authorize(Roles = "Administrators")]
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null || _context.Contracts == null)
            return NotFound();

        var contract = await _context.Contracts
            .FirstOrDefaultAsync(m => m.Id == id);
        if (contract == null)
            return NotFound();

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
