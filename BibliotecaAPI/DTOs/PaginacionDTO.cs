namespace BibliotecaAPI.DTOs
{
    public record PaginacionDTO(int Pagina = 1, int RecordsPorPagina = 10)
    {
        private const int CantidadMaximaRecordsPorPagina = 50;

        public int Pagina { get; init; } = Math.Max(1, Pagina);

        // El número de registros por página se limita a un máximo para evitar solicitudes excesivas.
        public int RecordsPorPagina { get; init; } = Math.Clamp(RecordsPorPagina, 1, CantidadMaximaRecordsPorPagina);   
    }
}
