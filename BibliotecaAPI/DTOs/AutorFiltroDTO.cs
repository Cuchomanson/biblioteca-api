namespace BibliotecaAPI.DTOs
{
    public class AutorFiltroDTO
    {
        public int Pagina { get; set; }
        public int RecordPorPagina { get; set; } = 10;
        public PaginacionDTO PaginacionDto => new(Pagina, RecordPorPagina);

        public string? Nombres { get; set; }
        public string? Apellidos { get; set; }
        public bool? TieneFoto { get; set; }
        public bool? TieneLibros { get; set; }
        public bool IncluirLibros { get; set; } = false;
        public string? TituloLibro { get; set; }


        public string? CampoOrdenar { get; set; }
        public bool OrdenAscendente { get; set; } = true;
    }
}
