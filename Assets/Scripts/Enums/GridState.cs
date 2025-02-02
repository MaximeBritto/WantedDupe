public enum GridState
{
    Aligned,            // Cartes alignées en ligne
    Columns,            // Cartes en colonnes
    Static,             // Cartes placées aléatoirement mais immobiles
    SlowMoving,         // Cartes en mouvement lent
    FastMoving,         // Cartes en mouvement rapide
    AlignedMoving,      // Cartes alignées qui se déplacent
    ColumnsMoving,      // Colonnes qui se déplacent
    CircularAligned,    // Cartes disposées en cercles concentriques
    CircularMoving      // Cartes en cercles qui tournent
} 