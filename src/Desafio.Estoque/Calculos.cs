namespace Desafio.Estoque;

public sealed record Venda(string Vendedor, decimal Valor);
public sealed record DadosVendas([property: System.Text.Json.Serialization.JsonRequired] List<Venda> Vendas);
public sealed record Comissao(string Vendedor, decimal TotalVendas, decimal TotalComissao);

public static class CalculadoraComissao
{
    public static decimal PorVenda(decimal valor)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(valor);
        var taxa = valor < 100m ? 0m : valor < 500m ? 0.01m : 0.05m;
        return decimal.Round(valor * taxa, 2, MidpointRounding.AwayFromZero);
    }

    public static IReadOnlyList<Comissao> Calcular(IEnumerable<Venda> vendas)
    {
        ArgumentNullException.ThrowIfNull(vendas);
        var lista = vendas.ToList();
        if (lista.Any(v => v is null || string.IsNullOrWhiteSpace(v.Vendedor)))
            throw new ArgumentException("Toda venda deve informar o vendedor.");
        return lista.GroupBy(v => v.Vendedor.Trim(), StringComparer.OrdinalIgnoreCase)
            .Select(g => new Comissao(g.Key, g.Sum(v => v.Valor), g.Sum(v => PorVenda(v.Valor))))
            .OrderBy(c => c.Vendedor, StringComparer.OrdinalIgnoreCase).ToList();
    }
}

public sealed record ResultadoJuros(int DiasAtraso, decimal Juros, decimal Total);

public static class CalculadoraJuros
{
    public static ResultadoJuros Calcular(decimal valor, DateOnly vencimento, DateOnly hoje)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(valor);
        var dias = Math.Max(0, hoje.DayNumber - vencimento.DayNumber);
        var juros = decimal.Round(valor * 0.025m * dias, 2, MidpointRounding.AwayFromZero);
        return new ResultadoJuros(dias, juros, valor + juros);
    }
}
