// ════════════════════════════════════════════════════════════════════════
//  Services/Interfaces/IPatientEncounterService.cs
//  Patient sees ONLY approved encounters. READ-ONLY.
//  Students use IEncounterService (full CRUD + submit).
// ════════════════════════════════════════════════════════════════════════

using UL_Optometry.Models.Common;
using UL_Optometry.Models;

namespace UL_Optometry.Services.Interfaces;

public interface IPatientEncounterService
{
    /// <summary>
    /// Returns all approved encounters for the current patient,
    /// newest first. Only status = "Approved" rows are returned.
    /// </summary>
    Task<ApiResult<List<Encounter>>> GetMyEncountersAsync();

    /// <summary>Single approved encounter by ID.</summary>
    Task<ApiResult<Encounter>> GetEncounterByIdAsync(Guid encounterId);
}
