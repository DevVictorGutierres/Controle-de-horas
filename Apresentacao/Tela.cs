using System.Diagnostics;
using System.Drawing.Drawing2D;
using ControleTempo.Aplicacao;

namespace ControleTempo.Apresentacao;

sealed class Tela : Form
{
    static readonly Color Fundo = ColorTranslator.FromHtml("#272727");
    static readonly Color Cinza = ColorTranslator.FromHtml("#606060");
    static readonly Color Destaque = ColorTranslator.FromHtml("#00D3B0");
    readonly ControleRegistros controle;
    readonly Stopwatch tempo = new();
    readonly TextBox cliente = Campo();
    readonly TextBox atividade = Campo();
    readonly TextBox descricao = Campo(true);
    readonly DateTimePicker data = new() { Format = DateTimePickerFormat.Short, Dock = DockStyle.Fill };
    readonly CheckBox todoHistorico = new() { Text = "Exportar todo o histórico", Dock = DockStyle.Fill };
    readonly Label relogio = new() { TextAlign = ContentAlignment.MiddleCenter, Dock = DockStyle.Fill };
    readonly Label status = new() { ForeColor = Color.Gainsboro, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft };
    readonly System.Windows.Forms.Timer timer = new() { Interval = 150 };
    readonly Button play;
    readonly Button pause;
    readonly Button stop;
    readonly Button exportar;
    bool historicoDisponivel = true;

    public Tela(ControleRegistros controle)
    {
        this.controle = controle;
        Text = "Controle de Tempo";
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(820, 442);
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        AutoScaleMode = AutoScaleMode.Dpi;
        BackColor = Fundo;
        ForeColor = Color.WhiteSmoke;
        Font = new Font("Segoe UI", 10);

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill, Padding = new Padding(24, 16, 24, 14),
            ColumnCount = 1, RowCount = 0
        };
        Controls.Add(layout);
        var titulo = Rotulo("Controle de tempo");
        titulo.Font = new Font("Segoe UI Semibold", 21);
        Linha(layout, titulo, 53);

        relogio.Font = new Font("Segoe UI Semibold", 37);
        relogio.ForeColor = Destaque;
        play = Botao("Iniciar", Destaque, () => { tempo.Start(); Atualizar(); });
        pause = Botao("Pausar", Cinza, () => { tempo.Stop(); Atualizar(); });
        stop = Botao("Parar e salvar", Cinza, Salvar);

        var cronometro = Cartao(new Padding(18, 17, 18, 12));
        Linha(cronometro, Rotulo("Cronômetro"), 30);
        Linha(cronometro, relogio, 105);
        Linha(cronometro, Grupo(play, pause), 44);
        Linha(cronometro, stop, 44);

        var formulario = Cartao(new Padding(18, 12, 18, 12));
        Linha(formulario, Rotulo("Cliente"), 24);
        Linha(formulario, cliente, 31);
        Linha(formulario, Rotulo("Atividade"), 24);
        Linha(formulario, atividade, 31);
        Linha(formulario, Rotulo("Descrição"), 24);
        Linha(formulario, descricao, 83);

        var conteudo = Grupo(cronometro, formulario);
        conteudo.ColumnStyles[0].Width = 44;
        conteudo.ColumnStyles[1].Width = 56;
        cronometro.Margin = new Padding(0, 0, 8, 0);
        formulario.Margin = new Padding(8, 0, 0, 0);
        Linha(layout, conteudo, 250);

        exportar = Botao("Gerar Excel", Destaque, Exportar);
        var limpar = Botao("Limpar histórico", Cinza, Limpar);
        var rodape = Grupo(data, exportar, status, limpar);
        rodape.ColumnStyles[0].Width = 20;
        rodape.ColumnStyles[1].Width = 21;
        rodape.ColumnStyles[2].Width = 36;
        rodape.ColumnStyles[3].Width = 23;
        status.Padding = new Padding(10, 0, 0, 0);
        data.AccessibleName = "Data para exportação";
        var selecao = Grupo(Rotulo("Data para exportação"), todoHistorico);
        selecao.ColumnStyles[0].Width = 41;
        selecao.ColumnStyles[1].Width = 59;
        Linha(layout, selecao, 38);
        Linha(layout, rodape, 42);
        try
        {
            controle.Carregar();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidDataException)
        {
            historicoDisponivel = false;
            Erro("Falha ao carregar o histórico.", ex);
        }

        data.ValueChanged += (_, _) => Atualizar();
        todoHistorico.CheckedChanged += (_, _) => Atualizar();
        timer.Tick += (_, _) => AtualizarCronometro();
        FormClosing += (_, e) =>
        {
            if (tempo.Elapsed != TimeSpan.Zero || cliente.TextLength + atividade.TextLength + descricao.TextLength > 0)
                e.Cancel = MessageBox.Show(this, "Descartar o registro em edição?", Text,
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes;
        };
        timer.Start();
        Atualizar();
    }

    static TextBox Campo(bool multiline = false) => new()
    {
        Dock = DockStyle.Fill, BackColor = Cinza, ForeColor = Color.White,
        BorderStyle = BorderStyle.FixedSingle, Multiline = multiline, MaxLength = multiline ? 4000 : 250,
        ScrollBars = multiline ? ScrollBars.Vertical : ScrollBars.None
    };

    static Label Rotulo(string texto) => new()
    {
        Text = texto, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft
    };

