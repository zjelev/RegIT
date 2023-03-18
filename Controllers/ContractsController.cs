using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
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
    private readonly IWebHostEnvironment _env;

    public ContractsController(ApplicationDbContext context, UserManager<IdentityUser> userManager, IWebHostEnvironment env)
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
        _env = env;
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

        var departmentQuery = _context.Departments.OrderBy(c => c).Select(c => c);
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

    public IActionResult DownloadXlsx(string? searchSubject, string? department)
    {
        var isAuthorized = User.IsInRole(Constants.ManagersRole) ||
                           User.IsInRole(Constants.AdministratorsRole);

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
            return NotFound();

        var contract = await _context.Contracts
            .Include(c => c.ControlledBy)
            .Include(c => c.Responsible)
            .Include(c => c.Owner)
            .FirstOrDefaultAsync(m => m.Id == id);
        if (contract == null)
            return NotFound();

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
    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = "Administrators")]
    public async Task<IActionResult> Create([Bind(ContractsViewModel.props)] Contract contract)
    {
        var errors = ModelState
            .Where(x => x.Value.Errors.Count > 0)
            .Select(x => new { x.Key, x.Value.Errors })
            .ToArray();

        if (ModelState.IsValid)
        {
            if (contract.File != null)
            {
                if (contract.File.Length > 1024 * 16) // || !"application/pdf".Equals(contract.ContractFile.ContentType))
                {
                    var fileName = GetUniqueFileName(contract.File.FileName);
                    var uploads = Path.Combine(_env.ContentRootPath, "uploads");
                    var department = _context.Departments.Where(d => d.Id == contract.ResponsibleId).Select(n => n.Name).FirstOrDefault();
                    uploads = Path.Combine(uploads, department);
                    var filePath = Path.Combine(uploads, fileName);
                    if (!Directory.Exists(uploads))
                        Directory.CreateDirectory(uploads);
                        
                    using FileStream stream = System.IO.File.Create(filePath);
                    await contract.File.CopyToAsync(stream);
                    contract.FilePath = department + Path.DirectorySeparatorChar + fileName;
                }
                else
                {
                    using MemoryStream ms = new MemoryStream();
                    // copy the file to memory stream 
                    await contract.File.CopyToAsync(ms);
                    // set the byte array 
                    contract.FileBytes = ms.ToArray();
                }
            }
            contract.OwnerId = _userManager.GetUserId(User);

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

        ViewData["Departments"] = new SelectList(_context.Departments, "Id", "Name");
        return View(contract);
    }

    // POST: Contracts/Edit/5
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = "Administrators")]
    public async Task<IActionResult> Edit(int id, [Bind(ContractsViewModel.props)] Contract contract)
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
        ViewData["Departments"] = new SelectList(_context.Departments, "Id", "Name");
        return View(contract);
    }

    // GET: Contracts/Delete/5
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null || _context.Contracts == null)
            return NotFound();

        var contract = await _context.Contracts
            .Include(c => c.ControlledBy)
            .Include(c => c.Responsible)
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

    // [HttpPost]
    // public async Task<IActionResult> Upload(IFormFile file)
    // {
    //     if (file == null || file.Length == 0)
    //     {
    //         return BadRequest("Invalid file");
    //     }

    //     var filePath = Path.Combine(_env.ContentRootPath, "uploads", file.FileName);

    //     using (var stream = new FileStream(filePath, FileMode.Create))
    //     {
    //         await file.CopyToAsync(stream);
    //     }

    //     return Ok();
    // }

    [HttpGet]
    public IActionResult Download(string fileName)
    {
        if (string.IsNullOrEmpty(fileName))
            return BadRequest("Invalid file name");

        var filePath = _env.ContentRootPath + Path.DirectorySeparatorChar + "uploads" + Path.DirectorySeparatorChar + fileName;

        if (!System.IO.File.Exists(filePath))
            return NotFound();

        return PhysicalFile(filePath, "application/octet-stream", Path.GetFileName(fileName));
    }

    private bool ContractExists(int id) =>
        (_context.Contracts?.Any(e => e.Id == id)).GetValueOrDefault();

    private string GetUniqueFileName(string fileName)
    {
        fileName = Path.GetFileName(fileName);
        return Path.GetFileNameWithoutExtension(fileName)
                  + "_"
                  //+ Guid.NewGuid().ToString().Substring(0, 4) 
                  + DateTime.Now.ToString("yyyy-MM-dd_HH-mm")
                  + Path.GetExtension(fileName);
    }
}
