namespace ControleTempo.Dominio;

sealed record Registro(DateOnly Data, string Cliente, string Atividade, string Descricao, TimeSpan Tempo);
