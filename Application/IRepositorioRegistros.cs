using ControleTempo.Domain;

namespace ControleTempo.Application;

interface IRepositorioRegistros
{
    List<Registro> Carregar();
    void Salvar(IReadOnlyList<Registro> registros);
    void Limpar();
}
