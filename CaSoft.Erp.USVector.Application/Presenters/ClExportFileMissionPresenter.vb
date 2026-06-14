Imports System.ComponentModel
Imports System.IO
Imports System.Text

Public Class ClExportFileMissionPresenter
    Public Shared Function ConvertToCsv(Of T)(data As IList(Of T), fileName As String, Optional sign As String = Nothing) As Boolean

        Dim result As Boolean = True
        Dim sbData As New StringBuilder()

        Try
            If Not String.IsNullOrEmpty(sign) Then
                sbData.Append(sign & System.Environment.NewLine)
            End If

            Dim properties As PropertyDescriptorCollection = TypeDescriptor.GetProperties(GetType(T))

            For Each item As T In data

                '  Dim row As DataRow = table.NewRow()
                For Each prop As PropertyDescriptor In properties
                    If prop.Name.StartsWith("C") Then

                        Dim ColumnValue = If(prop.GetValue(item), DBNull.Value)
                        If ColumnValue Is Nothing Or 0 Then
                            sbData.Append(""""";")
                        ElseIf ColumnValue.GetType() Is GetType(System.DateTime) Then
                            sbData.Append(CType(ColumnValue, DateTime).ToString("yyyy-MM-dd HH:mm:ss.fff") & ";")
                        Else
                            sbData.Append("""" & ColumnValue.ToString().Replace("""", """""") & """;")
                        End If

                    End If
                Next

                sbData.Replace(";", System.Environment.NewLine, sbData.Length - 1, 1)

            Next

            File.WriteAllText(fileName, sbData.ToString())
        Catch ex As Exception
            result = False
        End Try

        Return result


    End Function

End Class
