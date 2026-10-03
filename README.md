# Desafio técnico — Target Sistemas

Aplicação de console em **C# / .NET 8** que resolve os três exercícios do desafio para Desenvolvedor/a de Sistemas Jr.

## Executar sem instalar .NET (Windows 64 bits)

1. Acesse [Releases](https://github.com/joaoaraujovasconcellos/desafio-target-csharp/releases/latest).
2. Baixe **desafio-target-win-x64.zip** na seção **Assets**.
3. Extraia todo o ZIP para uma pasta no computador.
4. Dê dois cliques em **Target.Desafio.exe** e escolha uma opção no menu.

O pacote inclui o runtime do .NET e os JSONs do exercício. Não precisa instalar SDK nem .NET. Mantenha todos os arquivos extraídos juntos, incluindo a pasta `dados`. O saldo e o histórico de estoque são salvos na pasta `estado` do diretório de execução; extraia em uma pasta onde tenha permissão de escrita.

Para testar: consulte as comissões pela opção 1; na opção 2, registre uma entrada de 10 unidades para o produto 101 (saldo inicial 150, saldo final 160); na opção 3, informe um valor e uma data vencida para calcular os juros.

## Executar pelo código-fonte

Pré-requisito: [.NET SDK 8](https://dotnet.microsoft.com/pt-br/download/dotnet/8.0) ou SDK compatível com `net8.0`. Somente o runtime não permite compilar.

Na raiz do repositório:

```sh
dotnet run --project src/Target.Desafio
```

O menu apresenta comissões, estoque, juros e saída. A aplicação não depende de banco de dados nem de serviços externos.

## 1. Comissões

Lê `dados/vendas.json`, com os registros fornecidos no enunciado. A taxa é aplicada **a cada venda**, antes de agrupar por vendedor:

| Valor da venda | Comissão |
| --- | --- |
| Menor que R$ 100,00 | 0% |
| De R$ 100,00 até menos de R$ 500,00 | 1% |
| A partir de R$ 500,00 | 5% |

Usa `decimal` para valores monetários. Como o enunciado não define arredondamento, cada comissão é arredondada para duas casas decimais com `MidpointRounding.AwayFromZero`, e depois somada. Por exemplo, uma venda de R$ 1.200,50 gera R$ 60,03. Agrupa nomes ignorando maiúsculas/minúsculas e espaços nas extremidades.

Resultados esperados para os dados fornecidos:

| Vendedor | Comissão |
| --- | --- |
| Ana Lima | R$ 404,99 |
| Carlos Oliveira | R$ 379,38 |
| João Silva | R$ 495,69 |
| Maria Souza | R$ 465,96 |

## 2. Estoque

Na primeira execução, lê os cinco produtos de `dados/estoque.json`. Permite consultar saldo, lançar entrada ou saída e consultar histórico. Cada movimentação tem GUID único, tipo, descrição obrigatória, produto, quantidade, data, saldo anterior e saldo final.

Exemplo: entrada de 10 canetas no produto 101 altera o saldo de 150 para 160. Uma saída posterior de 20 altera para 140 e retorna esse saldo ao usuário.

Rejeita produto desconhecido, quantidades menores ou iguais a zero, descrição vazia, tipo inválido e saída maior que o estoque disponível. A alteração inválida não modifica saldo nem histórico.

Após uma movimentação, salva saldo e histórico juntos em `estado/estoque.json`, relativo ao diretório em que o comando foi executado. Escreve primeiro em arquivo temporário e substitui o arquivo final somente depois da escrita. O estado é ignorado pelo Git. Se houver erro ao salvar, a operação não é anunciada como concluída; o próximo acesso recarrega o estado do disco.

O arquivo de estado prevalece sobre o cadastro inicial nas próximas execuções. Para reiniciar a demonstração, faça uma cópia do histórico e remova **somente o arquivo de estado**. A persistência foi projetada para uma única instância da aplicação; não há coordenação entre processos simultâneos.

## 3. Juros por atraso

Solicita valor usando vírgula decimal (sem separador de milhar) e vencimento no formato `dd/MM/aaaa`. Usa a data local do computador como data de hoje.

O enunciado chama a taxa de “multa de 2,5% ao dia”, mas não especifica capitalização. A solução adota **juros simples**:

```text
dias de atraso = máximo(0, hoje - vencimento)
juros = valor × 0,025 × dias de atraso
total = valor + juros
```

O dia do vencimento não gera juros. Vencimento futuro também não. O valor dos juros é arredondado a duas casas decimais ao final, e valores negativos são rejeitados. Exemplo: R$ 1.000,00 com 10 dias de atraso gera R$ 250,00 de juros e total de R$ 1.250,00.

## Organização

```text
dados/                         JSONs originais do enunciado
src/Target.Desafio/
  Program.cs                   Menu e entrada/saída de console
  Calculos.cs                  Regras de comissões e juros
  Estoque.cs                   Regras de estoque e persistência JSON
tests/Target.Desafio.Tests/     Testes automatizados xUnit
.github/workflows/ci.yml        Build e testes no GitHub Actions
```

As regras ficam separadas da interface para serem testadas sem simular o console. A data de referência do cálculo de juros é recebida por parâmetro, tornando os testes independentes do dia de execução. A estrutura mantém o escopo simples, sem frameworks de aplicação desnecessários.

## Validar

```sh
dotnet build src/Target.Desafio/Target.Desafio.csproj --configuration Release
dotnet test tests/Target.Desafio.Tests/Target.Desafio.Tests.csproj --configuration Release
```

Os testes cobrem os limites R$ 100/R$ 500, arredondamento, agrupamento por vendedor, entrada/saída, saldo insuficiente, entradas inválidas, IDs distintos, persistência e juros antes/no/depois do vencimento.

## Gerar a versão para Windows

```sh
dotnet publish src/Target.Desafio/Target.Desafio.csproj --configuration Release --runtime win-x64 --self-contained true --output publish/win-x64
```

Compacte todo o conteúdo de `publish/win-x64` para distribuir. Essa versão contém o runtime e executa diretamente pelo arquivo `Target.Desafio.exe`.
