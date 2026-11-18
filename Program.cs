using ControleTempo.Application;
using ControleTempo.Presentation;
using ControleTempo.Infrastructure;

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
        System.Windows.Forms.Application.Run(new Tela(controle));
    }
}
