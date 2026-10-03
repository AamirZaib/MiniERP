using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiniERP.Application.DTOs;
using MiniERP.Application.Interfaces;
using MiniERP.Domain.Entities;
using System.Security.Claims;

namespace MiniERP.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class SuppliersController : ControllerBase
    {
        private readonly IUnitOfWork _unitOfWork; 
        private readonly IAuditService _auditService;

        public SuppliersController(IUnitOfWork unitOfWork, IAuditService auditService)
        {
            _unitOfWork = unitOfWork;
            _auditService = auditService;
        }
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var suppliers = await _unitOfWork.Suppliers.GetAllAsync();
            var response = suppliers.Select(s => new SupplierResponse
            {
                Id = s.Id,
                Name = s.Name,
                ContactPerson = s.ContactPerson,
                Email = s.Email,
                Phone = s.Phone
            });
            return Ok(response);
        }

        [HttpPost]
        [Authorize(Roles = "Admin,WarehouseStaff")]
        public async Task<IActionResult> Create(CreateSupplierRequest request)
        {
            var supplier = new Supplier
            {
                Name = request.Name,
                ContactPerson = request.ContactPerson,
                Email = request.Email,
                Phone = request.Phone
            };
            await _unitOfWork.Suppliers.AddAsync(supplier);
            await _unitOfWork.SaveChangesAsync();
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var userEmail = User.FindFirstValue(ClaimTypes.Email)!;

            await _auditService.LogAsync(userId, userEmail, "SupplierCreated", "Supplier", supplier.Id.ToString(),
                $"Supplier '{supplier.Name}' added");
            return Ok(new SupplierResponse
            {
                Id = supplier.Id,
                Name = supplier.Name,
                ContactPerson = supplier.ContactPerson,
                Email = supplier.Email,
                Phone = supplier.Phone
            });
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Update(int id, UpdateSupplierRequest request)
        {
            var supplier = await _unitOfWork.Suppliers.GetByIdAsync(id);
            if (supplier == null) return NotFound();

            supplier.Name = request.Name;
            supplier.ContactPerson = request.ContactPerson;
            supplier.Email = request.Email;
            supplier.Phone = request.Phone;

            _unitOfWork.Suppliers.Update(supplier);
            await _unitOfWork.SaveChangesAsync();

            return NoContent();
        }
    }
}
