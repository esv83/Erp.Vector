Public Class ClJobTimeModel
    'ATTENTION les heures arrivent en string et en UTC (Zulu) 

    Public Property OnSiteTime As String
    Public Property TerminatedTime As String
    Public Property GoTime As String
    'Public Shared Function GetBuilder() As ClJobTimeModelBuilder
    '    Return ClJobTimeModelBuilder.GetBuilder
    'End Function

    'Public Class ClJobTimeModelBuilder
    '    Private _result As ClUpdateJobTimeModel
    '    Private Sub New()
    '        _result = New ClUpdateJobTimeModel
    '    End Sub

    '    Public Function WithGoTime(dte As DateTime?) As ClJobTimeModelBuilder
    '        _result.GoTime = dte
    '        Return Me

    '    End Function
    '    Public Function WithOnSiteTime(dte As DateTime?) As ClJobTimeModelBuilder
    '        _result.OnSiteTime = dte

    '        Return Me

    '    End Function
    '    Public Function WithTerminatedTime(dte As DateTime?) As ClJobTimeModelBuilder
    '        _result.TerminatedTime = dte

    '        Return Me

    '    End Function

    '    Friend Shared Function GetBuilder() As ClJobTimeModelBuilder
    '        Return New ClJobTimeModelBuilder
    '    End Function

    '    Public Function Build() As ClUpdateJobTimeModel
    '        Return _result
    '    End Function
    'End Class

End Class
