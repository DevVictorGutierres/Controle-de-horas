using ControleTempo.Dominio;

namespace ControleTempo.Aplicacao;

interface IRepositorioRegistros
{
    List<Registro> Carregar();
    void Salvar(IReadOnlyList<Registro> registros);
    void Limpar();
}
