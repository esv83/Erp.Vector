Imports CaSoft.Framework.Application
Imports System.Threading
Imports CaSoft.Erp.USVector.Application.Port

''' <summary>
''' L'équipage retire le motif « le patient ne peut pas signer » (il a finalement signé, ou erreur de
''' saisie). Sans effet s'il n'y en a pas ; la projection vers Order part avec l'écriture (PRJ-1).
''' Result pattern (DR-004, 22/11 : la validation quitte le contrôleur).
''' </summary>
Public Class ClWithdrawSignatureWaiverUseCase
    Implements IResultUseCaseAsync(Of Boolean)

    Private ReadOnly _missionId As Guid
    Private ReadOnly _repository As ISignatureWaiverRepository
    Private ReadOnly _unitOfWork As IUnitOfWork

    Public Sub New(missionId As Guid, repository As ISignatureWaiverRepository, unitOfWork As IUnitOfWork)
        _missionId = missionId
        _repository = repository
        _unitOfWork = unitOfWork
    End Sub

    Public Async Function HandleAsync(ct As CancellationToken) As Task(Of ClResult(Of Boolean)) Implements IResultUseCaseAsync(Of Boolean).HandleAsync
        If _missionId = Guid.Empty Then
            Return ClResult(Of Boolean).Fail(ClError.Application("Mission obligatoire."))
        End If

        Await _repository.DeleteAsync(_missionId, ct)
        Await _unitOfWork.CommitAsync(ct)
        Return ClResult(Of Boolean).Ok(True)
    End Function

End Class
