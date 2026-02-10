namespace ZeltekWeb.Models
{
    /// <summary>
    /// Formulario de inspección de calidad para un ítem (Por Revisar → En Stock o En Reparación).
    /// </summary>
    public class InspeccionCalidadViewModel
    {
        public int ItemID { get; set; }
        public string ProductoNombre { get; set; } = "";
        public string NumeroSerie { get; set; } = "";

        // Cosmética y batería (se guardan en ItemInventario)
        public string? Condicion { get; set; }   // Grado A, B, C
        public int? SaludBateriaPorcentaje { get; set; }

        // Checklist (opcional guardar en notas)
        public bool EncendioCorrectamente { get; set; }
        public bool PantallaSinManchas { get; set; }
        public bool TecladoFuncional { get; set; }
        public bool CargadorIncluido { get; set; }
        public bool LimpiezaRealizada { get; set; }
        public bool PantallaOk { get; set; }
        public bool TecladoOk { get; set; }
        public bool AudioOk { get; set; }

        public string? NotasInspeccion { get; set; }
    }
}
