using System.Text.Json;
using ControleTempo.Aplicacao;
using ControleTempo.Dominio;

namespace ControleTempo.Infraestrutura;

sealed class RepositorioJson(string caminho) : IRepositorioRegistros
{
    string Temporario => caminho + ".tmp";

    public List<Registro> Carregar()
    {
        try
        {
            return JsonSerializer.Deserialize<List<Registro>>(File.ReadAllText(caminho))
                ?? throw new InvalidDataException("Histórico vazio ou inválido.");
        }
        catch (FileNotFoundException) { return []; }
        catch (DirectoryNotFoundException) { return []; }
        catch (JsonException ex)
        {
            throw new InvalidDataException("Histórico local inválido.", ex);
        }
    }

    public void Salvar(IReadOnlyList<Registro> registros)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(caminho)!);
        File.WriteAllText(Temporario, JsonSerializer.Serialize(registros));
        File.Move(Temporario, caminho, overwrite: true);
    }

    public void Limpar()
    {
        try
        {
            File.Delete(Temporario);
            File.Delete(caminho);
        }
        catch (DirectoryNotFoundException) { }
    }
}
