using ControleTempo.Dominio;

namespace ControleTempo.Aplicacao;

interface IExportadorRegistros
{
    void Exportar(string destino, IReadOnlyList<Registro> registros);
}