    static void Linha(TableLayoutPanel layout, Control controle, int altura)
    {
        int linha = layout.RowCount++;
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, altura));
        layout.Controls.Add(controle, 0, linha);
    }

    static TableLayoutPanel Grupo(params Control[] controles)
    {
        var grupo = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = controles.Length, Margin = Padding.Empty };
        foreach (var controle in controles)
        {
            grupo.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f / controles.Length));
            grupo.Controls.Add(controle);
        }
        return grupo;
    }

    static Button Botao(string texto, Color cor, Action acao)
    {
        var botao = new Button
        {
            Text = texto, BackColor = cor, ForeColor = cor == Destaque ? Fundo : Color.White,
            Dock = DockStyle.Fill, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand
        };
        botao.FlatAppearance.BorderSize = 0;
        Arredondar(botao, 16);
        botao.Click += (_, _) => acao();
        return botao;
    }

    static TableLayoutPanel Cartao(Padding padding)
    {
        var painel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill, ColumnCount = 1, Padding = padding,
            BackColor = Color.FromArgb(53, 53, 53)
        };
        Arredondar(painel, 28);
        return painel;
    }

    static void Arredondar(Control controle, int diametro)
    {
        controle.SizeChanged += (_, _) =>
        {
            int w = controle.Width;
            int h = controle.Height;
            int d = Math.Min(diametro, Math.Min(w, h));
            if (d <= 0) return;

            using var path = new GraphicsPath();
            path.AddArc(0, 0, d, d, 180, 90);
            path.AddArc(w - d, 0, d, d, 270, 90);
            path.AddArc(w - d, h - d, d, d, 0, 90);
            path.AddArc(0, h - d, d, d, 90, 90);
            path.CloseFigure();
            var anterior = controle.Region;
            controle.Region = new Region(path);
            anterior?.Dispose();
        };
    }

    DateOnly? DataFiltro => todoHistorico.Checked ? null : DateOnly.FromDateTime(data.Value);

    void AtualizarCronometro()
    {
        var decorrido = tempo.Elapsed;
        relogio.Text = $"{(long)decorrido.TotalHours:00}:{decorrido.Minutes:00}:{decorrido.Seconds:00}";
        play.Text = decorrido == TimeSpan.Zero ? "Iniciar" : "Retomar";
        play.Enabled = !tempo.IsRunning;
        pause.Enabled = tempo.IsRunning;
        stop.Enabled = historicoDisponivel && decorrido > TimeSpan.Zero;
    }

    void Atualizar()
    {
        AtualizarCronometro();
        exportar.Enabled = historicoDisponivel;
        bool exportarTudo = todoHistorico.Checked;
        data.Enabled = !exportarTudo;
        int quantidade = controle.Contar(DataFiltro);
        string periodo = exportarTudo ? "no histórico" : "na data selecionada";
        status.Text = historicoDisponivel ? $"{quantidade} registro(s) {periodo}" : "Histórico local indisponível";
    }

    void Salvar()
    {
        tempo.Stop();
        Atualizar();
        try
        {
            if (!controle.Registrar(cliente.Text, atividade.Text, descricao.Text, tempo.Elapsed))
            {
                MessageBox.Show(this, "Preencha Cliente e Atividade para salvar o registro.", Text);
                return;
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            Erro("Falha ao salvar o registro.", ex);
            return;
        }

        tempo.Reset();
        cliente.Clear();
        atividade.Clear();
        descricao.Clear();
        Atualizar();
    }

    void Limpar()
    {
        if (MessageBox.Show(this, "Apagar todo o histórico local e descartar o registro em edição?", Text,
            MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2) != DialogResult.Yes)
            return;

        try
        {
            controle.Limpar();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            Erro("Falha ao limpar o histórico.", ex);
            return;
        }

        historicoDisponivel = true;
        tempo.Reset();
        cliente.Clear();
        atividade.Clear();
        descricao.Clear();
        todoHistorico.Checked = false;
        data.Value = DateTime.Today;
        Atualizar();
    }

    void Exportar()
    {
        bool exportarTudo = todoHistorico.Checked;
        var filtro = DataFiltro;
        if (controle.Contar(filtro) == 0)
        {
            string mensagem = exportarTudo ? "O histórico local está vazio." : "Nenhum registro para a data selecionada.";
            MessageBox.Show(this, mensagem, Text);
            return;
        }

        using var dialogo = new SaveFileDialog
        {
            Filter = "Planilha Excel (*.xlsx)|*.xlsx", DefaultExt = "xlsx",
            FileName = exportarTudo ? "desenvolvimento-historico-completo.xlsx" : $"desenvolvimento-{filtro:yyyy-MM-dd}.xlsx"
        };
        if (dialogo.ShowDialog(this) != DialogResult.OK) return;

        try
        {
            controle.Exportar(dialogo.FileName, filtro);
            MessageBox.Show(this, "Planilha salva.", Text);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException)
        {
            Erro("Falha ao gerar a planilha.", ex);
        }
    }

    void Erro(string mensagem, Exception ex) =>
        MessageBox.Show(this, $"{mensagem}\n\n{ex.Message}", Text, MessageBoxButtons.OK, MessageBoxIcon.Error);

    protected override void Dispose(bool disposing)
    {
        if (disposing) timer.Dispose();
        base.Dispose(disposing);
    }
}
