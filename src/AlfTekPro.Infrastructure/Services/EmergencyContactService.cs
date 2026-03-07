using Microsoft.EntityFrameworkCore;
using AlfTekPro.Application.Features.EmergencyContacts.DTOs;
using AlfTekPro.Application.Features.EmergencyContacts.Interfaces;
using AlfTekPro.Domain.Entities.CoreHR;
using AlfTekPro.Infrastructure.Data.Contexts;

namespace AlfTekPro.Infrastructure.Services;

public class EmergencyContactService : IEmergencyContactService
{
    private readonly HrmsDbContext _context;

    public EmergencyContactService(HrmsDbContext context)
    {
        _context = context;
    }

    public async Task<List<EmergencyContactResponse>> GetByEmployeeAsync(Guid employeeId, CancellationToken ct = default)
    {
        return await _context.EmergencyContacts
            .AsNoTracking()
            .Where(e => e.EmployeeId == employeeId)
            .OrderByDescending(e => e.IsPrimary)
            .ThenBy(e => e.Name)
            .Select(e => Map(e))
            .ToListAsync(ct);
    }

    public async Task<EmergencyContactResponse?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var contact = await _context.EmergencyContacts.AsNoTracking().FirstOrDefaultAsync(e => e.Id == id, ct);
        return contact is null ? null : Map(contact);
    }

    public async Task<EmergencyContactResponse> CreateAsync(Guid employeeId, EmergencyContactRequest request, CancellationToken ct = default)
    {
        if (request.IsPrimary)
            await DemotePrimaryAsync(employeeId, ct);

        var contact = new EmergencyContact
        {
            EmployeeId     = employeeId,
            Name           = request.Name,
            Relationship   = request.Relationship,
            PhoneNumber    = request.PhoneNumber,
            AlternatePhone = request.AlternatePhone,
            Email          = request.Email,
            Address        = request.Address,
            IsPrimary      = request.IsPrimary
        };

        _context.EmergencyContacts.Add(contact);
        await _context.SaveChangesAsync(ct);
        return Map(contact);
    }

    public async Task<EmergencyContactResponse?> UpdateAsync(Guid id, EmergencyContactRequest request, CancellationToken ct = default)
    {
        var contact = await _context.EmergencyContacts.FindAsync(new object[] { id }, ct);
        if (contact is null) return null;

        if (request.IsPrimary && !contact.IsPrimary)
            await DemotePrimaryAsync(contact.EmployeeId, ct);

        contact.Name           = request.Name;
        contact.Relationship   = request.Relationship;
        contact.PhoneNumber    = request.PhoneNumber;
        contact.AlternatePhone = request.AlternatePhone;
        contact.Email          = request.Email;
        contact.Address        = request.Address;
        contact.IsPrimary      = request.IsPrimary;

        await _context.SaveChangesAsync(ct);
        return Map(contact);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var contact = await _context.EmergencyContacts.FindAsync(new object[] { id }, ct);
        if (contact is null) return false;
        _context.EmergencyContacts.Remove(contact);
        await _context.SaveChangesAsync(ct);
        return true;
    }

    private async Task DemotePrimaryAsync(Guid employeeId, CancellationToken ct)
    {
        var existing = await _context.EmergencyContacts
            .Where(e => e.EmployeeId == employeeId && e.IsPrimary).ToListAsync(ct);
        foreach (var e in existing) e.IsPrimary = false;
    }

    private static EmergencyContactResponse Map(EmergencyContact e) => new()
    {
        Id             = e.Id,
        EmployeeId     = e.EmployeeId,
        Name           = e.Name,
        Relationship   = e.Relationship,
        PhoneNumber    = e.PhoneNumber,
        AlternatePhone = e.AlternatePhone,
        Email          = e.Email,
        Address        = e.Address,
        IsPrimary      = e.IsPrimary,
        CreatedAt      = e.CreatedAt
    };
}
