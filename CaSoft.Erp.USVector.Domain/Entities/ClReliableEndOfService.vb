Imports CaSoft.Framework

Public Class ClReliableEndOfService
    Inherits ClReliableValue(Of DateTime)

    Public Overrides Sub AnalyzeInfo()

        SetValue(Nothing, ValueReliableLevel.Null)

        If Contains("Autority") Then

            'Si on a une saisie a la main TimeByAutority
            Dim timeByAutority = ValueInfos.First(Function(f) f.Value = "Autority")

            MyBase.SetValue(timeByAutority.Value, ValueReliableLevel.Max)

        ElseIf Contains("endOfSceNotifyByRegul") Then

            If Contains("VehicleOnSite") Then
                Dim vehicleOnSite = ValueInfos.First(Function(f) f.Value = "VehicleOnSite")

                If Contains("MovementOnSite") Then
                    Dim movements = ValueInfos.Where(Function(f) f.Attribut = "movementOnSite").ToList
                    For Each Mov In movements.OrderBy(Of DateTime)(Function(f) f.Value)

                        Dim duration = Mov.Value.Subtract(vehicleOnSite.Value)
                        If duration.TotalMinutes < 2 Then
                            SetValue(Mov.Value, ValueReliableLevel.Max)
                        End If

                    Next

                Else
                    MyBase.SetValue(vehicleOnSite.Value, ValueReliableLevel.Approx)

                End If

            End If

        End If

    End Sub

End Class
