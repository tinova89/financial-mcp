using FinancialMcp.Application.Statements.ImportStatement;
using Microsoft.Extensions.Logging;

namespace FinancialMcp.Console.CsvTest;

public sealed class CsvTestService(ILogger<CsvTestService> logger, IStatementCsvParser parser)
{
    public void Test()
    {
        logger.LogInformation("Hello from FinancialMcp.Console, resolved via dependency injection!");

        string csvcontent = @"
Tipo;Status;Data prevista;Data efetiva;Venc. Fatura;Valor;Descrição;Categoria;Conta;Conta transferência;Centro;Data competência;Tags;Cartão;Repetição;Parcela Atual;Parcela Total;Data de criação
Despesa;Conciliado;03/06/2026;03/06/2026;05/07/2026;-70,00;Cachorro quente na brasa ;Lazer/Lanches;Sofisa Visa;;;03/06/2026;;Allan;Único;;;04/06/2026
Despesa;Conciliado;03/06/2026;03/06/2026;05/07/2026;-5,00;Agua com gás ;Lazer/Lanches;Sofisa Visa;;;03/06/2026;;Allan;Único;;;04/06/2026
Despesa;Conciliado;04/06/2026;04/06/2026;05/07/2026;-86,95;Doce doçura ;Lazer/Lanches;Sofisa Visa;;;04/06/2026;;Allan;Único;;;05/06/2026
Despesa;Conciliado;05/06/2026;05/06/2026;05/07/2026;-256,11;O lenhador itaipu ;Lazer/Restaurante;Sofisa Visa;;;05/06/2026;;Allan;Único;;;06/06/2026
Despesa;Conciliado;06/06/2026;06/06/2026;05/07/2026;-53,36;Lady day café da manhã ;Lazer/Lanches;Sofisa Visa;;;06/06/2026;;Allan;Único;;;06/06/2026
Despesa;Conciliado;11/06/2026;11/06/2026;05/07/2026;-298,48;Japones itaipu - dia dos namorados ;Lazer/Restaurante;Sofisa Visa;;;11/06/2026;;Allan;Único;;;15/06/2026
Despesa;Conciliado;13/06/2026;13/06/2026;05/07/2026;-73,50;Deusedinada ;Lazer/Lanches;Sofisa Visa;;;13/06/2026;;Allan;Único;;;15/06/2026
Despesa;Conciliado;20/06/2026;20/06/2026;05/07/2026;-231,55;Pizzaria dilidia ;Lazer/Restaurante;Sofisa Visa;;;20/06/2026;;Allan;Único;;;22/06/2026
Despesa;Conciliado;21/06/2026;21/06/2026;05/07/2026;-15,00;Doces na 128 ;Lazer/Lanches;Sofisa Visa;;;21/06/2026;;Allan;Único;;;22/06/2026
Despesa;Conciliado;21/06/2026;21/06/2026;05/07/2026;-109,99;Lanche no rei dos lanches 83 ;Lazer/Lanches;Sofisa Visa;;;21/06/2026;;Allan;Único;;;22/06/2026
Despesa;Conciliado;25/06/2026;25/06/2026;05/07/2026;-7,95;Ifood club;Lazer;Sofisa Visa;;;25/06/2026;;Allan;Fixo Mês;;;01/06/2026
Despesa;Conciliado;25/06/2026;25/06/2026;05/07/2026;-18,60;Padaria ;Mercado/Padaria;Sofisa Visa;;;25/06/2026;;Allan;Único;;;26/06/2026

";

        parser.Parse(csvcontent, true, Guid.NewGuid(), out var warnings);
    }
}
