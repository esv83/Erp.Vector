Public Class ClHelper
    Public Shared Function GetScheduleString(dteSchedule As DateTime, bIsNow As Boolean?, intDelai As Integer?, dteCallTime As DateTime?) As String
        Dim result As String = String.Empty


        If bIsNow Then

            If intDelai.HasValue Then
                If dteCallTime.HasValue Then
                    result = String.Format("avant {0} ({1} mn)", dteCallTime.Value.AddMinutes(intDelai).ToShortTimeString, intDelai.ToString)
                End If

            Else
                result = "ASAP" 'String.Format("avant {0} ({1} mn)", .MI_APPEL.Value.AddMinutes(.CDE_DELAI).ToShortTimeString, .CDE_DELAI.ToString)

            End If
        Else
            If dteSchedule.ToShortDateString = Date.Now.ToShortDateString Then
                result = dteSchedule.ToShortTimeString
            Else
                result = dteSchedule.ToString()
            End If

        End If

        Return result
    End Function
    Public Shared Function GetAppointmentString(dteAppointment As DateTime?) As String
        Dim result As String = String.Empty

        If dteAppointment.HasValue Then

            result = dteAppointment.Value.ToShortTimeString

        End If

        Return result

    End Function
    Public Shared Function GetPatientString(strName As String, strSurname As String, Optional dteBirtday As Date? = Nothing) As String
        Dim result As String

        If dteBirtday.HasValue Then
            result = String.Format("{0} {1} {2}", strName, strSurname, GetAgeString(dteBirtday))
        Else
            result = String.Format("{0} {1}", strName, strSurname)
        End If

        Return result

    End Function
    Public Shared Function GetPatientWithBirthdayString(strName As String, strSurname As String, dteBirtday As Date?) As String
        Dim result As String


        If dteBirtday.HasValue Then
            result = String.Format("{0} {1} ({2})", strName, strSurname, dteBirtday.Value.ToShortDateString)
        Else
            result = String.Format("{0} {1}", strName, strSurname)
        End If



        Return result

    End Function
    Public Shared Function GetAgeString(dteBirthdayDate As Date) As String
        Dim result = String.Empty

        If dteBirthdayDate.AddMonths(1) > Date.Now Then
            Dim ageTimeSpan = Now.Subtract(dteBirthdayDate)
            result = String.Format("{0} jours", ageTimeSpan.Days.ToString)
        ElseIf dteBirthdayDate.AddYears(1) > Date.Now Then
            result = String.Format("{0} mois", (Date.Now.Month - dteBirthdayDate.Month).ToString)
        Else
            result = String.Format("{0} ans", (Date.Now.Year - dteBirthdayDate.Year).ToString)
        End If

        Return result

    End Function
    Public Shared Function GetSiteString(strSite As String, strSce As String, strCommune As String) As String
        Dim sb As New Text.StringBuilder()
        sb.AppendLine(strSite)
        If Not String.IsNullOrWhiteSpace(strSce) Then
            sb.AppendLine(strSce)
        End If
        If Not strSite.Contains(strCommune) Then
            sb.AppendLine(strCommune)
        End If

        Return sb.ToString

    End Function
    Public Shared Function GetSiteString(strSite As String, strSce As String, strLine1 As String, strLine2 As String, strLine3 As String, strCp As String, strCommune As String) As String
        Dim sb As New Text.StringBuilder()
        sb.AppendLine(strSite)
        If Not String.IsNullOrWhiteSpace(strSce) Then
            sb.AppendLine(strSce)
        End If
        If Not String.IsNullOrWhiteSpace(strLine1) Then
            sb.AppendLine(strLine1)
        End If
        If Not String.IsNullOrWhiteSpace(strLine2) Then
            sb.AppendLine(strLine2)
        End If
        If Not String.IsNullOrWhiteSpace(strLine3) Then
            sb.AppendLine(strLine3)
        End If

        If Not String.IsNullOrWhiteSpace(strCp) Then
            sb.Append(strCp + " ")
        End If

        If Not String.IsNullOrWhiteSpace(strCommune) Then
            sb.AppendLine(strCommune)
        End If

        Return sb.ToString

    End Function
    Public Shared Function GetCommentsString(strTripComment As String, strOrderComment As String) As String
        Dim sb As New Text.StringBuilder()
        If Not String.IsNullOrWhiteSpace(strTripComment) Then
            sb.AppendLine(strTripComment)
        End If
        If Not String.IsNullOrWhiteSpace(strOrderComment) Then
            sb.AppendLine(strOrderComment)
        End If

        Return sb.ToString

    End Function


End Class
