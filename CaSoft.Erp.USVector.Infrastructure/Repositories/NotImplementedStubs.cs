using CaSoft.Erp.USVector.Application;
using CaSoft.Erp.USVector.Application.Port;
using CaSoft.Erp.USVector.Domain;

namespace CaSoft.Erp.USVector.Infrastructure.Repositories;

// ============================================================================
// Stubs MOB-1 : implémentations vides des ports de CaSoft.Erp.USVector.Application,
// posées pour valider le contrat et l'assemblage DI avant les vraies implémentations.
//
// OC-9 — Il n'en reste que trois, et pour une raison précise : ce sont les seuls
// encore enregistrés dans Program.cs, parce que des routes les injectent encore
// (ContactController, MecanicLogController, AnalyzeLogController). Ces routes
// répondent donc 500 aujourd'hui — elles le faisaient déjà, ce n'est pas une
// régression, mais c'est ce qui empêche de supprimer ce fichier.
//
// Les retirer suppose de trancher le sort de ces routes (les implémenter ou les
// retirer du contrat mobile, ce qui relève de D14) — pas de ce lot.
// ============================================================================

public class ContactRepositoryStub : IContactRepository
{
    public void UpdateContact(ClJobBeneficiary Contact) => throw new NotImplementedException("MOB-13");
    public ClJobBeneficiary GetContact(Guid gId) => throw new NotImplementedException("MOB-13");
    public List<ClJobBeneficiary> GetContactList(string strName, string strFirstName) => throw new NotImplementedException("MOB-13");
    public List<ClJobBeneficiary> GetContactList(string FullSearchName) => throw new NotImplementedException("MOB-13");
}

public class LogRepositoryStub : ILogRepository
{
    public ClLogEntry GetLog(int logId) => throw new NotImplementedException("MOB-14");
    public List<ClLogEntry> GetLogsByCrew(Guid crewId) => throw new NotImplementedException("MOB-14");
    public List<ClLogEntry> GetLogsByDate(DateOnly dteDebut, DateOnly dteFin) => throw new NotImplementedException("MOB-14");
    public void InsertLog(Guid gCrewId, string strConstat, DateTime dte) => throw new NotImplementedException("MOB-14");
    public void DeleteLog(int logId) => throw new NotImplementedException("MOB-14");
}

public class LogAnalyzeRepositoryStub : ILogAnalyzeRepository
{
    public ClLogAnalyze GetAnalyze(int intLogId) => throw new NotImplementedException("MOB-14");
    public void SaveAnalyze(ClLogAnalyze analyze) => throw new NotImplementedException("MOB-14");
}
