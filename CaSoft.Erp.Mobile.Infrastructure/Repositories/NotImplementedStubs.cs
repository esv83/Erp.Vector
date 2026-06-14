using CaSoft.Erp.Mobile.Application;
using CaSoft.Erp.Mobile.Application.Dto;
using CaSoft.Erp.Mobile.Application.Port;
using CaSoft.Erp.Mobile.Domain;

namespace CaSoft.Erp.Mobile.Infrastructure.Repositories;

// ============================================================================
// Stubs MOB-1 : implémentations vides des ports de CaSoft.Erp.Mobile.Application.
// Objectif : valider le contrat et l'assemblage DI (build vert), avant les
// vraies implémentations (ERP in-process : MOB-3+, BD Mobile : MOB-2).
// Chaque stub sera remplacé itération par itération (cf. mobile_devplan.md).
// ============================================================================

public class JobRepositoryStub : IJobRepository
{
    // La timeline opérationnelle (GetJobTime/SaveJobTime) est purement BD Mobile :
    // déléguée au JobTimeRepository réel dès MOB-2. Le reste attend l'ERP (MOB-6+).
    private readonly IJobTimeRepository _jobTimeRepository;

    public JobRepositoryStub(IJobTimeRepository jobTimeRepository)
    {
        _jobTimeRepository = jobTimeRepository;
    }

    public ClJob GetJob(Guid gJobId) => throw new NotImplementedException("MOB-6");
    public void Save(ClJob Job) => throw new NotImplementedException("MOB-6");
    public void UpdateCommande(ClUpdateCommandeDto CommandDto) => throw new NotImplementedException("MOB-13");
    public bool IsExist(Guid jobId) => throw new NotImplementedException("MOB-6");
    // Création paresseuse : le legacy supposait la ligne timeline pré-créée par la régulation ;
    // ici elle naît au premier geste de l'ambulancier (ClUpdateTimeUseCase ne gère pas l'absence).
    public ClJobTimeData GetJobTime(Guid jobId) =>
        _jobTimeRepository.GetJobTimeData(jobId)
        ?? ClJobTimeData.GetBuilder().WithId(jobId).Build();
    public void SaveJobTime(ClJobTimeData jobTime) => _jobTimeRepository.Save(jobTime.JobId, jobTime);
    public IInvoicingRepository Invoicing => throw new NotImplementedException("MOB-13");
}

public class CrewRepositoryStub : ICrewRepository
{
    public ClCrew GetCrew(Guid gCrewID) => throw new NotImplementedException("MOB-4");
    public bool IsEmployeeInCrew(Guid gCrewID, Guid gEmployeeId) => throw new NotImplementedException("MOB-4");
    public ClLogDriverModel GetCrewDriver(Guid gVehicleID) => throw new NotImplementedException("MOB-11");
    public List<ClJobListItemModel> FetchJobList(Guid gCrewId) => throw new NotImplementedException("MOB-5");
    public List<ClJobListItemModel> FetchJobList(IReadOnlyCollection<Guid> gCrewIds) => throw new NotImplementedException("MOB-5");
    public List<ClInstructionListItemModel> FetchInstructionList(Guid gCrewId) => throw new NotImplementedException("MOB-5");
    public void Update(ClCrew crew) => throw new NotImplementedException("MOB-4");
    public void AckInstruction(int instructionId) => throw new NotImplementedException("MOB-5");
    public List<Guid> GetCrewIdList(DateOnly id) => throw new NotImplementedException("MOB-4");
}

public class LoginRepositoryStub : ILoginRepository
{
    public bool HasToken(string strName, Guid gCrewId) => throw new NotImplementedException("MOB-4");
    public ClTokenDto GetLoginToken(string strName, Guid gCrewId) => throw new NotImplementedException("MOB-4");
    public List<ClCrewModel> GetCrewList() => throw new NotImplementedException("MOB-4");
    public Guid CreateToken(Guid gCrewId) => throw new NotImplementedException("MOB-4");
}

public class SignatureRepositoryStub : ISignatureRepository
{
    public ClSignatureDto Fetch(Guid jobId) => throw new NotImplementedException("MOB-8");
    public void Insert(Guid gJobId, string strSignData) => throw new NotImplementedException("MOB-8");
    public void Update(Guid gJobId, string strSignData) => throw new NotImplementedException("MOB-8");
    public void Delete(Guid gJobId, string strSignData) => throw new NotImplementedException("MOB-8");
    public bool Exists(Guid jobId) => throw new NotImplementedException("MOB-8");
    public HashSet<Guid> ExistingFor(IEnumerable<Guid> jobIds) => throw new NotImplementedException("MOB-8");
}

public class JobTimeRepositoryStub : IJobTimeRepository
{
    public void Save(Guid gJobId, ClJobTimeData timeData) => throw new NotImplementedException("MOB-7");
    public ClJobTimeData GetJobTimeData(Guid gJobId) => throw new NotImplementedException("MOB-7");
}

public class ContactRepositoryStub : IContactRepository
{
    public void UpdateContact(ClJobBeneficiary Contact) => throw new NotImplementedException("MOB-13");
    public ClJobBeneficiary GetContact(Guid gId) => throw new NotImplementedException("MOB-13");
    public List<ClJobBeneficiary> GetContactList(string strName, string strFirstName) => throw new NotImplementedException("MOB-13");
    public List<ClJobBeneficiary> GetContactList(string FullSearchName) => throw new NotImplementedException("MOB-13");
}

public class InvoicingRepositoryStub : IInvoicingRepository
{
    public ClContractType GetContract(Guid gJobId) => throw new NotImplementedException("MOB-13");
    public List<ClContractType> GetContractList(Guid jobId) => throw new NotImplementedException("MOB-13");
    public IAttributsRepository AttributValuesRepository => throw new NotImplementedException("MOB-13");
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

public class AttributsRepositoryStub : IAttributsRepository
{
    public void Delete(Guid gJobId, ClContractAttribut attribut) => throw new NotImplementedException("MOB-13");
    public void Update(Guid gJobId, ClContractAttribut attribut) => throw new NotImplementedException("MOB-13");
    public void Insert(Guid gJobId, ClContractAttribut attribut) => throw new NotImplementedException("MOB-13");
    public ClAttributCollection GetAttributsByContract(Guid gJobId, int intContractId) => throw new NotImplementedException("MOB-13");
}

public class MissionRepositoryStub : IMissionRepositary
{
    public ClMission GetMission(Guid gMissionId) => throw new NotImplementedException("MOB-6");
}
