using Desafio.Estoque;
using Xunit;

namespace Desafio.Estoque.Tests;

public class RegrasTests
{
    [Fact]
    public void VendasNulasSaoRejeitadas()
    {
        Assert.Throws<ArgumentNullException>(() => CalculadoraComissao.Calcular(null!));
        Assert.Throws<ArgumentException>(() => CalculadoraComissao.Calcular([null!]));
    }

    [Fact]
    public void CadastroEHistoricoComItensNulosSaoRejeitados()
    {
        Assert.Throws<ArgumentNullException>(() => new ControleEstoque(null!));
        Assert.Throws<ArgumentException>(() => new ControleEstoque([null!]));
        Assert.Throws<ArgumentException>(() => new ControleEstoque([new Produto(101, "Caneta", 1)], [null!]));
    }

    [Fact]
    public void JsonExigeListasDeDadosEHistorico()
    {
        Assert.Throws<System.Text.Json.JsonException>(() =>
            System.Text.Json.JsonSerializer.Deserialize<DadosVendas>("{}", Arquivos.Opcoes));
        Assert.Throws<System.Text.Json.JsonException>(() =>
            System.Text.Json.JsonSerializer.Deserialize<DadosEstoque>("{}", Arquivos.Opcoes));
        Assert.Throws<System.Text.Json.JsonException>(() =>
            System.Text.Json.JsonSerializer.Deserialize<EstadoEstoque>("{\"produtos\":[]}", Arquivos.Opcoes));
        Assert.Throws<System.Text.Json.JsonException>(() =>
            System.Text.Json.JsonSerializer.Deserialize<EstadoEstoque>("{\"movimentacoes\":[]}", Arquivos.Opcoes));
        var vazio = System.Text.Json.JsonSerializer.Deserialize<EstadoEstoque>(
            "{\"produtos\":[],\"movimentacoes\":[]}", Arquivos.Opcoes);
        Assert.NotNull(vazio);
        Assert.Empty(vazio.Produtos);
        Assert.Empty(vazio.Movimentacoes);
    }

    [Fact]
    public void TipoInvalidoEOverflowNaoAlteramEstoque()
    {
        var estoque = new ControleEstoque([new Produto(101, "Caneta", int.MaxValue)]);
        Assert.Throws<ArgumentException>(() => estoque.Movimentar(101, (TipoMovimentacao)99, 1, "Compra"));
        Assert.Throws<OverflowException>(() => estoque.Movimentar(101, TipoMovimentacao.Entrada, 1, "Compra"));
        Assert.Equal(int.MaxValue, Assert.Single(estoque.Produtos).Estoque);
        Assert.Empty(estoque.Movimentacoes);
    }

    [Theory]
    [InlineData("99.99", "0")]
    [InlineData("100", "1")]
    [InlineData("499.99", "5")]
    [InlineData("500", "25")]
    [InlineData("1200.50", "60.03")]
    public void ComissaoRespeitaLimitesEArredondamento(string valor, string esperado)
    {
        var cultura = System.Globalization.CultureInfo.InvariantCulture;
        Assert.Equal(decimal.Parse(esperado, cultura), CalculadoraComissao.PorVenda(decimal.Parse(valor, cultura)));
    }

    [Fact]
    public void ComissaoAgrupaVendedorEAplicaTaxaPorVenda()
    {
        var resultado = Assert.Single(CalculadoraComissao.Calcular([new("João", 90m), new("João", 100m), new("João", 500m)]));
        Assert.Equal(690m, resultado.TotalVendas);
        Assert.Equal(26m, resultado.TotalComissao);
        Assert.Throws<ArgumentOutOfRangeException>(() => CalculadoraComissao.PorVenda(-1m));
    }

    [Fact]
    public void EntradaESaidaPossuemIdsDistintosESaldoCorreto()
    {
        var estoque = NovoEstoque();
        var entrada = estoque.Movimentar(101, TipoMovimentacao.Entrada, 10, "Compra");
        var saida = estoque.Movimentar(101, TipoMovimentacao.Saida, 160, "Venda");
        Assert.Equal(160, entrada.EstoqueFinal);
        Assert.Equal(0, saida.EstoqueFinal);
        Assert.NotEqual(entrada.Id, saida.Id);
        Assert.Equal(2, estoque.Movimentacoes.Count);
    }

    [Fact]
    public void MovimentacoesInvalidasNaoAlteramEstoque()
    {
        var estoque = NovoEstoque();
        Assert.Throws<ArgumentException>(() => estoque.Movimentar(101, TipoMovimentacao.Saida, 151, "Venda"));
        Assert.Throws<ArgumentException>(() => estoque.Movimentar(999, TipoMovimentacao.Entrada, 1, "Compra"));
        Assert.Throws<ArgumentException>(() => estoque.Movimentar(101, TipoMovimentacao.Entrada, 0, "Compra"));
        Assert.Throws<ArgumentException>(() => estoque.Movimentar(101, TipoMovimentacao.Entrada, -1, "Compra"));
        Assert.Throws<ArgumentException>(() => estoque.Movimentar(101, TipoMovimentacao.Entrada, 1, " "));
        Assert.Equal(150, Assert.Single(estoque.Produtos).Estoque);
        Assert.Empty(estoque.Movimentacoes);
    }

    [Fact]
    public void PersistenciaMantemSaldoEHistorico()
    {
        var pasta = Path.Combine(Path.GetTempPath(), "estoque-tests-" + Guid.NewGuid());
        var caminho = Path.Combine(pasta, "estoque.json");
        try
        {
            var estoque = NovoEstoque();
            var movimento = estoque.Movimentar(101, TipoMovimentacao.Saida, 10, "Venda");
            estoque.Salvar(caminho);
            var estado = Arquivos.Ler<EstadoEstoque>(caminho);
            var restaurado = new ControleEstoque(estado.Produtos, estado.Movimentacoes);
            Assert.Equal(140, Assert.Single(restaurado.Produtos).Estoque);
            Assert.Equal(movimento, Assert.Single(restaurado.Movimentacoes));
        }
        finally { if (Directory.Exists(pasta)) Directory.Delete(pasta, true); }
    }

    [Theory]
    [InlineData(-1, 0, 0)]
    [InlineData(0, 0, 0)]
    [InlineData(1, 1, 25)]
    [InlineData(10, 10, 250)]
    public void JurosSimplesSomenteAposVencimento(int atraso, int dias, int juros)
    {
        var hoje = new DateOnly(2026, 10, 3);
        var resultado = CalculadoraJuros.Calcular(1000m, hoje.AddDays(-atraso), hoje);
        Assert.Equal(dias, resultado.DiasAtraso);
        Assert.Equal((decimal)juros, resultado.Juros);
        Assert.Equal(1000m + juros, resultado.Total);
    }

    [Fact]
    public void JurosValidamValorEArredondamCentavos()
    {
        var data = new DateOnly(2026, 10, 3);
        Assert.Equal(0.03m, CalculadoraJuros.Calcular(1m, data.AddDays(-1), data).Juros);
        Assert.Throws<ArgumentOutOfRangeException>(() => CalculadoraJuros.Calcular(-1m, data, data));
    }

    private static ControleEstoque NovoEstoque() => new([new Produto(101, "Caneta Azul", 150)]);
}
