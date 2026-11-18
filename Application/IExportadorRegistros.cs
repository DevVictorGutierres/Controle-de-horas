using ControleTempo.Domain;

namespace ControleTempo.Application;

interface IExportadorRegistros
{
    void Exportar(string destino, IReadOnlyList<Registro> registros);
}
