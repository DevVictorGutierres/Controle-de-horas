using ControleTempo.Aplicacao;
using ControleTempo.Apresentacao;
using ControleTempo.Infraestrutura;

namespace ControleTempo;

static class Program
{
    [STAThread]
    static void Main()
    {
        using var instance = new Mutex(true, @"Local\ControleTempoDev", out bool first);
        if (!first)
        {
            MessageBox.Show("O Controle de Tempo já está aberto.");
            return;
        }

        ApplicationConfiguration.Initialize();
        string caminho = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ControleTempoDev", "registros.json");
        var controle = new ControleRegistros(new RepositorioJson(caminho), new ExportadorExcel(), TimeProvider.System);
        Application.Run(new Tela(controle));
    }
}
