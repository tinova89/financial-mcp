using FinancialMcp.Domain.ValueObjects;
using FluentAssertions;
using Xunit;

namespace FinancialMcp.Application.Tests;

/// <summary>
/// Cobre CLAUDE.md > Orientações de Teste > "Agregação de Gasto_Real por
/// categoria-mãe vs. subcategoria completa".
/// </summary>
public class CategoriaTests
{
    [Fact]
    public void Parse_deve_separar_categoria_mae_e_subcategoria()
    {
        var categoria = Categoria.Parse("Moradia/Seguro");

        categoria.CategoriaMae.Should().Be("Moradia");
        categoria.Subcategoria.Should().Be("Seguro");
        categoria.TemSubcategoria.Should().BeTrue();
    }

    [Fact]
    public void Parse_sem_barra_deve_ter_subcategoria_nula()
    {
        var categoria = Categoria.Parse("Moradia");

        categoria.CategoriaMae.Should().Be("Moradia");
        categoria.Subcategoria.Should().BeNull();
        categoria.TemSubcategoria.Should().BeFalse();
    }

    [Fact]
    public void Meta_apenas_com_categoria_mae_deve_casar_com_qualquer_subcategoria()
    {
        var metaMoradia = Categoria.Parse("Moradia");
        var despesaSeguro = Categoria.Parse("Moradia/Seguro");
        var despesaAluguel = Categoria.Parse("Moradia/Aluguel");

        despesaSeguro.CasaComMeta(metaMoradia).Should().BeTrue();
        despesaAluguel.CasaComMeta(metaMoradia).Should().BeTrue();
    }

    [Fact]
    public void Meta_com_subcategoria_completa_deve_exigir_match_exato()
    {
        var metaSeguro = Categoria.Parse("Moradia/Seguro");
        var despesaSeguro = Categoria.Parse("Moradia/Seguro");
        var despesaAluguel = Categoria.Parse("Moradia/Aluguel");

        despesaSeguro.CasaComMeta(metaSeguro).Should().BeTrue();
        despesaAluguel.CasaComMeta(metaSeguro).Should().BeFalse();
    }
}
