''' <summary>
''' Motif pour lequel un équipage n'est pas sélectionnable par le terrain à un instant donné
''' (cf. <see cref="ClCrew.UnselectableReasonAt"/>).
''' </summary>
Public Enum EnCrewUnselectableReason
    ''' <summary>Sélectionnable.</summary>
    None = 0
    ''' <summary>Accès pas encore ouvert : prise de service dans plus de <see cref="ClCrew.EarlyAccessMinutes"/> min.</summary>
    NotYetOpen = 1
    ''' <summary>Service clôturé : fin de service déclarée ou fenêtre de vacation dépassée.</summary>
    Closed = 2
    ''' <summary>Vacation ouverte depuis plus de <see cref="ClCrew.MaxServiceDurationHours"/> h, vraisemblablement oubliée.</summary>
    Expired = 3
End Enum
