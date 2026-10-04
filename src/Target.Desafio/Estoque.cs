using System.Text.Json;

namespace Target.Desafio;

public sealed record Produto(int CodigoProduto, string DescricaoProduto, int Estoque);
public sealed record DadosEstoque([property: System.Text.Json.Serialization.JsonRequired] List<Produto> Estoque);
public enum TipoMovimentacao { Entrada, Saida }
public sealed record Movimentacao(Guid Id, int CodigoProduto, TipoMovimentacao Tipo,
    string Descricao, int Quantidade, int EstoqueAnterior, int EstoqueFinal, DateTimeOffset Data);
public sealed record EstadoEstoque(
    [property: System.Text.Json.Serialization.JsonRequired] List<Produto> Produtos,
    [property: System.Text.Json.Serialization.JsonRequired] List<Movimentacao> Movimentacoes);

public sealed class ControleEstoque
{
    private readonly List<Produto> produtos;
    private readonly List<Movimentacao> movimentacoes;
    public IReadOnlyList<Produto> Produtos => produtos.AsReadOnly();
    public IReadOnlyList<Movimentacao> Movimentacoes => movimentacoes.AsReadOnly();

    public ControleEstoque(IEnumerable<Produto> produtos, IEnumerable<Movimentacao>? historico = null)
    {
        ArgumentNullException.ThrowIfNull(produtos);
        this.produtos = produtos.ToList();
        movimentacoes = historico?.ToList() ?? [];
        if (this.produtos.Any(p => p is null || p.CodigoProduto <= 0 || p.Estoque < 0 || string.IsNullOrWhiteSpace(p.DescricaoProduto))
            || this.produtos.Select(p => p.CodigoProduto).Distinct().Count() != this.produtos.Count)
            throw new ArgumentException("Cadastro de produtos inválido.");
        if (movimentacoes.Any(m => m is null))
            throw new ArgumentException("Histórico de movimentações inválido.");
    }

    public Movimentacao Movimentar(int codigo, TipoMovimentacao tipo, int quantidade, string descricao)
    {
        if (!Enum.IsDefined(tipo)) throw new ArgumentException("Tipo de movimentação inválido.");
        if (quantidade <= 0) throw new ArgumentException("A quantidade deve ser maior que zero.");
        if (string.IsNullOrWhiteSpace(descricao)) throw new ArgumentException("Informe uma descrição.");
        var indice = produtos.FindIndex(p => p.CodigoProduto == codigo);
        if (indice < 0) throw new ArgumentException("Produto não encontrado.");
        var produto = produtos[indice];
        if (tipo == TipoMovimentacao.Saida && quantidade > produto.Estoque)
            throw new ArgumentException("Estoque insuficiente para esta saída.");
        var saldo = checked(produto.Estoque + (tipo == TipoMovimentacao.Entrada ? quantidade : -quantidade));
        var movimento = new Movimentacao(Guid.NewGuid(), codigo, tipo, descricao.Trim(),
            quantidade, produto.Estoque, saldo, DateTimeOffset.Now);
        produtos[indice] = produto with { Estoque = saldo };
        movimentacoes.Add(movimento);
        return movimento;
    }

    public void Salvar(string caminho)
    {
        var destino = Path.GetFullPath(caminho);
        Directory.CreateDirectory(Path.GetDirectoryName(destino)!);
        var temporario = destino + ".tmp";
        File.WriteAllText(temporario, JsonSerializer.Serialize(new EstadoEstoque(produtos, movimentacoes), Arquivos.Opcoes));
        File.Move(temporario, destino, overwrite: true);
    }
}

public static class Arquivos
{
    public static readonly JsonSerializerOptions Opcoes = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
    };
    public static T Ler<T>(string caminho) => JsonSerializer.Deserialize<T>(File.ReadAllText(caminho), Opcoes)
        ?? throw new JsonException("O arquivo JSON está vazio.");
}
