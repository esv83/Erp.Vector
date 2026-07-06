Imports System.Runtime.CompilerServices

''' <summary>
''' Mapping ClCrew (domaine) → <see cref="ClActiveCrewDtoOut"/> pour le sélecteur d'équipage.
''' Toute la logique de présentation (libellé, fenêtre, « couvre maintenant », « clôturé ») est
''' calculée ICI, côté backend — l'UI n'affiche que le résultat.
''' </summary>
Public Module ModActiveCrewMapping

    <Extension()>
    Public Function ToActiveCrewDtoOut(crew As ClCrew, at As DateTime) As ClActiveCrewDtoOut
        Dim immat = If(crew.Vehicle?.Immatriculation, String.Empty)
        Dim members = String.Join(" / ", crew.EmployeeList.Select(Function(e) e.DisplayName()))

        ' Couvre l'instant présent : début passé et (fin absente OU pas encore atteinte).
        Dim isCurrent = crew.ServiceStart <= at AndAlso (Not crew.ServiceEnd.HasValue OrElse at <= crew.ServiceEnd.Value)
        ' Clôturé : fin de service marquée, ou fenêtre de vacation déjà passée.
        Dim isClosed = crew.IsServiceEnded OrElse (crew.ServiceEnd.HasValue AndAlso crew.ServiceEnd.Value < at)

        Dim label = If(String.IsNullOrEmpty(immat), members, $"{immat} · {members}")

        Return New ClActiveCrewDtoOut With {
            .CrewId = crew.CrewId,
            .VehicleImmat = immat,
            .Members = members,
            .ServiceWindow = FormatWindow(crew.ServiceStart, crew.ServiceEnd),
            .IsCurrent = isCurrent,
            .IsClosed = isClosed,
            .DisplayLabel = label
        }
    End Function

    Private Function FormatWindow(dteStart As DateTime, dteEnd As DateTime?) As String
        Dim s = dteStart.ToString("HH:mm")
        Return If(dteEnd.HasValue, $"{s} – {dteEnd.Value.ToString("HH:mm")}", $"{s} – …")
    End Function

End Module
