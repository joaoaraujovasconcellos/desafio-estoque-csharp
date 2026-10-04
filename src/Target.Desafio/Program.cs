using System.Globalization;
using System.Text;
using System.Text.Json;
using Target.Desafio;

Console.OutputEncoding = Encoding.UTF8;
var cultura = CultureInfo.GetCultureInfo("pt-BR");
var dados = Path.Combine(AppContext.BaseDirectory, "dados");
var estado = Path.Combine(Environment.CurrentDirectory, "estado", "estoque.json");

while (true)
{
    Console.WriteLine("\nDESAFIO TARGET — 1 Comissões | 2 Estoque | 3 Juros | 0 Sair");
    var opcao = Console.ReadLine();
    if (opcao is null or "0") break;
    try
    {
        switch (opcao)
        {
            case "1":
                var vendas = Arquivos.Ler<DadosVendas>(Path.Combine(dados, "vendas.json"));
                foreach (var c in CalculadoraComissao.Calcular(vendas.Vendas))
                    Console.WriteLine($"{c.Vendedor}: vendas {c.TotalVendas.ToString("C2", cultura)} | comissão {c.TotalComissao.ToString("C2", cultura)}");
                break;
            case "2":
                Estoque();
                break;
            case "3":
                Console.Write("Valor (ex.: 1000,50): ");
                if (!decimal.TryParse(Ler(), NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign, cultura, out var valor))
                    throw new ArgumentException("Valor inválido. Use vírgula para os centavos, sem separador de milhar.");
                Console.Write("Vencimento (dd/MM/aaaa): ");
                if (!DateOnly.TryParseExact(Ler(), "dd/MM/yyyy", cultura, DateTimeStyles.None, out var vencimento))
                    throw new ArgumentException("Data inválida.");
                var hoje = DateOnly.FromDateTime(DateTime.Today);
                var resultado = CalculadoraJuros.Calcular(valor, vencimento, hoje);
                Console.WriteLine($"Data do cálculo: {hoje:dd/MM/yyyy} | Atraso: {resultado.DiasAtraso} dia(s)");
                Console.WriteLine($"Juros: {resultado.Juros.ToString("C2", cultura)} | Total: {resultado.Total.ToString("C2", cultura)}");
                break;
            default:
                Console.WriteLine("Escolha uma opção válida.");
                break;
        }
    }
    catch (EndOfStreamException) { break; }
    catch (Exception ex) when (ex is ArgumentException or IOException or JsonException or OverflowException or UnauthorizedAccessException)
    {
        Console.WriteLine($"Não foi possível concluir: {ex.Message}");
    }
}

string Ler() => Console.ReadLine() ?? throw new EndOfStreamException("Entrada encerrada.");
int Inteiro(string pergunta)
{
    Console.Write(pergunta);
    return int.TryParse(Ler(), out var numero) ? numero : throw new ArgumentException("Informe um número inteiro válido.");
}
void Estoque()
{
    var controle = File.Exists(estado)
        ? CarregarEstado()
        : new ControleEstoque(Arquivos.Ler<DadosEstoque>(Path.Combine(dados, "estoque.json")).Estoque);
    foreach (var p in controle.Produtos)
        Console.WriteLine($"{p.CodigoProduto} — {p.DescricaoProduto}: {p.Estoque} unidade(s)");
    Console.WriteLine("1 Entrada | 2 Saída | 3 Histórico | 0 Voltar");
    var escolha = Ler();
    if (escolha == "0") return;
    if (escolha == "3")
    {
        foreach (var m in controle.Movimentacoes)
            Console.WriteLine($"{m.Id} | {m.Data:dd/MM/yyyy HH:mm:ss} | Produto {m.CodigoProduto} | {m.Tipo} | {m.Descricao} | Qtde {m.Quantidade} | Saldo {m.EstoqueAnterior} → {m.EstoqueFinal}");
        if (controle.Movimentacoes.Count == 0) Console.WriteLine("Nenhuma movimentação registrada.");
        return;
    }
    var tipo = escolha switch { "1" => TipoMovimentacao.Entrada, "2" => TipoMovimentacao.Saida,
        _ => throw new ArgumentException("Opção inválida.") };
    var codigo = Inteiro("Código do produto: ");
    var quantidade = Inteiro("Quantidade: ");
    Console.Write("Descrição da movimentação: ");
    var movimento = controle.Movimentar(codigo, tipo, quantidade, Ler());
    controle.Salvar(estado);
    Console.WriteLine($"Movimentação salva: {movimento.Id} | Estoque final: {movimento.EstoqueFinal}");
}
ControleEstoque CarregarEstado()
{
    var salvo = Arquivos.Ler<EstadoEstoque>(estado);
    if (salvo.Movimentacoes is null)
        throw new JsonException("O estado deve informar a lista de movimentações.");
    return new ControleEstoque(salvo.Produtos, salvo.Movimentacoes);
}
