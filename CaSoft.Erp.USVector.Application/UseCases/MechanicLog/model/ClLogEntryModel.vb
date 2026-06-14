Public Class ClLogEntryModel

    Public Property LogId As Integer
    Public Property LogDate As DateTime
    Public Property Report As String
    Public Property ReportState As String
    Public Property State As LogStateEnum
    Public Property LastStateDate As DateTime?
    Public Property Immatriculation As String
    Public Property Crew As String
    Public Property Analyse As String
    Public Property Action As String
    Public Property NextDeadLine As Date?
    Public Enum LogStateEnum
        [New] = 1
        Read = 2
        InProgress = 3
        Closed = 4
    End Enum

End Class
