# Controle de Tempo

Aplicativo Windows Forms para registrar tempo de desenvolvimento e exportar os registros em Excel. Para executar a partir do código-fonte, instale o SDK do .NET 10 no Windows e rode `dotnet run` na raiz do repositório.

1. Clique em Iniciar. Use Pausar e Retomar para interromper e continuar a contagem.
2. Preencha Cliente e Atividade antes de salvar. A Descrição é opcional. Os campos ficam editáveis enquanto o relógio corre ou está pausado.
3. Clique em Parar e salvar. O registro recebe a data local do momento em que foi salvo.
4. Escolha uma data ou marque Exportar todo o histórico. Depois clique em Gerar Excel. Você escolhe a pasta e o nome da planilha.
5. Limpar histórico local pede confirmação e apaga os registros e a edição atual. Planilhas exportadas continuam disponíveis.

O histórico fica em %LOCALAPPDATA%/ControleTempoDev/registros.json. Fechar o aplicativo com um registro em edição pede confirmação, pois o rascunho e o cronômetro são mantidos em memória até salvar.

Para editar no Visual Studio, abra `ControleTempo.csproj` com o suporte a desenvolvimento para desktop com .NET.


## Gerar o executável (.exe)

No Windows, com o SDK do .NET 10 instalado, execute este comando na raiz do repositório:

```bash
dotnet publish ControleTempo.csproj -c Release -o ./bin/publicacao
```

Abra `bin/publicacao/ControleTempo.exe` para iniciar o aplicativo. Para distribuir esta versão, compacte a pasta `bin/publicacao` inteira. O computador que vai executar o aplicativo precisa ter o **.NET Desktop Runtime 10** instalado.

## Organização do código

- Presentation reúne a janela WinForms, os eventos e as mensagens da interface.
- Application contém os casos de uso e os contratos de persistência e exportação.
- Domain contém o modelo Registro, usado pelos casos de uso e pela infraestrutura.
- Infrastructure implementa a leitura e a gravação do JSON e a exportação XLSX.
- Program.cs cria as dependências e abre a janela.

As camadas usam namespaces e pastas no mesmo projeto. A aplicação depende dos contratos que ela define, e Program.cs conecta esses contratos às implementações. O caminho e o formato do histórico local são compatíveis com a versão anterior.
