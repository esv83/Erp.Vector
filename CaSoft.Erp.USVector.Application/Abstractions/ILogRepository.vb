Imports CaSoft.Erp.USVector.Domain

Public Interface ILogRepository
    Function GetLog(logId As Integer) As ClLogEntry
    Function GetLogsByCrew(crewId As Guid) As List(Of ClLogEntry)
    Function GetLogsByDate(dteDebut As DateOnly, dteFin As DateOnly) As List(Of ClLogEntry)
    Sub InsertLog(gCrewId As Guid, strConstat As String, dte As Date)
    Sub DeleteLog(logId As Integer)

End Interface
