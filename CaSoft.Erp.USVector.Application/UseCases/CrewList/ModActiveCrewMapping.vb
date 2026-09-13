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

        ' Couvre l'instant présent : service commencé et vacation non clôturée. La fin de service n'entre
        ' pas en compte : depuis le 13/09/2026 elle peut être théorique (début + 10 h) et dépassée par un
        ' équipage encore en route — il doit rester pré-sélectionné.
        Dim isCurrent = crew.ServiceStart <= at AndAlso Not crew.IsServiceEnded
        ' Accès anticipé : proposé avant la prise de service (fenêtre ClCrew.EarlyAccessMinutes).
        Dim isPending = at < crew.ServiceStart
        ' Clôturé : vacation clôturée chez Orders (statut), et rien d'autre.
        Dim isClosed = crew.IsServiceEnded

        Dim label = If(String.IsNullOrEmpty(immat), members, $"{immat} · {members}")

        Return New ClActiveCrewDtoOut With {
            .CrewId = crew.CrewId,
            .VehicleImmat = immat,
            .Members = members,
            .ServiceWindow = FormatWindow(crew.ServiceStart, crew.ServiceEnd),
            .IsCurrent = isCurrent,
            .IsPending = isPending,
            .IsClosed = isClosed,
            .DisplayLabel = label
        }
    End Function

    Private Function FormatWindow(dteStart As DateTime, dteEnd As DateTime?) As String
        Dim s = dteStart.ToString("HH:mm")
        Return If(dteEnd.HasValue, $"{s} – {dteEnd.Value.ToString("HH:mm")}", $"{s} – …")
    End Function

End Module
