using ControleTempo.Dominio;

namespace ControleTempo.Aplicacao;

sealed class ControleRegistros(IRepositorioRegistros repositorio, IExportadorRegistros exportador, TimeProvider horario)
{
    List<Registro> registros = [];

    public void Carregar() => registros = repositorio.Carregar();

    public bool Registrar(string cliente, string atividade, string descricao, TimeSpan tempo)
    {
        if (string.IsNullOrWhiteSpace(cliente) || string.IsNullOrWhiteSpace(atividade))
            return false;

        var data = DateOnly.FromDateTime(horario.GetLocalNow().DateTime);
        var registro = new Registro(data, cliente.Trim(), atividade.Trim(), descricao.Trim(), tempo);
        List<Registro> atualizados = [.. registros, registro];
        repositorio.Salvar(atualizados);
        registros = atualizados;
        return true;
    }

    public void Limpar()
    {
        repositorio.Limpar();
        registros.Clear();
    }

    public int Contar(DateOnly? data) =>
        data is null ? registros.Count : registros.Count(registro => registro.Data == data);

    public void Exportar(string destino, DateOnly? data)
    {
        IReadOnlyList<Registro> selecionados = data is null
            ? registros
            : registros.Where(registro => registro.Data == data).ToArray();
        exportador.Exportar(destino, selecionados);
    }
}
